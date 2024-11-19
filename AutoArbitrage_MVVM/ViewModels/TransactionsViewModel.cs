using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using AutoArbitrage_MVVM.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoArbitrage_MVVM.ViewModels
{
    public class Order
    {
        public string Price { get; set; }
        public string Quantity { get; set; }
        public string Exchange { get; set; }
        public string Currency { get; set; }
        public string Action { get; set; }
        public string Profit { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
    }

    public partial class TransactionsViewModel : ObservableObject
    {
        private string connectionString = "Server=autoarbitrage.cri2yu04sa9j.me-central-1.rds.amazonaws.com;Database=userdb;User ID=admin;Password=autoarbitrage12;";

        private string? _email;
        public string? Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private ObservableCollection<Order> _orders = new();
        public ObservableCollection<Order> Orders
        {
            get => _orders;
            set => SetProperty(ref _orders, value); // Notify changes to the UI
        }

        [ObservableProperty] private string binanceKey;
        [ObservableProperty] private string binanceSecret;
        [ObservableProperty] private string bybitKey;
        [ObservableProperty] private string bybitSecret;
        

        public TransactionsViewModel()
        {
            // Initialize Email from UserService
            Email = UserService.Instance.Email;

            // Register for email updates
            UserService.Instance.OnEmailChanged += (sender, args) =>
            {
                Email = UserService.Instance.Email;
                Console.WriteLine($"TransactionsViewModel loaded with email: {Email}");
                Task.Run(() => FetchTransactionData()); // Fetch transactions whenever email changes
            };

            
            GetAPICreds();
            // Fetch data initially
            Task.Run(() => FetchTransactionData());
            
        }

        
        
        private async Task FetchTransactionData()
        {
            if (string.IsNullOrEmpty(Email))
            {
                Console.WriteLine("Email is not set. Unable to fetch transaction data.");
                return;
            }

            string query = @"
    SELECT order_id, price, quantity, exchange, currency, action, profit, date, time, email
    FROM transactions
    WHERE price <> 0 AND email = @Email";

            try
            {
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Email", Email);

                using var reader = await command.ExecuteReaderAsync();
                var fetchedOrders = new ObservableCollection<Order>();

                while (await reader.ReadAsync())
                {
                    fetchedOrders.Add(new Order
                    {
                        Price = reader.GetDecimal(reader.GetOrdinal("price")).ToString("F8"),
                        Quantity = reader.GetDecimal(reader.GetOrdinal("quantity")).ToString("F8"),
                        Exchange = reader.GetString(reader.GetOrdinal("exchange")),
                        Currency = reader.GetString(reader.GetOrdinal("currency")),
                        Action = reader.GetString(reader.GetOrdinal("action")),
                        Profit = reader.IsDBNull(reader.GetOrdinal("profit")) ? "0" : reader.GetDecimal(reader.GetOrdinal("profit")).ToString("F8"),
                        Date = reader.GetDateTime(reader.GetOrdinal("date")).ToString("yyyy-MM-dd"),
                        Time = ((TimeSpan)reader["time"]).ToString(@"hh\:mm\:ss")
                    });
                }

                Orders = fetchedOrders; // Update observable collection
                Console.WriteLine($"Fetched {Orders.Count} transactions for email {Email}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching transaction data: {ex.Message}");
            }
        }
        
        public async Task GetAPICreds()
        {
            if (string.IsNullOrEmpty(Email))
            {
                Console.WriteLine("Email is not set. Unable to fetch API credentials.");
                return;
            }

            string query = "SELECT binance_key, binance_secret, bybit_key, bybit_secret FROM users WHERE email = @Email";

            try
            {
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Email", Email);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    BinanceKey = reader.IsDBNull(reader.GetOrdinal("binance_key")) ? string.Empty : reader.GetString(reader.GetOrdinal("binance_key"));
                    BinanceSecret = reader.IsDBNull(reader.GetOrdinal("binance_secret")) ? string.Empty : reader.GetString(reader.GetOrdinal("binance_secret"));
                    BybitKey = reader.IsDBNull(reader.GetOrdinal("bybit_key")) ? string.Empty : reader.GetString(reader.GetOrdinal("bybit_key"));
                    BybitSecret = reader.IsDBNull(reader.GetOrdinal("bybit_secret")) ? string.Empty : reader.GetString(reader.GetOrdinal("bybit_secret"));

                    Console.WriteLine("API credentials fetched successfully.");
                }
                else
                {
                    Console.WriteLine($"No user found with email {Email}.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching API credentials: {ex.Message}");
            }
        }
    }    
}
