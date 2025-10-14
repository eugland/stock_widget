namespace OoplesFinance.YahooFinanceAPI.Models;

public class ChartData
{
    [JsonProperty("result")]
    public List<ChartResult> Result { get; set; } = [];

    [JsonProperty("error")]
    public object Error { get; set; } = new();
}

public class TradingPeriod
{
    [JsonProperty("timezone")]
    public string Timezone { get; set; } = string.Empty;

    [JsonProperty("end")]
    public int? End { get; set; }

    [JsonProperty("start")]
    public int? Start { get; set; }

    [JsonProperty("gmtoffset")]
    public int? Gmtoffset { get; set; }
}

public class ChartQuote
{
    [JsonProperty("open")]
    public List<double?> Open { get; set; } = [];

    [JsonProperty("low")]
    public List<double?> Low { get; set; } = [];

    [JsonProperty("volume")]
    public List<long?> Volume { get; set; } = [];

    [JsonProperty("close")]
    public List<double?> Close { get; set; } = [];

    [JsonProperty("high")]
    public List<double?> High { get; set; } = [];
}

public class ChartResult
{
    [JsonProperty("meta")]
    public Meta? Meta { get; set; }

    [JsonProperty("timestamp")]
    public List<long> Timestamp { get; set; } = [];

    [JsonProperty("indicators")]
    public Indicators? Indicators { get; set; }
}

public class ChartRoot
{
    [JsonProperty("chart")]
    public ChartData Chart { get; set; } = new();
}

public class ChartInfo
{
    public List<DateTime> DateList { get; set; } = [];
    public List<double> OpenList { get; set; } = [];
    public List<double> HighList { get; set; } = [];
    public List<double> LowList { get; set; } = [];
    public List<double> CloseList { get; set; } = [];
    public List<long> VolumeList { get; set; } = [];

    public override string ToString()
    {
        if (DateList.Count == 0)
            return "ChartInfo: (no data)";

        int count = DateList.Count;
        DateTime firstDate = DateList.First();
        DateTime lastDate = DateList.Last();

        double minLow = LowList.Min();
        double maxHigh = HighList.Max();
        double avgClose = CloseList.Average();
        long totalVolume = VolumeList.Sum();

        var sb = new System.Text.StringBuilder();

        // --- Summary ---
        sb.AppendLine("=== ChartInfo Summary ===");
        sb.AppendLine($"Points      : {count}");
        sb.AppendLine($"Date Range  : {firstDate} → {lastDate}");
        sb.AppendLine($"Price Range : Low={minLow}  High={maxHigh}");
        sb.AppendLine($"Avg Close   : {avgClose:F2}");
        sb.AppendLine($"Total Volume: {totalVolume}");
        sb.AppendLine();

        // --- Header (aligned widths) ---
        sb.AppendLine(
            $"{"Date",-20}" +
            $"{"Open",10}" +
            $"{"High",10}" +
            $"{"Low",10}" +
            $"{"Close",10}" +
            $"{"Volume",12}"
        );

        // --- Rows ---
        for (int i = 0; i < count; i++)
        {
            sb.AppendLine(
                $"{DateList[i],-20:yyyy-MM-dd HH:mm}" +
                $"{OpenList[i],10:F3}" +
                $"{HighList[i],10:F3}" +
                $"{LowList[i],10:F3}" +
                $"{CloseList[i],10:F3}" +
                $"{VolumeList[i],12}"
            );
        }

        return sb.ToString();
    }
}

