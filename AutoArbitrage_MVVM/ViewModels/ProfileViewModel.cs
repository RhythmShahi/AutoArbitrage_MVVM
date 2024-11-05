using System;
using System.Threading.Tasks;
using AutoArbitrage_MVVM.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MySql.Data.MySqlClient;

namespace AutoArbitrage_MVVM.ViewModels
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Salt { get; set; }
        public string? BinanceKey { get; set; }
        public string? BybitKey { get; set; }
        public string? BinanceSecret { get; set; }
        public string? BybitSecret { get; set; }
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public partial class ProfileViewModel : ObservableObject
    {
        private string connectionString = "Server=autoarbitrage.cri2yu04sa9j.me-central-1.rds.amazonaws.com;Database=userdb;User ID=admin;Password=autoarbitrage12;";

        private string? _email;
        public string? Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private int? _id;
        public int? Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        // Observable Objects
        [ObservableProperty] private string displayEmail;
        [ObservableProperty] private string name;
        [ObservableProperty] private string phone;
        [ObservableProperty] private string joined;
        [ObservableProperty] private string binanceKey;
        [ObservableProperty] private string bybitKey;
        [ObservableProperty] private string binanceSecret;
        [ObservableProperty] private string bybitSecret;

        public ProfileViewModel()
        {
            // Initialize Email immediately if available
            Email = UserService.Instance.Email;

            // Register for email updates
            UserService.Instance.OnEmailChanged += (sender, args) =>
            {
                Email = UserService.Instance.Email;
                Console.WriteLine($"ProfileViewModel loaded with email: {Email}");
            };

            // Call DisplayInfo asynchronously
            Task.Run(() => DisplayInfo());
        }


        [RelayCommand]
        public async Task SaveInfo()
        {
            int userId = await GetIDAsync();
            Console.WriteLine($"Attempting to update user with Id: {userId}");

            if (userId == 0)
            {
                Console.WriteLine("Error: Id is null or invalid, unable to update user information.");
                return;
            }

            try
            {
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Define the SQL query to update user info
                string query = @"
                UPDATE users SET 
                    fullname = @Name,
                    email = @Email,
                    phone = @Phone,
                    binance_key = @BinanceKey,
                    bybit_key = @BybitKey,
                    binance_secret = @BinanceSecret,
                    bybit_secret = @BybitSecret         
                WHERE id = @UserId";

                // Prepare the command with parameters to avoid SQL injection
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Name", Name ?? string.Empty);
                command.Parameters.AddWithValue("@Email", DisplayEmail ?? string.Empty);
                command.Parameters.AddWithValue("@Phone", Phone ?? string.Empty);
                command.Parameters.AddWithValue("@BinanceKey", BinanceKey ?? string.Empty);
                command.Parameters.AddWithValue("@BybitKey", BybitKey ?? string.Empty);
                command.Parameters.AddWithValue("@BinanceSecret", BinanceSecret ?? string.Empty);
                command.Parameters.AddWithValue("@BybitSecret", BybitSecret ?? string.Empty);
                command.Parameters.AddWithValue("@UserId", userId);

                // Execute the update command
                int affectedRows = await command.ExecuteNonQueryAsync();

                if (affectedRows > 0)
                {
                    Console.WriteLine("User information updated successfully.");
                }
                else
                {
                    Console.WriteLine("Failed to update user information. No rows affected.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user information: {ex.Message}");
            }
        }

        private async Task<int> GetIDAsync()
        {
            var user = await GetInfo();
            return user?.Id ?? 0;
        }

        private async Task<User?> GetInfo()
        {
            if (string.IsNullOrEmpty(Email))
            {
                Console.WriteLine("Email is not set. Unable to query the user.");
                return null;
            }
            
            Console.WriteLine($"Attempting to find user with email: {Email}");
            string query = "SELECT * FROM users WHERE email = @Email LIMIT 1";
            User? user = null;

            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", Email);

                    try
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                user = new User
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                                    Email = reader.GetString(reader.GetOrdinal("email")),
                                    PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
                                    Salt = reader.GetString(reader.GetOrdinal("salt")),
                                    BinanceKey = reader.IsDBNull(reader.GetOrdinal("binance_key")) ? null : reader.GetString(reader.GetOrdinal("binance_key")),
                                    BybitKey = reader.IsDBNull(reader.GetOrdinal("bybit_key")) ? null : reader.GetString(reader.GetOrdinal("bybit_key")),
                                    BinanceSecret = reader.IsDBNull(reader.GetOrdinal("binance_secret")) ? null : reader.GetString(reader.GetOrdinal("binance_secret")),
                                    BybitSecret = reader.IsDBNull(reader.GetOrdinal("bybit_secret")) ? null : reader.GetString(reader.GetOrdinal("bybit_secret")),
                                    FullName = reader.IsDBNull(reader.GetOrdinal("fullname")) ? null : reader.GetString(reader.GetOrdinal("fullname")),
                                    Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString(reader.GetOrdinal("phone")),
                                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                                };
                                Console.WriteLine($"Retrieved user info for email {Email}.");
                            }
                            else
                            {
                                Console.WriteLine($"No user found with email {Email}.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error retrieving user info: {ex.Message}");
                    }
                }
            }

            return user;
        }


        private async Task DisplayInfo()
        {
            User? user = await GetInfo();

            if (user != null)
            {
                Id = user.Id;
                DisplayEmail = $"{user.Email}";
                Name = $"{user.FullName}";
                Phone = $"{user.Phone}";
                BinanceKey = $"{user.BinanceKey}";
                BybitKey = $"{user.BybitKey}";
                BinanceSecret = $"{user.BinanceSecret}";
                BybitSecret = $"{user.BybitSecret}";
                Joined = $"Joined {await FormatDate(user.CreatedAt)}";
            }
            else
            {
                Console.WriteLine("No user found.");
            }
        }

        private async Task<string> FormatDate(DateTime dateTime)
        {
            int day = dateTime.Day;
            string monthName = dateTime.ToString("MMMM"); // Gets the full month name
            int year = dateTime.Year;

            string suffix = day % 10 == 1 && day % 100 != 11 ? "st"
                        : day % 10 == 2 && day % 100 != 12 ? "nd"
                        : day % 10 == 3 && day % 100 != 13 ? "rd"
                        : "th";

            return $"{day}{suffix} {monthName}, {year}";
        }
    }
}
