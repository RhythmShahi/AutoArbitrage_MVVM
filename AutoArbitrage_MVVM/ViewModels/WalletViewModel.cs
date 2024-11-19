using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Clients;
using Bybit.Net;
using Bybit.Net.Clients;
using Bybit.Net.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoExchange.Net.Authentication;
using DynamicData.Kernel;
using Google.Protobuf.WellKnownTypes;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Extensions;

namespace AutoArbitrage_MVVM.ViewModels;

public partial class WalletViewModel : ObservableObject
{
    public static readonly string Bybit_API_KEY = "aVcZaYygMMUqRwDT4J";
    public static readonly string Bybit_API_SECRET = "Hsv944KqPncici7DdHBOVuIcSwvypPn9jVnt";
    public static readonly BybitEnvironment By_env = BybitEnvironment.Testnet;

    public static readonly string Binance_API_KEY = "e84bee016bbe783cc1a965bd189c2ca18fe0686449a6dce0eb9546800632d3f3";
    public static readonly string Binance_API_SECRET = "ba3d8443f3ce00335d91feca0a0ffa1af41ad2c7dbbae0184b10b2453a3ae1c3";
    public static readonly BinanceEnvironment Bin_env = BinanceEnvironment.Testnet;
    
    
    public IEnumerable<ISeries> Series { get; set; } =
        new[] { 2, 4, 1, 4, 3 }.AsPieSeries((value, series) =>
        {
            series.MaxRadialColumnWidth = 60;
        });

    [ObservableProperty] 
    private string _bybitBalance;

    [ObservableProperty] 
    private string _bybitEquity;
    
    [ObservableProperty]
    private List<ISeries> _bybitSeries = new();

    [ObservableProperty] 
    private string _binanceBalance;

    [ObservableProperty] 
    private string _binanceEquity;

    [ObservableProperty]
    private List<ISeries> _binanceSeries = new();
    
    [ObservableProperty] private string binanceKey;
    [ObservableProperty] private string binanceSecret;
    [ObservableProperty] private string bybitKey;
    [ObservableProperty] private string bybitSecret;
    

    public WalletViewModel()
    {
        GetBybitBalance();
        GetBinanceBalance();
    }
    
    private int GetSortOrder(string asset)
    {
        // Custom sort order
        var customOrder = new Dictionary<string, int>
        {
            { "BTC", 1 },
            { "ETH", 2 },
            { "USDT", 3 },
            { "USDC", 4 },
            { "Others", int.MaxValue } // Ensure "Others" always comes last
        };

        // If the asset is in the custom order, return its index; otherwise, treat as "Others"
        return customOrder.ContainsKey(asset) ? customOrder[asset] : int.MaxValue - 1;
    }


    private async Task GetBinanceBalance()
    {
        var binanceClient = new BinanceRestClient(options =>
        {
            options.ApiCredentials = new ApiCredentials(Binance_API_KEY, Binance_API_SECRET);
            options.Environment = Bin_env;
        });

        try
        {
            var trans = await binanceClient.UsdFuturesApi.Account.GetBalancesAsync(100000000000000, ct: CancellationToken.None);

            if (!trans.Success)
            {
                Console.WriteLine($"Error: {trans.Error?.Message}");
            }
            else
            {
                var list = trans.Data;

                if (list != null && list.AsList().Count > 0)
                {
                    decimal totalUsdValue = 0;
                    decimal othersUsdValue = 0;

                    BinanceSeries.Clear();

                    // Create a temporary list for storing assets with USD values
                    var assetList = new List<(string Asset, decimal UsdValue)>();

                    foreach (var item in list)
                    {
                        if (item.Asset == "USDT")
                        {
                            Console.WriteLine(item.AvailableBalance);
                            BinanceBalance = $"${item.AvailableBalance:N2}";
                        }

                        if (item.AvailableBalance > 0)
                        {
                            decimal usdValue = 0;

                            // Get the USD price of the asset
                            decimal priceInUsdt = await GetPriceInUsdt(item.Asset);
                            if (priceInUsdt > 0)
                            {
                                usdValue = item.AvailableBalance * priceInUsdt;
                            }

                            // Add assets to the list for sorting
                            if (!new[] { "BTC", "ETH", "USDT", "USDC" }.Contains(item.Asset))
                            {
                                othersUsdValue += usdValue; // Group "BNB" and "FDUSD" under "Others"
                            }
                            else
                            {
                                assetList.Add((item.Asset, usdValue));
                            }

                            totalUsdValue += usdValue;
                        }
                    }

                    // Add "Others" to the list with combined USD value
                    if (othersUsdValue > 0)
                    {
                        assetList.Add(("Others", othersUsdValue));
                    }

                    // Sort assets by custom order
                    var sortedAssets = assetList.OrderBy(a => GetSortOrder(a.Asset)).ToList();

                    // Add sorted assets to the pie chart
                    foreach (var asset in sortedAssets)
                    {
                        BinanceSeries.Add(new PieSeries<decimal>
                        {
                            Values = new List<decimal> { asset.UsdValue },
                            Name = asset.Asset,
                            MaxRadialColumnWidth = 60
                        });
                    }

                    // Set Binance balance and equity
                    BinanceEquity = $"${totalUsdValue:N2}";
                    Console.WriteLine($"Total USD Value: ${totalUsdValue:N2}");
                }
                else
                {
                    Console.WriteLine("No balances available.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching balances: {ex.Message}");
        }
    }



    private async Task<decimal> GetPriceInUsdt(string asset)
    {
        var binanceClient = new BinanceSocketClient();
        
        
        try
        {
            // For assets that are usually stablecoins or have direct USDT pairs
            if (asset == "USDT" || asset == "USDC")
            {
                return 1; // Stablecoins are typically pegged to 1 USD
            }

            // Construct symbol for direct USDT pairs (e.g., BTCUSDT, ETHUSDT)
            var symbol = $"{asset}USDT";
            var priceResult = await binanceClient.SpotApi.ExchangeData.GetCurrentAvgPriceAsync(symbol);

            if (priceResult.Success)
            {
                return priceResult.Data.Result.Price;
            }
            else
            {
                // If direct USDT pair is not available, try to convert via other pairs
                // Try using BTC as an intermediary if no direct pair
                if (asset != "BTC") 
                {
                    // Example: Try BTC as intermediary (asset -> BTC -> USDT)
                    var btcPrice = await GetPriceInUsdt("BTC");
                    if (btcPrice > 0)
                    {
                        // Fetch price of asset in BTC (e.g., BNB -> BTC)
                        var btcSymbol = $"{asset}BTC";
                        var btcToAssetResult = await binanceClient.SpotApi.ExchangeData.GetCurrentAvgPriceAsync(symbol);

                        if (btcToAssetResult.Success)
                        {
                            return btcToAssetResult.Data.Result.Price * btcPrice;
                        }
                        else
                        {
                            Console.WriteLine($"Error fetching price for {asset} in BTC: {btcToAssetResult.Error?.Message}");
                            return 0; // Unable to find conversion
                        }
                    }
                }

                Console.WriteLine($"Error fetching price for {asset}: {priceResult.Error?.Message}");
                return 0; // Return 0 if no price was found
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching price for {asset}: {ex.Message}");
            return 0;
        }
    }

    public async Task GetBybitBalance()
    {
        var bybitClient = new BybitRestClient(options =>
        {
            options.ApiCredentials = new ApiCredentials(Bybit_API_KEY, Bybit_API_SECRET);
            options.Environment = By_env;
        });

        try
        {
            AccountType accountType = AccountType.Unified;
            var result = await bybitClient.V5Api.Account.GetBalancesAsync(accountType);

            if (!result.Success)
            {
                Console.WriteLine($"Error: {result.Error?.Message}");
            }
            else
            {
                if (result?.Data?.List != null && result.Data.List.AsList().Count > 0)
                {
                    decimal totalUsdValue = 0;
                    decimal othersUsdValue = 0;

                    BybitSeries.Clear();

                    // Create a temporary list for storing assets with USD values
                    var assetList = new List<(string Asset, decimal UsdValue)>();

                    foreach (var item in result.Data.List)
                    {
                        var assets = item.Assets.AsList();

                        foreach (var asset in assets)
                        {
                            decimal usdValue = asset.UsdValue ?? 0;

                            // Add assets to the list for sorting
                            if (!new[] { "BTC", "ETH", "USDT", "USDC" }.Contains(asset.Asset))
                            {
                                othersUsdValue += usdValue; // Group "BNB" and "FDUSD" under "Others"
                            }
                            else
                            {
                                assetList.Add((asset.Asset, usdValue));
                            }

                            totalUsdValue += usdValue;
                        }
                    }

                    // Add "Others" to the list with combined USD value
                    if (othersUsdValue > 0)
                    {
                        assetList.Add(("Others", othersUsdValue));
                    }

                    // Sort assets by custom order
                    var sortedAssets = assetList.OrderBy(a => GetSortOrder(a.Asset)).ToList();

                    // Add sorted assets to the pie chart
                    foreach (var asset in sortedAssets)
                    {
                        BybitSeries.Add(new PieSeries<decimal>
                        {
                            Values = new List<decimal> { asset.UsdValue },
                            Name = asset.Asset,
                            MaxRadialColumnWidth = 60
                        });
                    }

                    // Set Bybit balance and equity
                    BybitBalance = $"${totalUsdValue:N2}";
                    BybitEquity = $"${totalUsdValue:N2}";
                    Console.WriteLine($"Total USD Value: ${totalUsdValue:N2}");
                }
                else
                {
                    Console.WriteLine($"No balances available for {accountType} account.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching balances: {ex.Message}");
        }
    }


}