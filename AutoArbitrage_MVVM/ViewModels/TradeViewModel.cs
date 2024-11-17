using System;
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

    [ObservableProperty] 
    private string _frequency;
    
    [RelayCommand]
    public async Task StartDiscrepancyCheckerAsync()
    {
        _isRunning = true;
        await Task.Delay(100);
        while (_isRunning)
        {
            await CheckPriceDiscrepancyAsync();
            await Task.Delay(int.Parse(Frequency));
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
        // Define fee rates
        decimal binanceTakerFeeRate = 0.0005m; // 0.0500% taker fee rate
        decimal bybitTakerFeeRate = 0.00055m;   // 0.055% taker fee rate

        decimal bybitPrice = BybitPrice;
        decimal binancePrice = BinancePrice;
        decimal discrepancy = Math.Abs(bybitPrice - binancePrice);

        // Check if the discrepancy is greater than or equal to the threshold
        if (discrepancy >= decimal.Parse(Threshold))
        {
            string higherExchange, lowerExchange;
            decimal higherPrice, lowerPrice;
            bool isBybitHigher = bybitPrice > binancePrice;

            decimal totalFees = 0;

            if (isBybitHigher)
            {
                higherExchange = "Bybit";
                higherPrice = bybitPrice;
                lowerExchange = "Binance";
                lowerPrice = binancePrice;

                decimal orderQuantity = decimal.Parse(Quantity); // Adjust according to your order quantity

                // Calculate the order values
                decimal bybitOrderValue = orderQuantity / bybitPrice;
                decimal binanceOrderValue = orderQuantity / binancePrice;

                // Calculate taker fees for Bybit and maker fees for Binance
                decimal bybitTakerFee = bybitOrderValue * bybitTakerFeeRate;
                decimal binanceTakerFee = binanceOrderValue * binanceTakerFeeRate;

                // Total fees are the sum of the taker fee (Bybit) and the maker fee (Binance)
                totalFees = bybitTakerFee + binanceTakerFee;
            }
            else
            {
                higherExchange = "Binance";
                higherPrice = binancePrice;
                lowerExchange = "Bybit";
                lowerPrice = bybitPrice;

                decimal orderQuantity = decimal.Parse(Quantity); // Adjust according to your order quantity

                // Calculate the order values
                decimal binanceOrderValue = orderQuantity / binancePrice;
                decimal bybitOrderValue = orderQuantity / bybitPrice;

                // Calculate maker fee for Binance and taker fee for Bybit
                decimal binanceTakerFee = binanceOrderValue * binanceTakerFeeRate;
                decimal bybitTakerFee = bybitOrderValue * bybitTakerFeeRate;

                // Total fees are the sum of the taker fee (Binance) and the maker fee (Bybit)
                totalFees = binanceTakerFee + bybitTakerFee;
            }

            // Adjust the discrepancy by subtracting total fees
            decimal netDiscrepancy = discrepancy - totalFees;

            // Proceed only if the net discrepancy meets or exceeds the threshold
            if (netDiscrepancy >= decimal.Parse(Threshold))
            {
                Console.WriteLine("\n");
                Console.WriteLine($"Found net discrepancy of {netDiscrepancy} dollars after fees");
                Console.WriteLine($"{higherExchange}: {higherPrice} dollars : Sell/Short position");
                Console.WriteLine($"{lowerExchange}: {lowerPrice} dollars : Buy/Long position");

                await HandleDiscrepancyAndTradeAsync(isBybitHigher);
                
                await MonitorAndClosePositionAsync(isBybitHigher, decimal.Parse(Threshold) / 2);
            }
        }
    }
    
    private async Task MonitorAndClosePositionAsync(bool isBybitHigher, decimal closeThreshold)
    {
        decimal currentDiscrepancy;
    
        // Monitor until the discrepancy falls below the close threshold
        do
        {
            decimal bybitPrice = BybitPrice;
            decimal binancePrice = BinancePrice;
            currentDiscrepancy = Math.Abs(bybitPrice - binancePrice);

            // Wait for a short interval before rechecking
            await Task.Delay(500);
        }
        while (currentDiscrepancy >= closeThreshold);

        Console.WriteLine($"Discrepancy fell below {closeThreshold}. Closing positions.");

        // Close the open positions once the discrepancy is below the close threshold
        await ClosePositionsAsync(isBybitHigher);
    }

    private async Task ClosePositionsAsync(bool isBybitHigher)
    {
        // If Bybit was higher at the opening, now close the Bybit short and Binance long
        if (isBybitHigher)
        {
            await OpenCloseBybitPositions(OrderSide.Buy); // Close short on Bybit
            await OpenCloseBinancePositions(Binance.Net.Enums.OrderSide.Sell); // Close long on Binance
        }
        // If Binance was higher at the opening, close the Binance short and Bybit long
        else
        {
            await OpenCloseBybitPositions(OrderSide.Sell); // Close long on Bybit
            await OpenCloseBinancePositions(Binance.Net.Enums.OrderSide.Buy); // Close short on Binance
        }
    }

    private async Task HandleDiscrepancyAndTradeAsync(bool isBybitHigher)
    {
        // If Bybit is higher, short Bybit and long Binance
        if (isBybitHigher)
        {
            await OpenCloseBybitPositions(OrderSide.Sell);
            await OpenCloseBinancePositions(Binance.Net.Enums.OrderSide.Buy);
        }
        // If Binance is higher, short Binance and long Bybit
        else
        {
            await OpenCloseBybitPositions(OrderSide.Buy);
            await OpenCloseBinancePositions(Binance.Net.Enums.OrderSide.Sell);
        }
    }

    public static readonly string Bybit_API_KEY = "aVcZaYygMMUqRwDT4J";
    public static readonly string Bybit_API_SECRET = "Hsv944KqPncici7DdHBOVuIcSwvypPn9jVnt";
    public static readonly BybitEnvironment By_env = BybitEnvironment.Testnet;
    
    public static readonly string Binance_API_KEY = "e84bee016bbe783cc1a965bd189c2ca18fe0686449a6dce0eb9546800632d3f3";
    public static readonly string Binance_API_SECRET = "ba3d8443f3ce00335d91feca0a0ffa1af41ad2c7dbbae0184b10b2453a3ae1c3";
    public static readonly BinanceEnvironment Bin_env = BinanceEnvironment.Testnet;

    [ObservableProperty] 
    private string _quantity;

    private async Task OpenCloseBybitPositions(OrderSide side)
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
            side: side,
            type: NewOrderType.Market,
            quantity: decimal.Parse(Quantity),
            timeInForce: TimeInForce.ImmediateOrCancel
        );

        if (!newOrder.Success)
        {
            Console.WriteLine($"Failed to create an order on Bybit! Error: {newOrder.Error.Message}");
            return;
        }

        Console.WriteLine($"Opened {side} position on Bybit. Order ID: {newOrder.Data.OrderId}");
        await SetTransactionData(newOrder.Data.OrderId, BybitPrice, decimal.Parse(Quantity), "Bybit", currency, side.ToString());
    }
    
    private async Task OpenCloseBinancePositions(Binance.Net.Enums.OrderSide side)
    {
        var currency = OnCurrencyChanged();

        var binanceClient = new BinanceRestClient(options =>
        {
            options.ApiCredentials = new ApiCredentials(Binance_API_KEY, Binance_API_SECRET);
            options.Environment = Bin_env;
        });

        // Set position side explicitly based on the order side
        var positionSide = side == Binance.Net.Enums.OrderSide.Buy 
            ? Binance.Net.Enums.PositionSide.Long 
            : Binance.Net.Enums.PositionSide.Short;

        var newOrder = await binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(
            symbol: currency,
            side: side,
            type: Binance.Net.Enums.FuturesOrderType.Market,
            quantity: decimal.Parse(Quantity),
            positionSide: positionSide
        );

        if (!newOrder.Success)
        {
            Console.WriteLine($"Failed to create an order on Binance! Error: {newOrder.Error.Message}");
            return;
        }

        Console.WriteLine($"Opened {side} position on Binance. Order ID: {newOrder.Data.Id}");

        await SetTransactionData(newOrder.Data.Id.ToString(), BinancePrice, decimal.Parse(Quantity), "Binance", currency, side.ToString());
    }

    private async Task SetTransactionData(string orderId, decimal price, decimal quantity, string exchange, string currency, string action, decimal? profit = null)
    {
        string insertQuery = @"
        INSERT INTO transactions 
        (order_id, price, quantity, exchange, currency, action, profit, date, time, email) 
        VALUES 
        (@orderId, @price, @quantity, @exchange, @currency, @action, @profit, @date, @time, @Email)";

        using (var connection = new MySqlConnection(connectionString))
        {
            await connection.OpenAsync();

            using (var command = new MySqlCommand(insertQuery, connection))
            {
                // Adding parameters to prevent SQL injection
                command.Parameters.AddWithValue("@orderId", orderId);
                command.Parameters.AddWithValue("@price", price);
                command.Parameters.AddWithValue("@quantity", quantity);
                command.Parameters.AddWithValue("@exchange", exchange);
                command.Parameters.AddWithValue("@currency", currency);
                command.Parameters.AddWithValue("@action", action);
                command.Parameters.AddWithValue("@profit", profit.HasValue ? (object)profit.Value : DBNull.Value);
                command.Parameters.AddWithValue("@date", DateTime.UtcNow.Date);
                command.Parameters.AddWithValue("@time", DateTime.UtcNow.TimeOfDay);
                command.Parameters.AddWithValue("@Email", Email);

                try
                {
                    await command.ExecuteNonQueryAsync();
                    Console.WriteLine($"Transaction recorded for {exchange} on {currency}: {action} position, Order ID: {orderId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error recording transaction: {ex.Message}");
                }
            }
        }
    }

    

}

