namespace WebViewWidget.service;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OoplesFinance.YahooFinanceAPI;
using OoplesFinance.YahooFinanceAPI.Enums;


using OoplesFinance.YahooFinanceAPI;

public interface IStockDataService
{
    YahooClient Client { get; }
}

public sealed class YahooStockDataService : IStockDataService
{
    public YahooClient Client { get; } = new();
}