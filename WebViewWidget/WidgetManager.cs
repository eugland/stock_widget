namespace WebViewWidget;

public sealed class WidgetManager
{
    private readonly Dictionary<string, StockWidgetWindow> _windows = new();

    public IReadOnlyCollection<string> ActiveSymbols => _windows.Keys.ToList().AsReadOnly();

    public void EnsureWidget(string symbol)
    {
        if (_windows.ContainsKey(symbol))
        {
            _windows[symbol].Show();
            _windows[symbol].Activate();
            return;
        }

        var win = new StockWidgetWindow(symbol);
        _windows[symbol] = win;
        win.Show();
    }

    public void ShowAll()
    {
        foreach (var w in _windows.Values)
        {
            w.Show();
            w.Activate();
        }
    }

    public void HideAll()
    {
        foreach (var w in _windows.Values)
            w.Hide();
    }

    public void CloseAllAndDispose()
    {
        foreach (var w in _windows.Values.ToList())
            w.Close(); // OnClosing() will hide; during app exit we actually want to shutdown
        _windows.Clear();
    }
}