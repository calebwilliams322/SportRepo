using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsBetting.Data;
using SportsBetting.Domain.Entities;

namespace SportsBetting.API.Controllers;

/// <summary>
/// API endpoints for house revenue tracking and reporting
/// Admin-only access to view revenue metrics
/// </summary>
[ApiController]
[Route("api/admin/revenue")]
[Authorize(Roles = "Admin")] // Only admins can view revenue
public class RevenueController : ControllerBase
{
    private readonly SportsBettingDbContext _context;
    private readonly ILogger<RevenueController> _logger;

    public RevenueController(
        SportsBettingDbContext context,
        ILogger<RevenueController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get current hour's revenue
    /// </summary>
    /// <returns>Revenue summary for the current hour</returns>
    [HttpGet("current-hour")]
    public async Task<ActionResult<RevenueSummary>> GetCurrentHourRevenue()
    {
        var now = DateTime.UtcNow;
        var hourStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
        var hourEnd = hourStart.AddHours(1);

        var revenue = await _context.HouseRevenue
            .FirstOrDefaultAsync(r =>
                r.PeriodStart == hourStart &&
                r.PeriodEnd == hourEnd &&
                r.PeriodType == "Hourly"
            );

        if (revenue == null)
        {
            return Ok(new RevenueSummary
            {
                PeriodStart = hourStart,
                PeriodEnd = hourEnd,
                PeriodType = "Hourly"
            });
        }

        return Ok(revenue.GetSummary());
    }

    /// <summary>
    /// Get today's revenue (aggregated from hourly records)
    /// </summary>
    /// <returns>Revenue summary for today</returns>
    [HttpGet("today")]
    public async Task<ActionResult<RevenueSummary>> GetTodayRevenue()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var hourlyRecords = await _context.HouseRevenue
            .Where(r =>
                r.PeriodStart >= today &&
                r.PeriodStart < tomorrow &&
                r.PeriodType == "Hourly"
            )
            .ToListAsync();

        if (!hourlyRecords.Any())
        {
            return Ok(new RevenueSummary
            {
                PeriodStart = today,
                PeriodEnd = tomorrow,
                PeriodType = "Daily"
            });
        }

        // Aggregate hourly records into daily summary
        return Ok(new RevenueSummary
        {
            PeriodStart = today,
            PeriodEnd = tomorrow,
            PeriodType = "Daily",

            SportsbookRevenue = hourlyRecords.Sum(r => r.SportsbookNetRevenue),
            SportsbookVolume = hourlyRecords.Sum(r => r.SportsbookVolume),
            SportsbookBetsCount = hourlyRecords.Sum(r => r.SportsbookBetsSettled),
            SportsbookHoldPercentage = CalculateHoldPercentage(
                hourlyRecords.Sum(r => r.SportsbookNetRevenue),
                hourlyRecords.Sum(r => r.SportsbookVolume)
            ),

            ExchangeRevenue = hourlyRecords.Sum(r => r.ExchangeCommissionRevenue),
            ExchangeVolume = hourlyRecords.Sum(r => r.ExchangeVolume),
            ExchangeMatchesCount = hourlyRecords.Sum(r => r.ExchangeMatchesSettled),
            ExchangeEffectiveRate = CalculateEffectiveRate(
                hourlyRecords.Sum(r => r.ExchangeCommissionRevenue),
                hourlyRecords.Sum(r => r.ExchangeVolume)
            ),

            TotalRevenue = hourlyRecords.Sum(r => r.TotalRevenue),
            TotalVolume = hourlyRecords.Sum(r => r.TotalVolume),
            EffectiveMargin = CalculateEffectiveRate(
                hourlyRecords.Sum(r => r.TotalRevenue),
                hourlyRecords.Sum(r => r.TotalVolume)
            )
        });
    }

    /// <summary>
    /// Get revenue for a specific date range
    /// </summary>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (exclusive)</param>
    /// <returns>Aggregated revenue summary</returns>
    [HttpGet("range")]
    public async Task<ActionResult<RevenueSummary>> GetRevenueRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        if (endDate <= startDate)
        {
            return BadRequest("End date must be after start date");
        }

        if ((endDate - startDate).TotalDays > 90)
        {
            return BadRequest("Date range cannot exceed 90 days");
        }

        var hourlyRecords = await _context.HouseRevenue
            .Where(r =>
                r.PeriodStart >= startDate &&
                r.PeriodStart < endDate &&
                r.PeriodType == "Hourly"
            )
            .ToListAsync();

        if (!hourlyRecords.Any())
        {
            return Ok(new RevenueSummary
            {
                PeriodStart = startDate,
                PeriodEnd = endDate,
                PeriodType = "Custom"
            });
        }

        return Ok(new RevenueSummary
        {
            PeriodStart = startDate,
            PeriodEnd = endDate,
            PeriodType = "Custom",

            SportsbookRevenue = hourlyRecords.Sum(r => r.SportsbookNetRevenue),
            SportsbookVolume = hourlyRecords.Sum(r => r.SportsbookVolume),
            SportsbookBetsCount = hourlyRecords.Sum(r => r.SportsbookBetsSettled),
            SportsbookHoldPercentage = CalculateHoldPercentage(
                hourlyRecords.Sum(r => r.SportsbookNetRevenue),
                hourlyRecords.Sum(r => r.SportsbookVolume)
            ),

            ExchangeRevenue = hourlyRecords.Sum(r => r.ExchangeCommissionRevenue),
            ExchangeVolume = hourlyRecords.Sum(r => r.ExchangeVolume),
            ExchangeMatchesCount = hourlyRecords.Sum(r => r.ExchangeMatchesSettled),
            ExchangeEffectiveRate = CalculateEffectiveRate(
                hourlyRecords.Sum(r => r.ExchangeCommissionRevenue),
                hourlyRecords.Sum(r => r.ExchangeVolume)
            ),

            TotalRevenue = hourlyRecords.Sum(r => r.TotalRevenue),
            TotalVolume = hourlyRecords.Sum(r => r.TotalVolume),
            EffectiveMargin = CalculateEffectiveRate(
                hourlyRecords.Sum(r => r.TotalRevenue),
                hourlyRecords.Sum(r => r.TotalVolume)
            )
        });
    }

    /// <summary>
    /// Get hourly breakdown for a specific date
    /// </summary>
    /// <param name="date">Date to get hourly breakdown for</param>
    /// <returns>List of hourly revenue summaries</returns>
    [HttpGet("hourly")]
    public async Task<ActionResult<List<RevenueSummary>>> GetHourlyBreakdown(
        [FromQuery] DateTime date)
    {
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        var hourlyRecords = await _context.HouseRevenue
            .Where(r =>
                r.PeriodStart >= dayStart &&
                r.PeriodStart < dayEnd &&
                r.PeriodType == "Hourly"
            )
            .OrderBy(r => r.PeriodStart)
            .ToListAsync();

        return Ok(hourlyRecords.Select(r => r.GetSummary()).ToList());
    }

    /// <summary>
    /// Get revenue comparison: Book vs Exchange
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>Comparison of book and exchange revenue</returns>
    [HttpGet("comparison")]
    public async Task<ActionResult<object>> GetRevenueComparison(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var hourlyRecords = await _context.HouseRevenue
            .Where(r =>
                r.PeriodStart >= startDate &&
                r.PeriodStart < endDate &&
                r.PeriodType == "Hourly"
            )
            .ToListAsync();

        var sportsbookRevenue = hourlyRecords.Sum(r => r.SportsbookNetRevenue);
        var exchangeRevenue = hourlyRecords.Sum(r => r.ExchangeCommissionRevenue);
        var totalRevenue = sportsbookRevenue + exchangeRevenue;

        return Ok(new
        {
            PeriodStart = startDate,
            PeriodEnd = endDate,
            Sportsbook = new
            {
                Revenue = sportsbookRevenue,
                Percentage = totalRevenue > 0 ? (sportsbookRevenue / totalRevenue) * 100 : 0,
                Volume = hourlyRecords.Sum(r => r.SportsbookVolume),
                BetsCount = hourlyRecords.Sum(r => r.SportsbookBetsSettled),
                HoldPercentage = CalculateHoldPercentage(
                    sportsbookRevenue,
                    hourlyRecords.Sum(r => r.SportsbookVolume)
                )
            },
            Exchange = new
            {
                Revenue = exchangeRevenue,
                Percentage = totalRevenue > 0 ? (exchangeRevenue / totalRevenue) * 100 : 0,
                Volume = hourlyRecords.Sum(r => r.ExchangeVolume),
                MatchesCount = hourlyRecords.Sum(r => r.ExchangeMatchesSettled),
                EffectiveRate = CalculateEffectiveRate(
                    exchangeRevenue,
                    hourlyRecords.Sum(r => r.ExchangeVolume)
                )
            },
            Total = new
            {
                Revenue = totalRevenue,
                Volume = hourlyRecords.Sum(r => r.TotalVolume),
                EffectiveMargin = CalculateEffectiveRate(
                    totalRevenue,
                    hourlyRecords.Sum(r => r.TotalVolume)
                )
            }
        });
    }

    private static decimal CalculateHoldPercentage(decimal revenue, decimal volume)
    {
        return volume > 0 ? (revenue / volume) * 100 : 0;
    }

    private static decimal CalculateEffectiveRate(decimal commission, decimal volume)
    {
        return volume > 0 ? (commission / volume) * 100 : 0;
    }
}
