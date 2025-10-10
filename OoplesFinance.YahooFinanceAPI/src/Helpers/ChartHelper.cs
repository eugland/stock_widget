namespace OoplesFinance.YahooFinanceAPI.Helpers;

internal class ChartHelper : YahooJsonBase
{
    /// <summary>
    /// Parses the raw json data for the Chart data
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="jsonData"></param>
    /// <returns></returns>
    internal override IEnumerable<T> ParseYahooJsonData<T>(string jsonData)
    {
        var root = JsonConvert.DeserializeObject<ChartRoot>(jsonData)?.Chart.Result.FirstOrDefault();
        var timestamps = root?.Timestamp ?? [];
        var quotes = root?.Indicators?.Quote?.FirstOrDefault();
        
        var clean = timestamps
            .Select((t, i) => new
            {
                Time = t.FromUnixTimeStamp(),
                Open = quotes?.Open?.ElementAtOrDefault(i),
                High = quotes?.High?.ElementAtOrDefault(i),
                Low = quotes?.Low?.ElementAtOrDefault(i),
                Close = quotes?.Close?.ElementAtOrDefault(i),
                Volume = quotes?.Volume?.ElementAtOrDefault(i)
            })
            // Remove entries with missing key data (e.g. no close)
            .Where(x => x.Close.HasValue && x.Open.HasValue && x.High.HasValue && x.Low.HasValue && x.Volume.HasValue)
            .ToList();

        var result = new ChartInfo
        {
            DateList = clean.Select(x => x.Time).ToList(),
            OpenList = clean.Select(x => x.Open!.Value).ToList(),
            HighList = clean.Select(x => x.High!.Value).ToList(),
            LowList = clean.Select(x => x.Low!.Value).ToList(),
            CloseList = clean.Select(x => x.Close!.Value).ToList(),
            VolumeList = clean.Select(x => (long)x.Volume!.Value).ToList()
        };

        if (result.DateList.Count == 0 || result.CloseList.Count == 0 || result.OpenList.Count == 0 || result.HighList.Count == 0 || 
            result.VolumeList.Count == 0 || result.LowList.Count == 0)
        {
            throw new InvalidOperationException("Requested Information Not Available On Yahoo Finance");
        }

        return new[] { result }.Cast<T>();
    }
}