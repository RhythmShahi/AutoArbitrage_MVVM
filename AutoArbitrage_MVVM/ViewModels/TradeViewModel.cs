﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoArbitrage_MVVM.Services;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Binance.Net;
using Binance.Net.Clients;
using Bybit.Net;
using Bybit.Net.Clients;
using Bybit.Net.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.CommonObjects;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using MySql.Data.MySqlClient;

namespace AutoArbitrage_MVVM.ViewModels;

public partial class TradeViewModel : ObservableObject
{
    // ***Non-Observable Properties**
    private WebSocket _binanceFRWebSocket;
    
    private static readonly HttpClient client = new HttpClient();

    private BinanceSocketClient _binanceSocketClient;
    private BybitSocketClient _bybitSocketClient;
    
    private decimal _btcPreviousPrice_binance;
    private decimal _ethPreviousPrice_binance;
    
    private decimal _btcPreviousPrice_bybit;
    private decimal _ethPreviousPrice_bybit;
    
    private DateTime _nextTargetTime;
    
    private static readonly TimeSpan[] TargetTimes =
    {
        new TimeSpan(4, 0, 0), // 4:00 AM
        new TimeSpan(12, 0, 0), // 12:00 PM
        new TimeSpan(20, 0, 0) // 8:00 PM
    };

    private static readonly string apiUrl = "https://api.bybit.com/v5/market/tickers";

    private string _selectedCurrency;
    
    private decimal _btcPrice_binance;
    private decimal _ethPrice_binance;
    
    private decimal _btcPrice_bybit;
    private decimal _ethPrice_bybit;
    
    private string connectionString = "Server=autoarbitrage.cri2yu04sa9j.me-central-1.rds.amazonaws.com;Database=userdb;User ID=admin;Password=autoarbitrage12;";
    
    private string? _email;
    public string? Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }
    
    private bool _isRunning = false;

    // ***Observable Properties***
    
    // Binance
    [ObservableProperty] 
    private decimal _binancePrice;

    [ObservableProperty] 
    private string _binanceAskPrice;
    
    [ObservableProperty] 
    private string _binanceAskQuantity;
    
    [ObservableProperty] 
    private string _binanceBidPrice;
    
    [ObservableProperty] 
    private string _binanceBidQuantity;

    [ObservableProperty] 
    private string _binanceFundingRate;

    [ObservableProperty] 
    private string _bPaymentFrom;
    
    [ObservableProperty] 
    private string _bPaymentTo;
    
    [ObservableProperty] 
    private string _bArrow;
    
    [ObservableProperty]
    private IBrush _bPaymentFromColor;

    [ObservableProperty]
    private IBrush _bPaymentToColor;
    
    [ObservableProperty]
    private IBrush _binancePriceColor = Brushes.Gray;
    
    [ObservableProperty]
    private double _binanceUpArrowOpacity = 0;

    [ObservableProperty]
    private double _binanceDownArrowOpacity = 0;
    
    [ObservableProperty]
    private decimal _binancePreviousPrice;
    
    
    // Bybit
    [ObservableProperty] 
    private decimal _bybitPrice;
    
    [ObservableProperty] 
    private string _bybitAskPrice;
    
    [ObservableProperty] 
    private string _bybitAskQuantity;
    
    [ObservableProperty] 
    private string _bybitBidPrice;
    
    [ObservableProperty] 
    private string _bybitBidQuantity;
    
    [ObservableProperty] 
    private string _bybitFundingRate;
    
    [ObservableProperty] 
    private string _byPaymentFrom;
    
    [ObservableProperty] 
    private string _byPaymentTo;
    
    [ObservableProperty] 
    private string _byArrow;
    
    [ObservableProperty]
    private IBrush _byPaymentFromColor;

    [ObservableProperty]
    private IBrush _byPaymentToColor;
    
    [ObservableProperty]
    private IBrush _bybitPriceColor = Brushes.Gray;
    
    [ObservableProperty]
    private double _bybitUpArrowOpacity = 0;

    [ObservableProperty]
    private double _bybitDownArrowOpacity = 0;
    
    [ObservableProperty]
    private decimal _bybitPreviousPrice;
    
    // General
    [ObservableProperty] 
    private string _countdown;

    [ObservableProperty] 
    private string _threshold;
    
    [RelayCommand]
    public async Task StartDiscrepancyCheckerAsync()
    {
        _isRunning = true;
        await Task.Delay(100);
        while (_isRunning)
        {
            await CheckPriceDiscrepancyAsync();
            await Task.Delay(1000);
        }
    }
    
    [RelayCommand]
    public void StopDiscrepancyChecker()
    {
        _isRunning = false;
    }

    public string SelectedCurrency
    {
        get => _selectedCurrency;
        set
        {
            SetProperty(ref _selectedCurrency, value, nameof(SelectedCurrency));
            OnCurrencyChanged();

            // Update the database with the new selected currency
            _ = SetCurrency(value);
        }
    }

    public List<string> CurrencyOptions { get; }
    
    public TradeViewModel()
    {
        _binanceSocketClient = new BinanceSocketClient();
        _bybitSocketClient = new BybitSocketClient();

        CurrencyOptions = new List<string> { "Bitcoin", "Ethereum" };

        UserService.Instance.OnEmailChanged += (sender, args) =>
        {
            Email = UserService.Instance.Email;
            Console.WriteLine($"TradeViewModel loaded with email: {Email}");
        };

        Email = UserService.Instance.Email;
        
        _selectedCurrency = "Bitcoin";

        InitializeWebSocketsAsync();
        InitializeCurrency();

        StartDiscrepancyCheckerAsync();
    }
    
    public async Task InitializeCurrency()
    {
        string dbCurrency = await GetCurrency();
        if (dbCurrency != null)
        {
            SelectedCurrency = dbCurrency;
            OnPropertyChanged(nameof(SelectedCurrency)); // Ensures ComboBox updates
        }
        else
        {
            Console.WriteLine("No currency set in database; using default.");
        }
    }
    
    
    private async Task SetCurrency(string currency)
    {
        
        string updateQuery = "UPDATE current_currency SET currency = @currency WHERE email = @Email";
        string insertQuery = "INSERT INTO current_currency (currency, email) VALUES (@currency, @Email)";

        using (var connection = new MySqlConnection(connectionString))
        {
            await connection.OpenAsync();

            using (var updateCommand = new MySqlCommand(updateQuery, connection))
            {
                updateCommand.Parameters.AddWithValue("@currency", currency);
                updateCommand.Parameters.AddWithValue("@Email", Email);

                try
                {
                    // Try to update the currency
                    int rowsAffected = await updateCommand.ExecuteNonQueryAsync();

                    // If no rows were updated, insert a new row with the email and currency
                    if (rowsAffected == 0)
                    {
                        using (var insertCommand = new MySqlCommand(insertQuery, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@currency", currency);
                            insertCommand.Parameters.AddWithValue("@Email", Email);
                            await insertCommand.ExecuteNonQueryAsync();
                        }
                    }

                    Console.WriteLine($"Current currency '{currency}' set in database for email {Email}.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error setting current currency: {ex.Message}");
                }
            }
        }
    }


    
    private async Task<string> GetCurrency()
    {
        string query = "SELECT currency FROM current_currency WHERE email = @Email LIMIT 1";
        string currency = null;

        using (var connection = new MySqlConnection(connectionString))
        {
            await connection.OpenAsync();

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Email", Email);

                try
                {
                    // Execute the query and read the result asynchronously
                    var result = await command.ExecuteScalarAsync();
                    currency = result?.ToString();
                    Console.WriteLine($"Retrieved currency '{currency}' from database for email {Email}.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error retrieving current currency: {ex.Message}");
                }
            }
        }

        return currency;
    }

    
    
    private async Task InitializeWebSocketsAsync()
    {
        var currencies = new[] { "BTCUSDT", "ETHUSDT" };

        var tasks = new List<Task>
        {
            BinanceFuturesSocketAsync(currencies[0]),
            BinanceFuturesSocketAsync(currencies[1]),
            BybitFuturesSocketAsync(currencies[0]),
            BybitFuturesSocketAsync(currencies[1]),
            ConnectToBinanceFundingRate(currencies[0]),
            ConnectToBinanceFundingRate(currencies[1]),
            ConnectToBybitFundingRate(currencies[0]),
            ConnectToBybitFundingRate(currencies[1])
        };

        await Task.WhenAll(tasks); 
    }

    private string OnCurrencyChanged()
    {
        if (_selectedCurrency == "Bitcoin")
        {
            Console.WriteLine("Selected currency: Bitcoin (BTCUSDT)");
            BinancePrice = _btcPrice_binance;
            BybitPrice = _btcPrice_bybit;
            return "BTCUSDT";
        }
        else if (_selectedCurrency == "Ethereum")
        {
            Console.WriteLine("Selected currency: Ethereum (ETHUSDT)");
            BinancePrice = _ethPrice_binance;
            BybitPrice = _ethPrice_bybit;
            return "ETHUSDT";
        }

        return null;
    }

    private async Task BinanceFuturesSocketAsync(string currency)
    {
        var tickerSubscriptionResult =
            await _binanceSocketClient.UsdFuturesApi.ExchangeData.SubscribeToTickerUpdatesAsync(currency,
                (update) =>
                {
                    var newPrice = update.Data.LastPrice;

                    // Keep track of the previous price for comparison
                    if (currency == "BTCUSDT")
                    {
                        BinanceChangeColor(newPrice, _btcPreviousPrice_binance);
                        _btcPreviousPrice_binance = newPrice;

                        // Only update BinancePreviousPrice if Bitcoin is selected
                        if (SelectedCurrency == "Bitcoin")
                        {
                            BinancePreviousPrice = _btcPreviousPrice_binance;
                        }
                    }
                    else if (currency == "ETHUSDT")
                    {
                        BinanceChangeColor(newPrice, _ethPreviousPrice_binance);
                        _ethPreviousPrice_binance = newPrice;

                        // Only update BinancePreviousPrice if Ethereum is selected
                        if (SelectedCurrency == "Ethereum")
                        {
                            BinancePreviousPrice = _ethPreviousPrice_binance;
                        }
                    }

                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (SelectedCurrency == "Bitcoin")
                        {
                            BinancePrice = _btcPreviousPrice_binance;
                        }
                        else if (SelectedCurrency == "Ethereum")
                        {
                            BinancePrice = _ethPreviousPrice_binance;
                        }
                    });
                });

        var tickerOrderBook = _binanceSocketClient.UsdFuturesApi.ExchangeData.SubscribeToOrderBookUpdatesAsync(currency,
            500,
            (update) =>
            {
                var orderBook = update.Data;
                
                var askPrice = new StringBuilder();
                var askQuantity = new StringBuilder();
                var bidPrice = new StringBuilder();
                var bidQuantity = new StringBuilder();
                
                foreach (var ask in orderBook.Asks.Take(5))
                {
                    askPrice.AppendLine($"{ask.Price}");
                    askQuantity.AppendLine($"{ask.Quantity}");
                }

                var bids = orderBook.Bids
                    .TakeLast(5)
                    .OrderByDescending(bid => bid.Price);

                foreach (var bid in bids)
                {
                    bidPrice.AppendLine($"{bid.Price}");
                    bidQuantity.AppendLine($"{bid.Quantity}");
                }

                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (SelectedCurrency == "Bitcoin" && currency == "BTCUSDT")
                    {
                        BinanceAskPrice = askPrice.ToString();
                        BinanceAskQuantity = askQuantity.ToString();
                        BinanceBidPrice = bidPrice.ToString();
                        BinanceBidQuantity = bidQuantity.ToString();
                    }
                    else if (SelectedCurrency == "Ethereum" && currency == "ETHUSDT")
                    {
                        BinanceAskPrice = askPrice.ToString();
                        BinanceAskQuantity = askQuantity.ToString();
                        BinanceBidPrice = bidPrice.ToString();
                        BinanceBidQuantity = bidQuantity.ToString();
                    }
                });
            });
    }
    
    private Task ConnectToBinanceFundingRate(string currency)
    {
        string urlCurrency = currency.ToLower();
        return Task.Run(() =>
        {
            FundingRateTimer();
            string binanceWebSocketUrl = $"wss://fstream.binance.com/ws/{urlCurrency}@markPrice";

            _binanceFRWebSocket = new WebSocket(binanceWebSocketUrl);
            _binanceFRWebSocket.OnMessage += BinanceWebSocket_OnMessage;
            _binanceFRWebSocket.OnOpen += BinanceWebSocket_OnOpen;
            _binanceFRWebSocket.OnClose += BinanceWebSocket_OnClose;

            _binanceFRWebSocket.Connect();
        });
    }

    private void BinanceWebSocket_OnMessage(object sender, WebSocketSharp.MessageEventArgs e)
    {
        // Parse the message data
        var data = JObject.Parse(e.Data);
        var fundingRate = data["r"]?.ToObject<double>() ?? 0.0;
        var fundingAmount = data["p"]?.ToString();

        // Convert funding rate to percentage
        var fundingRatePercentage = fundingRate * 100;

        // Determine the payment direction and colors
        string paymentFrom;
        string paymentTo;
        var paymentFromColor = Brushes.Gray;
        var paymentToColor = Brushes.Gray;
        string arrowText;

        if (fundingRate > 0)
        {
            paymentFrom = "Longs";
            paymentTo = "Shorts";
            paymentFromColor = Brushes.DarkGreen;
            paymentToColor = Brushes.DarkRed;
            arrowText = "--->";
        }
        else if (fundingRate < 0)
        {
            paymentFrom = "Shorts";
            paymentTo = "Longs";
            paymentFromColor = Brushes.DarkRed;
            paymentToColor = Brushes.DarkGreen;
            arrowText = "--->";
        }
        else
        {
            paymentFrom = "No payment";
            paymentTo = "(Rate is 0)";
            paymentFromColor = Brushes.Gray;
            paymentToColor = Brushes.Gray;
            arrowText = "";
        }

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (SelectedCurrency == "Bitcoin")
            {
                BinanceFundingRate = $"{fundingRatePercentage:F4}%";
                BPaymentFrom = paymentFrom;
                BPaymentTo = paymentTo;
                BArrow = arrowText;
                BPaymentFromColor = paymentFromColor;
                BPaymentToColor = paymentToColor;
            }
            else if (SelectedCurrency == "Ethereum")
            {
                BinanceFundingRate = $"{fundingRatePercentage:F4}%";
                BPaymentFrom = paymentFrom;
                BPaymentTo = paymentTo;
                BArrow = arrowText;
                BPaymentFromColor = paymentFromColor;
                BPaymentToColor = paymentToColor;
            }
        });
    }
    
    private void BinanceWebSocket_OnOpen(object sender, EventArgs e)
    {
        Console.WriteLine("Connected to Binance WebSocket.");
    }

    private void BinanceWebSocket_OnClose(object sender, WebSocketSharp.CloseEventArgs e)
    {
        Console.WriteLine("Disconnected from Binance WebSocket.");
    }

    private async Task BybitFuturesSocketAsync(string currency)
    {
        var tickerSubscriptionResult =
            await _bybitSocketClient.V5LinearApi.SubscribeToTickerUpdatesAsync(currency,
                (update) =>
                {
                    var newPrice = (decimal)update.Data.LastPrice;

                    // Keep track of the previous price for comparison
                    if (currency == "BTCUSDT")
                    {
                        BybitChangeColor(newPrice, _btcPreviousPrice_bybit);
                        _btcPreviousPrice_bybit = newPrice;

                        // Only update BybitPreviousPrice if Bitcoin is selected
                        if (SelectedCurrency == "Bitcoin")
                        {
                            BybitPreviousPrice = _btcPreviousPrice_bybit;
                        }
                    }
                    else if (currency == "ETHUSDT")
                    {
                        BybitChangeColor(newPrice, _ethPreviousPrice_bybit);
                        _ethPreviousPrice_bybit = newPrice;

                        // Only update BybitPreviousPrice if Ethereum is selected
                        if (SelectedCurrency == "Ethereum")
                        {
                            BybitPreviousPrice = _ethPreviousPrice_bybit;
                        }
                    }

                    // Update UI-bound properties
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (SelectedCurrency == "Bitcoin")
                        {
                            BybitPrice = _btcPreviousPrice_bybit; // Use Bitcoin's price
                        }
                        else if (SelectedCurrency == "Ethereum")
                        {
                            BybitPrice = _ethPreviousPrice_bybit; // Use Ethereum's price
                        }
                    });
                });
        
        var tickerOrderBook = _bybitSocketClient.V5LinearApi.SubscribeToOrderbookUpdatesAsync(currency, 500,
            (update) =>
            {
                var orderBook = update.Data;
                
                var askPrice = new StringBuilder();
                var askQuantity = new StringBuilder();
                var bidPrice = new StringBuilder();
                var bidQuantity = new StringBuilder();
                
                foreach (var ask in orderBook.Asks.Take(5))
                {
                    askPrice.AppendLine($"{ask.Price}");
                    askQuantity.AppendLine($"{ask.Quantity}");
                }

                var bids = orderBook.Bids
                    .TakeLast(5)
                    .OrderByDescending(bid => bid.Price);

                foreach (var bid in bids)
                {
                    bidPrice.AppendLine($"{bid.Price}");
                    bidQuantity.AppendLine($"{bid.Quantity}");
                }

                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (SelectedCurrency == "Bitcoin" && currency == "BTCUSDT")
                    {
                        BybitAskPrice = askPrice.ToString();
                        BybitAskQuantity = askQuantity.ToString();
                        BybitBidPrice = bidPrice.ToString();
                        BybitBidQuantity = bidQuantity.ToString();
                    }
                    else if (SelectedCurrency == "Ethereum" && currency == "ETHUSDT")
                    {
                        BybitAskPrice = askPrice.ToString();
                        BybitAskQuantity = askQuantity.ToString();
                        BybitBidPrice = bidPrice.ToString();
                        BybitBidQuantity = bidQuantity.ToString();
                    }
                });
            });
    
    }

    public async Task ConnectToBybitFundingRate(string currency)
    {
        // Set the symbol and limit
        string symbol = currency.ToUpper();
        int limit = 1; // Limit to get only the most recent data

        // Build the URL with query parameters
        string url = $"{apiUrl}?category=linear&symbol={symbol}";

        // Create an HttpClient instance
        using (HttpClient client = new HttpClient())
        {
            try
            {
                // Send an HTTP GET request to the Bybit API
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                // Parse the response content
                string responseBody = await response.Content.ReadAsStringAsync();

                JObject json = JObject.Parse(responseBody);

                // Extract funding rate from the API response
                var fundingData = json["result"]?["list"]?[0];
                if (fundingData != null)
                {
                    // Get the funding rate directly
                    double fundingRate = fundingData["fundingRate"]?.ToObject<double>() ?? 0.0;

                    // Convert to percentage if needed
                    double fundingRatePercentage = fundingRate * 100; // This was modified to reflect correct scaling

                    string paymentFrom;
                    string paymentTo;
                    var paymentFromColor = Brushes.Gray;
                    var paymentToColor = Brushes.Gray;
                    string arrowText;

                    if (fundingRate > 0)
                    {
                        paymentFrom = "Longs";
                        paymentTo = "Shorts";
                        paymentFromColor = Brushes.DarkGreen;
                        paymentToColor = Brushes.DarkRed;
                        arrowText = "--->";
                    }
                    else if (fundingRate < 0)
                    {
                        paymentFrom = "Shorts";
                        paymentTo = "Longs";
                        paymentFromColor = Brushes.DarkRed;
                        paymentToColor = Brushes.DarkGreen;
                        arrowText = "--->";
                    }
                    else
                    {
                        paymentFrom = "No payment";
                        paymentTo = "(Rate is 0)";
                        paymentFromColor = Brushes.Gray;
                        paymentToColor = Brushes.Gray;
                        arrowText = "";
                    }

                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (SelectedCurrency == "Bitcoin")
                        {
                            BybitFundingRate = $"{fundingRatePercentage:F4}%";
                            ByPaymentFrom = paymentFrom;
                            ByPaymentTo = paymentTo;
                            ByArrow = arrowText;
                            ByPaymentFromColor = paymentFromColor;
                            ByPaymentToColor = paymentToColor;
                        }
                        else if (SelectedCurrency == "Ethereum")
                        {
                            BybitFundingRate = $"{fundingRatePercentage:F4}%";
                            ByPaymentFrom = paymentFrom;
                            ByPaymentTo = paymentTo;
                            ByArrow = arrowText;
                            ByPaymentFromColor = paymentFromColor;
                            ByPaymentToColor = paymentToColor;
                        }
                    });
                }
                else
                {
                    Console.WriteLine("No funding history data available.");
                }
            }
            catch (HttpRequestException e)
            {
                // Handle any HTTP request errors
                Console.WriteLine($"Request error: {e.Message}");
            }
        }
    }
    
    private void BinanceChangeColor(decimal newPrice, decimal previousPrice)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Set color based on price comparison
            if (newPrice >= previousPrice)
            {
                BinancePriceColor = Brushes.DarkGreen;
                BinanceUpArrowOpacity = 100;   // Show up arrow
                BinanceDownArrowOpacity = 0; // Hide down arrow
            }
            else if (newPrice < previousPrice)
            {
                BinancePriceColor = Brushes.DarkRed;
                BinanceUpArrowOpacity = 0;   // Hide up arrow
                BinanceDownArrowOpacity = 100; // Show down arrow
            }
            else
            {
                BinancePriceColor = Brushes.Gray; // Neutral color for no change
            }
        });
    }

// Bybit Change Color logic
    private void BybitChangeColor(decimal newPrice, decimal previousPrice)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Set color based on price comparison
            if (newPrice >= previousPrice)
            {
                BybitPriceColor = Brushes.DarkGreen;
                BybitUpArrowOpacity = 100;   // Show up arrow
                BybitDownArrowOpacity = 0; // Hide down arrow
            }
            else if (newPrice < previousPrice)
            {
                BybitPriceColor = Brushes.DarkRed;
                BybitUpArrowOpacity = 0;   // Hide up arrow
                BybitDownArrowOpacity = 100; // Show down arrow
            }
            else
            {
                BybitPriceColor = Brushes.Gray; // Neutral color for no change
            }
        });
    }
    

    private void FundingRateTimer()
        {
            DispatcherTimer.Run(() =>
            {
                UpdateCountdown();
                return true; // Keep running
            }, TimeSpan.FromSeconds(1));
        }

        private void UpdateCountdown()
        {
            var now = DateTime.Now;
            var timeRemaining = _nextTargetTime - now;

            if (timeRemaining.TotalSeconds <= 0)
            {
                UpdateNextTargetTime();
                timeRemaining = _nextTargetTime - now;
            }

            Countdown = FormatTimeRemaining(timeRemaining);
        }

        private void UpdateNextTargetTime()
        {
            var now = DateTime.Now.TimeOfDay;
            foreach (var target in TargetTimes)
            {
                if (now < target)
                {
                    _nextTargetTime = DateTime.Today.Add(target);
                    return;
                }
            }

            _nextTargetTime = DateTime.Today.Add(TargetTimes[0]).AddDays(1);
        }

        private string FormatTimeRemaining(TimeSpan timeSpan)
        {
            return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }
        
        private async Task CheckPriceDiscrepancyAsync()
        {
            // Assume these prices are updated in real-time from the Bybit and Binance WebSocket connections
            decimal bybitPrice = BybitPrice;
            decimal binancePrice = BinancePrice;

            // Check if there is a discrepancy of $5 or more
            decimal discrepancy = Math.Abs(bybitPrice - binancePrice);
            if (discrepancy >= decimal.Parse(Threshold))
            {
                // Determine which exchange has the higher and lower price
                string higherExchange, lowerExchange;
                decimal higherPrice, lowerPrice;
                bool isBybitHigher = bybitPrice > binancePrice;

                if (isBybitHigher)
                {
                    higherExchange = "Bybit";
                    higherPrice = bybitPrice;
                    lowerExchange = "Binance";
                    lowerPrice = binancePrice;
                }
                else
                {
                    higherExchange = "Binance";
                    higherPrice = binancePrice;
                    lowerExchange = "Bybit";
                    lowerPrice = bybitPrice;
                }

                // Log the discrepancy details
                Console.WriteLine($"Found discrepancy of {discrepancy} dollars");

                // Indicate where to buy and sell based on the price difference
                Console.WriteLine($"{higherExchange}: {higherPrice} dollars : sell/short position");
                Console.WriteLine($"{lowerExchange}: {lowerPrice} dollars : buy/long position");

                // Retrieve the best prices from the order books for the lower and higher exchanges
                var bybitAskPrices = BybitAskPrice?.Split("\n");
                var bybitBestAskPrice = !string.IsNullOrEmpty(bybitAskPrices?.LastOrDefault())
                    ? bybitAskPrices.Last()
                    : bybitAskPrices?.SkipLast(1).LastOrDefault();
                
                var bybitBestBidPrice = BybitBidPrice?.Split("\n").FirstOrDefault(); // Best price for selling on Bybit
                
                var binanceAskPrices = BinanceAskPrice?.Split("\n");
                var binanceBestAskPrice = !string.IsNullOrEmpty(binanceAskPrices?.LastOrDefault())
                    ? binanceAskPrices.Last()
                    : binanceAskPrices?.SkipLast(1).LastOrDefault();

                var binanceBestBidPrice = BinanceBidPrice?.Split("\n").FirstOrDefault(); // Best price for selling on Binance

                // Determine the entry prices based on whether Bybit or Binance is the buy/sell exchange
                string longEntryPrice, shortEntryPrice, actualDiscrepancy;

                if (isBybitHigher)
                {
                    // Short Bybit, Long Binance
                    shortEntryPrice = bybitBestBidPrice ?? bybitPrice.ToString();
                    longEntryPrice = binanceBestAskPrice ?? binancePrice.ToString();
                    actualDiscrepancy = (binancePrice - bybitPrice).ToString();

                    Console.WriteLine($"Held long position on Binance, entry price = {longEntryPrice}");
                    Console.WriteLine($"Held short position on Bybit, entry price = {shortEntryPrice}");
                    Console.WriteLine($"Actual discrepancy = {actualDiscrepancy}");
                }
                else
                {
                    // Short Binance, Long Bybit
                    shortEntryPrice = binanceBestBidPrice ?? binancePrice.ToString();
                    longEntryPrice = bybitBestAskPrice ?? bybitPrice.ToString();
                    actualDiscrepancy = (bybitPrice - binancePrice).ToString();

                    Console.WriteLine($"Held long position on Bybit, entry price = {longEntryPrice}");
                    Console.WriteLine($"Held short position on Binance, entry price = {shortEntryPrice}");
                    Console.WriteLine($"Actual discrepancy = {actualDiscrepancy}");
                }
            }
        }

        public static readonly string Bybit_API_KEY = "aVcZaYygMMUqRwDT4J";
        public static readonly string Bybit_API_SECRET = "Hsv944KqPncici7DdHBOVuIcSwvypPn9jVnt";
        public static readonly BybitEnvironment By_env = BybitEnvironment.Testnet;
        
        public static readonly string Binance_API_KEY = "099c7a9c36082ba8e20062661f17e291e5bc5b92f8b6c5fd00e0887628b5d113";
        public static readonly string Binance_API_SECRET = "01be6c5ae8f4c57aafceb0e241ca0ecc38fe61f2801311c8ed98b3465d813b9f";
        public static readonly BinanceEnvironment Bin_env = BinanceEnvironment.Testnet;

        [ObservableProperty] 
        private string _quantity;
        
        private async Task OpenBybitPositions()
        {
            var currency = OnCurrencyChanged();
            
            var bybitClient = new BybitRestClient(options =>
            {
                options.ApiCredentials = new ApiCredentials(Bybit_API_KEY, Bybit_API_SECRET);
                options.Environment = By_env;
            });
            
            var newOrder = await bybitClient.V5Api.Trading.PlaceOrderAsync(
                category: Category.Linear,
                symbol: currency,
                side: OrderSide.Buy,
                type: NewOrderType.Market,
                quantity: decimal.Parse(Quantity),
                timeInForce: TimeInForce.ImmediateOrCancel
            );
            
            if (!newOrder.Success)
            {
                Console.WriteLine($"Failed to create an order! Error: {newOrder.Error.Message}");
                return;
            }

            Console.WriteLine($"Successful order creation! Order ID: {newOrder.Data.OrderId}\n");
            
            var positions = await bybitClient.V5Api.Trading.GetPositionsAsync(
                category: Category.Linear,
                settleAsset: "USDT"
            );
            var positionList = positions.Data.List.ToList();
            Console.WriteLine($"Found a total of {positionList.Count} positions.\n");

            var position = positionList.FirstOrDefault(z => z.Symbol == currency);
            if (position == null)
            {
                Console.WriteLine("Bybit Position was not found!");
                return;
            }
        }
        
        private async Task OpenBinancePositions()
        {
            var currency = OnCurrencyChanged();
            
            var binanceClient = new BinanceRestClient(options =>
            {
                options.ApiCredentials = new ApiCredentials(Binance_API_KEY, Binance_API_SECRET);
                options.Environment = Bin_env;
            });
            
            var newOrder = await binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(
                symbol: currency,
                side: Binance.Net.Enums.OrderSide.Buy,
                type: Binance.Net.Enums.FuturesOrderType.Market,
                quantity: decimal.Parse(Quantity),
                positionSide: Binance.Net.Enums.PositionSide.Long,
                timeInForce: Binance.Net.Enums.TimeInForce.ImmediateOrCancel
            );
            
            if (!newOrder.Success)
            {
                Console.WriteLine($"Failed to create an order! Error: {newOrder.Error.Message}");
                return;
            }

            Console.WriteLine($"Successful order creation! Order ID: {newOrder.Data.Id}\n");
            
            var positions = await binanceClient.UsdFuturesApi.Trading.GetPositionsAsync();
            
            var positionList = positions.Data.ToList();
            Console.WriteLine($"Found a total of {positionList.Count} positions.\n");

            var position = positionList.FirstOrDefault(z => z.Symbol == currency);
            if (position == null)
            {
                Console.WriteLine("Binance Position was not found!");
                return;
            }
        }
        
        

}

