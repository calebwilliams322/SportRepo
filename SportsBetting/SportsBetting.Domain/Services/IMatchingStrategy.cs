using SportsBetting.Domain.Entities;

namespace SportsBetting.Domain.Services;

/// <summary>
/// Strategy for matching exchange bets
/// </summary>
public interface IMatchingStrategy
{
    /// <summary>
    /// Allocate an incoming order against a list of candidate bets
    /// </summary>
    /// <param name="incomingStake">The stake to allocate</param>
    /// <param name="candidates">List of candidate bets (already sorted by price/time)</param>
    /// <returns>Dictionary of ExchangeBet -> allocated amount</returns>
    Dictionary<ExchangeBet, decimal> AllocateMatches(
        decimal incomingStake,
        List<ExchangeBet> candidates);
}

/// <summary>
/// FIFO (First-In-First-Out) matching strategy
/// Current default - matches earliest bets first
/// </summary>
public class FifoMatchingStrategy : IMatchingStrategy
{
    public Dictionary<ExchangeBet, decimal> AllocateMatches(
        decimal incomingStake,
        List<ExchangeBet> candidates)
    {
        var allocations = new Dictionary<ExchangeBet, decimal>();
        var remainingStake = incomingStake;

        foreach (var candidate in candidates)
        {
            if (remainingStake <= 0)
                break;

            var matchAmount = Math.Min(remainingStake, candidate.UnmatchedStake);
            allocations[candidate] = matchAmount;
            remainingStake -= matchAmount;
        }

        return allocations;
    }
}

/// <summary>
/// Pure Pro-Rata matching strategy
/// Distributes incoming orders proportionally based on order size
/// </summary>
public class ProRataMatchingStrategy : IMatchingStrategy
{
    public Dictionary<ExchangeBet, decimal> AllocateMatches(
        decimal incomingStake,
        List<ExchangeBet> candidates)
    {
        var allocations = new Dictionary<ExchangeBet, decimal>();

        if (candidates.Count == 0)
            return allocations;

        // Calculate total available liquidity
        var totalLiquidity = candidates.Sum(c => c.UnmatchedStake);

        // If incoming stake >= total liquidity, fill everyone completely (FIFO order)
        if (incomingStake >= totalLiquidity)
        {
            foreach (var candidate in candidates)
            {
                allocations[candidate] = candidate.UnmatchedStake;
            }
            return allocations;
        }

        // Pro-rata allocation based on proportion of liquidity provided
        foreach (var candidate in candidates)
        {
            var proportion = candidate.UnmatchedStake / totalLiquidity;
            var allocation = Math.Floor(incomingStake * proportion * 100) / 100; // Round down to 2 decimals
            allocations[candidate] = allocation;
        }

        // Handle rounding remainder (give to largest order)
        var totalAllocated = allocations.Values.Sum();
        var remainder = incomingStake - totalAllocated;

        if (remainder > 0)
        {
            var largestOrder = candidates.OrderByDescending(c => c.UnmatchedStake).First();
            allocations[largestOrder] += remainder;
        }

        return allocations;
    }
}

/// <summary>
/// Hybrid Pro-Rata with Top matching strategy
/// Gives priority to first N orders (FIFO), then distributes remainder pro-rata
/// </summary>
public class ProRataWithTopMatchingStrategy : IMatchingStrategy
{
    private readonly int _topOrderCount;
    private readonly decimal _topAllocationPercent;

    /// <summary>
    /// Create hybrid strategy
    /// </summary>
    /// <param name="topOrderCount">Number of top orders to give FIFO priority (default: 1)</param>
    /// <param name="topAllocationPercent">Percentage of incoming order to allocate FIFO (default: 40%)</param>
    public ProRataWithTopMatchingStrategy(
        int topOrderCount = 1,
        decimal topAllocationPercent = 0.40m)
    {
        _topOrderCount = topOrderCount;
        _topAllocationPercent = topAllocationPercent;
    }

    public Dictionary<ExchangeBet, decimal> AllocateMatches(
        decimal incomingStake,
        List<ExchangeBet> candidates)
    {
        var allocations = new Dictionary<ExchangeBet, decimal>();

        if (candidates.Count == 0)
            return allocations;

        // Split incoming stake: X% FIFO, remainder Pro-Rata
        var fifoAmount = incomingStake * _topAllocationPercent;
        var proRataAmount = incomingStake - fifoAmount;

        // Phase 1: Allocate top portion using FIFO
        var topOrders = candidates.Take(_topOrderCount).ToList();
        var fifoStrategy = new FifoMatchingStrategy();
        var fifoAllocations = fifoStrategy.AllocateMatches(fifoAmount, topOrders);

        foreach (var allocation in fifoAllocations)
        {
            allocations[allocation.Key] = allocation.Value;
        }

        // Calculate remaining capacity for top orders
        var topOrdersRemainingCapacity = topOrders
            .ToDictionary(
                o => o,
                o => o.UnmatchedStake - (fifoAllocations.ContainsKey(o) ? fifoAllocations[o] : 0));

        // If FIFO portion wasn't fully allocated, add remainder to pro-rata pool
        var fifoAllocated = fifoAllocations.Values.Sum();
        if (fifoAllocated < fifoAmount)
        {
            proRataAmount += (fifoAmount - fifoAllocated);
        }

        // Phase 2: Allocate remainder using Pro-Rata across ALL orders
        // (including top orders' remaining capacity)
        var proRataCandidates = candidates.Select(c =>
        {
            // For top orders, only consider remaining capacity
            if (topOrdersRemainingCapacity.ContainsKey(c))
            {
                var remainingCapacity = topOrdersRemainingCapacity[c];
                if (remainingCapacity <= 0)
                    return null; // Already fully filled in FIFO phase

                // Create a temporary bet with remaining capacity
                return new { Bet = c, AvailableStake = remainingCapacity };
            }
            else
            {
                return new { Bet = c, AvailableStake = c.UnmatchedStake };
            }
        })
        .Where(x => x != null && x.AvailableStake > 0)
        .ToList();

        if (proRataCandidates.Count > 0 && proRataAmount > 0)
        {
            var totalAvailable = proRataCandidates.Sum(c => c!.AvailableStake);

            foreach (var candidate in proRataCandidates)
            {
                var proportion = candidate!.AvailableStake / totalAvailable;
                var allocation = Math.Floor(proRataAmount * proportion * 100) / 100;

                if (allocations.ContainsKey(candidate.Bet))
                    allocations[candidate.Bet] += allocation;
                else
                    allocations[candidate.Bet] = allocation;
            }

            // Handle rounding remainder
            var proRataAllocated = allocations.Values.Sum() - fifoAllocated;
            var remainder = proRataAmount - proRataAllocated;

            if (remainder > 0)
            {
                var largestRemaining = proRataCandidates
                    .OrderByDescending(c => c!.AvailableStake)
                    .First();
                allocations[largestRemaining!.Bet] += remainder;
            }
        }

        return allocations;
    }
}
