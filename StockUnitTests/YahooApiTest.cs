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
        var chartRoot = await _yahoo.GetChartResults(symbol, range, interval);
        Assert.IsNotNull(chartRoot);
    }
}