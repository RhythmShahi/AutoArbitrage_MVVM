using System;
using System.Threading.Tasks;
using AutoArbitrage_MVVM.Services;
using Org.BouncyCastle.Bcpg.Attr;

namespace AutoArbitrage_MVVM.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MySql.Data.MySqlClient;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string Salt { get; set; }
    public string? BinanceKey { get; set; }
    public string? BybitKey { get; set; }
    public string? FullName { get; set; }
    public string? Country { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; }
}

public partial class ProfileViewModel : ObservableObject
{
    private string connectionString = "Server=database-1.c1auqyeukz94.me-central-1.rds.amazonaws.com;Database=userdb;User ID=admin;Password=autoarbitrage12;";

    private string? _email;
    public string? Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }
    
    // Observable Objects
    [ObservableProperty] private string displayEmail;

    [ObservableProperty] private string name;
    
    [ObservableProperty] private string phone;
    
    [ObservableProperty] private string country;

    [ObservableProperty] private string joined;

    [ObservableProperty] private string binanceKey;

    [ObservableProperty] private string bybitKey;
    
    public ProfileViewModel()
    {
        UserService.Instance.OnEmailChanged += (sender, args) =>
        {
            Email = UserService.Instance.Email;
            Console.WriteLine($"TradeViewModel loaded with email: {Email}");
        };

        Email = UserService.Instance.Email;

        DisplayInfo();
    }
    
    
    private async Task<User?> GetInfo()
    {
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
                                FullName = reader.IsDBNull(reader.GetOrdinal("fullname")) ? null : reader.GetString(reader.GetOrdinal("fullname")),
                                Country = reader.IsDBNull(reader.GetOrdinal("country")) ? null : reader.GetString(reader.GetOrdinal("country")),
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
            DisplayEmail = $"{user.Email}";
            Name = $"{user.FullName}";
            Phone = $"{user.Phone}";
            BinanceKey = $"{user.BinanceKey}";
            BybitKey = $"{user.BybitKey}";
            Joined = $"Joined {await FormatDate(user.CreatedAt)}";
        }
        else
        {
            Console.WriteLine("No user found.");
        }
    }

    private async Task<string> FormatDate(DateTime dateTime)
    {
        // Get the day, month name, and year
        int day = dateTime.Day;
        string monthName = dateTime.ToString("MMMM"); // Gets the full month name
        int year = dateTime.Year;

        // Determine the ordinal suffix
        string suffix;
        if (day % 10 == 1 && day % 100 != 11) suffix = "st";
        else if (day % 10 == 2 && day % 100 != 12) suffix = "nd";
        else if (day % 10 == 3 && day % 100 != 13) suffix = "rd";
        else suffix = "th";

        // Format the final string
        string formattedDate = $"{day}{suffix} {monthName}, {year}";

        return formattedDate;
    }

    
    
    
}