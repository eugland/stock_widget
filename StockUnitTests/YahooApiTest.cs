using OoplesFinance.YahooFinanceAPI;
using OoplesFinance.YahooFinanceAPI.Enums;

namespace StockUnitTests;

[TestClass]
public sealed class YahooApiTest {
    private readonly YahooClient _yahoo = new();

    [TestMethod]
    [TestCategory("Integration")]
    [Timeout(30_000, CooperativeCancellation = true)]
    public async Task GetChartInfo_SPY_1D_1Min_ReturnsValidSeries() {
        const string symbol = "SPY";
        var range = TimeRange._1Day;
        var interval = TimeInterval._1Minute;

        // Act
        // https://query1.finance.yahoo.com/v8/finance/chart/SPY?range=1d&interval=1m
        // They have lots of things we do not have
        var ci = await _yahoo.GetChartInfoAsync(symbol, range, interval);

        // Assert
        Assert.IsNotNull(ci, "ChartInfo is null");
        Assert.IsNotNull(ci.DateList, "ChartInfo is null");
        Assert.IsGreaterThan(0, ci.DateList.Count, "No dates returned");
        Assert.IsGreaterThan(0, ci.CloseList.Count, "No closes returned");

        // Basic shape checks
        Assert.HasCount(ci.DateList.Count, ci.CloseList, "Dates/Closes length mismatch");
        Assert.HasCount(ci.DateList.Count, ci.OpenList, "Dates/Opens length mismatch");
        Assert.HasCount(ci.DateList.Count, ci.HighList, "Dates/Highs length mismatch");
        Assert.HasCount(ci.DateList.Count, ci.LowList, "Dates/Lows length mismatch");
        Assert.HasCount(ci.DateList.Count, ci.VolumeList, "Dates/Volume length mismatch");

        // Sanity on values
        var first = ci.CloseList.First();
        var last = ci.CloseList.Last();
        Assert.IsTrue(first > 0 && last > 0, "Close values should be positive");

        // Monotonic timestamps
        for (var i = 1; i < ci.DateList.Count; i++) {
            Assert.IsTrue(ci.DateList[i] >= ci.DateList[i - 1],
                $"Timestamps not sorted at index {i}");
        }
    }
}