using SportsBetting.Domain.Configuration;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Services;
using Xunit;

namespace SportsBetting.Data.Tests;

/// <summary>
/// Tests for commission calculation service
/// Tests tiered commission rates and liquidity provider incentives
/// </summary>
public class CommissionServiceTests
{
    private readonly CommissionConfiguration _config;
    private readonly CommissionService _service;

    public CommissionServiceTests()
    {
        _config = new CommissionConfiguration();
        _service = new CommissionService(_config);
    }

    [Fact]
    public void CalculateCommission_StandardTierTaker_Charges5Percent()
    {
        // Arrange
        var user = User.CreateWithPassword("alice", "alice@test.com", "Password123!");
        var grossWinnings = 100m;

        // Act
        var commission = _service.CalculateCommission(user, grossWinnings, LiquidityRole.Taker);

        // Assert
        Assert.Equal(1.5m, commission); // 1.5% of $100 = $1.50
    }

    [Fact]
    public void CalculateCommission_StandardTierMaker_Charges4Percent()
    {
        // Arrange
        var user = User.CreateWithPassword("alice", "alice@test.com", "Password123!");
        var grossWinnings = 100m;

        // Act
        var commission = _service.CalculateCommission(user, grossWinnings, LiquidityRole.Maker);

        // Assert
        // 1.5% base - 20% maker discount = 1.2%
        Assert.Equal(1.2m, commission); // 1.2% of $100 = $1.20
    }

    [Fact]
    public void CalculateCommission_BronzeTierTaker_Charges4Percent()
    {
        // Arrange
        var user = User.CreateWithPassword("alice", "alice@test.com", "Password123!");
        user.UpdateCommissionTier(CommissionTier.Bronze);
        var grossWinnings = 100m;

        // Act
        var commission = _service.CalculateCommission(user, grossWinnings, LiquidityRole.Taker);

        // Assert
        Assert.Equal(1.25m, commission); // 1.25% of $100 = $1.25
    }

    [Fact]
    public void CalculateCommission_BronzeTierMaker_Charges3Point2Percent()
    {
        // Arrange
        var user = User.CreateWithPassword("alice", "alice@test.com", "Password123!");
        user.UpdateCommissionTier(CommissionTier.Bronze);
        var grossWinnings = 100m;

        // Act
        var commission = _service.CalculateCommission(user, grossWinnings, LiquidityRole.Maker);

        // Assert
        // 1.25% base - 20% maker discount = 1.0%
        Assert.Equal(1.0m, commission);
    }

    [Fact]
    public void CalculateCommission_PlatinumTierMaker_ChargesPoint8Percent()
    {
        // Arrange
        var user = User.CreateWithPassword("whale", "whale@test.com", "Password123!");
        user.UpdateCommissionTier(CommissionTier.Platinum);
        var grossWinnings = 10000m;

        // Act
        var commission = _service.CalculateCommission(user, grossWinnings, LiquidityRole.Maker);

        // Assert
        // 0.5% base - 20% maker discount = 0.4%
        Assert.Equal(40m, commission); // 0.4% of $10,000 = $40
    }

    [Fact]
    public void CalculateCommission_ZeroWinnings_ChargesZero()
    {
        // Arrange
        var user = User.CreateWithPassword("alice", "alice@test.com", "Password123!");
        var grossWinnings = 0m;

        // Act
        var commission = _service.CalculateCommission(user, grossWinnings, LiquidityRole.Taker);

        // Assert
        Assert.Equal(0m, commission);
    }

    [Fact]
    public void CalculateCommission_SmallWinnings_AppliesMinimum()
    {
        // Arrange
        var user = User.CreateWithPassword("alice", "alice@test.com", "Password123!");
        var grossWinnings = 0.10m; // 10 cents

        // Act
        var commission = _service.CalculateCommission(user, grossWinnings, LiquidityRole.Taker);

        // Assert
        // 1.5% of $0.10 = $0.0015, but minimum is $0.01
        Assert.Equal(0.01m, commission);
    }

    [Fact]
    public void CalculateTier_LowVolume_ReturnsStandard()
    {
        // Arrange
        var volume = 5000m;

        // Act
        var tier = _service.CalculateTier(volume);

        // Assert
        Assert.Equal(CommissionTier.Standard, tier);
    }

    [Fact]
    public void CalculateTier_BronzeThreshold_ReturnsBronze()
    {
        // Arrange
        var volume = 15000m;

        // Act
        var tier = _service.CalculateTier(volume);

        // Assert
        Assert.Equal(CommissionTier.Bronze, tier);
    }

    [Fact]
    public void CalculateTier_SilverThreshold_ReturnsSilver()
    {
        // Arrange
        var volume = 100000m;

        // Act
        var tier = _service.CalculateTier(volume);

        // Assert
        Assert.Equal(CommissionTier.Silver, tier);
    }

    [Fact]
    public void CalculateTier_GoldThreshold_ReturnsGold()
    {
        // Arrange
        var volume = 500000m;

        // Act
        var tier = _service.CalculateTier(volume);

        // Assert
        Assert.Equal(CommissionTier.Gold, tier);
    }

    [Fact]
    public void CalculateTier_PlatinumThreshold_ReturnsPlatinum()
    {
        // Arrange
        var volume = 2000000m;

        // Act
        var tier = _service.CalculateTier(volume);

        // Assert
        Assert.Equal(CommissionTier.Platinum, tier);
    }

    [Fact]
    public void UpdateUserTier_VolumeIncreases_PromotesUser()
    {
        // Arrange
        var user = User.CreateWithPassword("alice", "alice@test.com", "Password123!");
        var stats = new UserStatistics(user);

        // Simulate $50k in 30-day volume
        for (int i = 0; i < 50; i++)
        {
            stats.RecordBetMatched(1000m, isMaker: true);
        }

        // Act
        var wasChanged = _service.UpdateUserTier(user);

        // Assert
        Assert.True(wasChanged);
        Assert.Equal(CommissionTier.Silver, user.CommissionTier);
    }

    [Fact]
    public void UpdateUserTier_VolumeStaysSame_NoChange()
    {
        // Arrange
        var user = User.CreateWithPassword("alice", "alice@test.com", "Password123!");
        user.UpdateCommissionTier(CommissionTier.Bronze);
        var stats = new UserStatistics(user);

        // Simulate $15k in 30-day volume (still Bronze range)
        stats.RecordBetMatched(15000m, isMaker: true);

        // Act
        var wasChanged = _service.UpdateUserTier(user);

        // Assert
        Assert.False(wasChanged);
        Assert.Equal(CommissionTier.Bronze, user.CommissionTier);
    }

    [Fact]
    public void GetEffectiveRate_AllTiersMaker_ReturnsDiscountedRates()
    {
        // Arrange
        var user = User.CreateWithPassword("alice", "alice@test.com", "Password123!");

        // Act & Assert for each tier
        user.UpdateCommissionTier(CommissionTier.Standard);
        Assert.Equal(0.012m, _service.GetEffectiveRate(user, LiquidityRole.Maker)); // 1.5% - 20% = 1.2%

        user.UpdateCommissionTier(CommissionTier.Bronze);
        Assert.Equal(0.01m, _service.GetEffectiveRate(user, LiquidityRole.Maker)); // 1.25% - 20% = 1.0%

        user.UpdateCommissionTier(CommissionTier.Silver);
        Assert.Equal(0.008m, _service.GetEffectiveRate(user, LiquidityRole.Maker)); // 1% - 20% = 0.8%

        user.UpdateCommissionTier(CommissionTier.Gold);
        Assert.Equal(0.006m, _service.GetEffectiveRate(user, LiquidityRole.Maker)); // 0.75% - 20% = 0.6%

        user.UpdateCommissionTier(CommissionTier.Platinum);
        Assert.Equal(0.004m, _service.GetEffectiveRate(user, LiquidityRole.Maker)); // 0.5% - 20% = 0.4%
    }

    [Fact]
    public void RealWorldScenario_HighVolumeTrader_BenefitsFromTierAndMaker()
    {
        // Arrange: Alice is a high-volume trader who mostly provides liquidity
        var user = User.CreateWithPassword("alice_pro", "alice@trading.com", "Password123!");
        user.UpdateCommissionTier(CommissionTier.Gold); // She's earned Gold tier

        var grossWinnings = 5000m; // She wins $5,000 on a bet

        // Act: Calculate commission as maker (she provided liquidity)
        var makerCommission = _service.CalculateCommission(user, grossWinnings, LiquidityRole.Maker);

        // Calculate what she would have paid as Standard tier taker
        var standardTakerUser = User.CreateWithPassword("newbie", "newbie@test.com", "Password123!");
        var standardCommission = _service.CalculateCommission(standardTakerUser, grossWinnings, LiquidityRole.Taker);

        // Assert
        Assert.Equal(30m, makerCommission); // Gold maker: 0.6% of $5,000 = $30
        Assert.Equal(75m, standardCommission); // Standard taker: 1.5% of $5,000 = $75
        Assert.Equal(45m, standardCommission - makerCommission); // She saves $45!
    }
}
