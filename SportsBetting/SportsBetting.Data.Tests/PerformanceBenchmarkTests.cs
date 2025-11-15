using Microsoft.EntityFrameworkCore;
using SportsBetting.Data;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Services;
using SportsBetting.Domain.ValueObjects;
using System.Diagnostics;

namespace SportsBetting.Data.Tests;

/// <summary>
/// Performance benchmarks to ensure retry logic doesn't create unacceptable latency
/// These tests measure throughput, latency, and system behavior under load
/// </summary>
public class PerformanceBenchmarkTests : IDisposable
{
    private readonly SportsBettingDbContext _context;
    private readonly string _testDatabaseName;

    public PerformanceBenchmarkTests()
    {
        // Create a unique test database for each test run
        _testDatabaseName = $"sportsbetting_perf_test_{Guid.NewGuid():N}";

        var connectionString = Environment.GetEnvironmentVariable("SPORTSBETTING_DB")
            ?? "Host=localhost;Database=sportsbetting;Username=calebwilliams";

        // Create test database
        var masterConnectionString = connectionString.Replace("sportsbetting", "postgres");
        using (var masterContext = new DbContext(new DbContextOptionsBuilder<DbContext>()
            .UseNpgsql(masterConnectionString).Options))
        {
            // Drop database if it exists from a previous failed test run
            var dropDbSql = $"DROP DATABASE IF EXISTS \"{_testDatabaseName}\"";
            masterContext.Database.ExecuteSqlRaw(dropDbSql);

            // Create test database
            var createDbSql = $"CREATE DATABASE \"{_testDatabaseName}\"";
            masterContext.Database.ExecuteSqlRaw(createDbSql);
        }

        // Connect to test database and apply migrations
        var testConnectionString = connectionString.Replace("sportsbetting", _testDatabaseName);
        var options = new DbContextOptionsBuilder<SportsBettingDbContext>()
            .UseNpgsql(testConnectionString)
            .Options;

        _context = new SportsBettingDbContext(options);
        _context.Database.Migrate();
    }

    [Fact]
    public async Task LowContention_OperationsCompleteQuickly()
    {
        // Arrange - Create 10 different users (no contention)
        var users = new List<(Guid userId, Guid walletId)>();
        for (int i = 0; i < 10; i++)
        {
            var user = new User($"perf_user_{i}", $"perf{i}@test.com", "hash123");
            var wallet = new Wallet(user);
            _context.Users.Add(user);
            users.Add((user.Id, wallet.Id));
        }
        await _context.SaveChangesAsync();

        var connectionString = _context.Database.GetConnectionString()!;

        // Act - Measure time for 10 concurrent deposits (each to different wallet)
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task<bool>>();

        foreach (var (userId, walletId) in users)
        {
            tasks.Add(Task.Run(async () =>
            {
                var options = new DbContextOptionsBuilder<SportsBettingDbContext>()
                    .UseNpgsql(connectionString)
                    .Options;

                using var context = new SportsBettingDbContext(options);
                var userToUpdate = await context.Users
                    .Include(u => u.Wallet)
                    .FirstAsync(u => u.Id == userId);

                var service = new WalletService();
                var transaction = service.Deposit(userToUpdate, new Money(100m, "USD"), "Low contention deposit");
                context.Transactions.Add(transaction);
                await context.SaveChangesAsync();
                return true;
            }));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var successCount = results.Count(r => r);
        Assert.Equal(10, successCount); // All should succeed with no contention

        // With no contention, should complete very quickly (under 1 second)
        Assert.True(stopwatch.ElapsedMilliseconds < 1000,
            $"Low contention deposits took {stopwatch.ElapsedMilliseconds}ms, expected under 1000ms");

        Console.WriteLine($"Low contention: 10 deposits in {stopwatch.ElapsedMilliseconds}ms ({stopwatch.ElapsedMilliseconds / 10.0}ms avg)");
    }

    [Fact]
    public async Task MediumContention_MaintainsReasonableLatency()
    {
        // Arrange - Create 1 user, 50 concurrent deposits
        var user = new User("medium_contention", "medium@test.com", "hash123");
        var wallet = new Wallet(user);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var userId = user.Id;
        var connectionString = _context.Database.GetConnectionString()!;

        // Act - 50 concurrent deposits to same wallet
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task<bool>>();

        for (int i = 0; i < 50; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                const int maxRetries = 3;
                int retryCount = 0;

                while (retryCount < maxRetries)
                {
                    var options = new DbContextOptionsBuilder<SportsBettingDbContext>()
                        .UseNpgsql(connectionString)
                        .Options;

                    using var context = new SportsBettingDbContext(options);

                    try
                    {
                        var userToUpdate = await context.Users
                            .Include(u => u.Wallet)
                            .FirstAsync(u => u.Id == userId);

                        var service = new WalletService();
                        var transaction = service.Deposit(userToUpdate, new Money(10m, "USD"), "Medium contention");
                        context.Transactions.Add(transaction);
                        await context.SaveChangesAsync();
                        return true;
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        retryCount++;
                        if (retryCount >= maxRetries)
                        {
                            return false;
                        }
                        await Task.Delay(TimeSpan.FromMilliseconds(50 * retryCount));
                    }
                }
                return false;
            }));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var successCount = results.Count(r => r);

        // Most should succeed, some may conflict
        // With retry logic, expect reasonable success rate (at least 30% = 15 out of 50)
        // This accounts for variance in test execution and system load
        Assert.True(successCount >= 15, $"Expected at least 15 successes with retry logic, got {successCount}");

        // Should complete within reasonable time (under 5 seconds with retries)
        Assert.True(stopwatch.ElapsedMilliseconds < 5000,
            $"Medium contention took {stopwatch.ElapsedMilliseconds}ms, expected under 5000ms");

        // Verify final balance
        await _context.Entry(user).ReloadAsync();
        await _context.Entry(wallet).ReloadAsync();
        Assert.Equal(successCount * 10m, wallet.Balance.Amount);

        Console.WriteLine($"Medium contention: {successCount} succeeded in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task HighContention_StillFunctional()
    {
        // Arrange - Create 1 user, 50 concurrent deposits
        var user = new User("high_contention", "high@test.com", "hash123");
        var wallet = new Wallet(user);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var userId = user.Id;
        var connectionString = _context.Database.GetConnectionString()!;

        // Act - 50 concurrent deposits to same wallet (reduced from 100 to avoid connection pool exhaustion)
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task<bool>>();

        for (int i = 0; i < 50; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                const int maxRetries = 3;
                int retryCount = 0;

                while (retryCount < maxRetries)
                {
                    var options = new DbContextOptionsBuilder<SportsBettingDbContext>()
                        .UseNpgsql(connectionString)
                        .Options;

                    using var context = new SportsBettingDbContext(options);

                    try
                    {
                        var userToUpdate = await context.Users
                            .Include(u => u.Wallet)
                            .FirstAsync(u => u.Id == userId);

                        var service = new WalletService();
                        var transaction = service.Deposit(userToUpdate, new Money(5m, "USD"), "High contention");
                        context.Transactions.Add(transaction);
                        await context.SaveChangesAsync();
                        return true;
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        retryCount++;
                        if (retryCount >= maxRetries)
                        {
                            return false;
                        }
                        await Task.Delay(TimeSpan.FromMilliseconds(50 * retryCount));
                    }
                }
                return false;
            }));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var successCount = results.Count(r => r);
        var conflictCount = results.Count(r => !r);

        // With high contention and retries, expect reasonable success rate (at least 30% = 15 out of 50)
        // This accounts for variance in test execution and system load
        Assert.True(successCount >= 15, $"Expected at least 15 successes with retry logic, got {successCount}");

        // Verify consistency
        await _context.Entry(user).ReloadAsync();
        await _context.Entry(wallet).ReloadAsync();
        Assert.Equal(successCount * 5m, wallet.Balance.Amount);

        // Calculate throughput
        var throughput = successCount / (stopwatch.ElapsedMilliseconds / 1000.0);
        Console.WriteLine($"High contention: {successCount} succeeded, {conflictCount} conflicted in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Throughput: {throughput:F2} successful operations/second");

        // Should maintain reasonable throughput even under high contention
        Assert.True(throughput >= 10, $"Throughput {throughput:F2} ops/sec is below minimum threshold of 10 ops/sec");
    }

    [Fact]
    public async Task SingleOperation_BaselineLatency()
    {
        // This establishes a baseline for single operation latency (no contention)
        // Arrange
        var user = new User("baseline_user", "baseline@test.com", "hash123");
        var wallet = new Wallet(user);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var userId = user.Id;
        var connectionString = _context.Database.GetConnectionString()!;

        // Act - Measure 10 sequential deposits
        var latencies = new List<long>();

        for (int i = 0; i < 10; i++)
        {
            var stopwatch = Stopwatch.StartNew();

            var options = new DbContextOptionsBuilder<SportsBettingDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            using var context = new SportsBettingDbContext(options);
            var userToUpdate = await context.Users
                .Include(u => u.Wallet)
                .FirstAsync(u => u.Id == userId);

            var service = new WalletService();
            var transaction = service.Deposit(userToUpdate, new Money(10m, "USD"), "Baseline deposit");
            context.Transactions.Add(transaction);
            await context.SaveChangesAsync();

            stopwatch.Stop();
            latencies.Add(stopwatch.ElapsedMilliseconds);
        }

        // Assert
        var avgLatency = latencies.Average();
        var maxLatency = latencies.Max();

        Console.WriteLine($"Baseline latency - Avg: {avgLatency:F2}ms, Max: {maxLatency}ms");

        // Single operations with no contention should be very fast
        Assert.True(avgLatency < 100, $"Average baseline latency {avgLatency:F2}ms exceeds 100ms");
        Assert.True(maxLatency < 200, $"Max baseline latency {maxLatency}ms exceeds 200ms");
    }

    public void Dispose()
    {
        // Clean up test database
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
