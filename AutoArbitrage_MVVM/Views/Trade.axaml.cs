using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AutoArbitrage_MVVM.Services;
using AutoArbitrage_MVVM.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using MySql.Data.MySqlClient;

namespace AutoArbitrage_MVVM.Views;

public partial class Trade : UserControl, INotifyPropertyChanged
{
    private string connectionString = "Server=autoarbitrage.cri2yu04sa9j.me-central-1.rds.amazonaws.com;Database=userdb;User ID=admin;Password=autoarbitrage12;";
    
    private string? _email;
    public string? Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T backingField, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(backingField, value)) return false;
        backingField = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    public Trade()
    {
        InitializeComponent();
        DataContext = new TradeViewModel();
        
        UserService.Instance.OnEmailChanged += (sender, args) =>
        {
            Email = UserService.Instance.Email;
            Console.WriteLine($"TradeViewModel loaded with email: {Email}");
        };

        Email = UserService.Instance.Email;
        
        Unlock();
    }

    private async Task Unlock()
    {
        Console.WriteLine("Start Unlock");
        
        string query = "SELECT binance_key, bybit_key, binance_secret, bybit_secret FROM users WHERE email = @Email LIMIT 1";

        string? bin_key = null;
        string? byb_key = null;
        string? bin_sec = null;
        string? byb_sec = null;

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
                            bin_key = reader["binance_key"] as string;
                            byb_key = reader["bybit_key"] as string;
                            bin_sec = reader["binance_secret"] as string;
                            byb_sec = reader["bybit_secret"] as string;

                            Console.WriteLine($"Retrieved keys and secrets for email {Email}: Binance Key - {bin_key}, Bybit Key - {byb_key}");
                        }
                        else
                        {
                            Console.WriteLine($"No keys found for email {Email}.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error retrieving data: {ex.Message}");
                }
            }
        }

        if (string.IsNullOrEmpty(byb_key) || string.IsNullOrEmpty(bin_key) ||
            string.IsNullOrEmpty(bin_sec) || string.IsNullOrEmpty(byb_sec))
        {
            LockBox.Opacity = 15;
            Lock.ZIndex = 3;
            LockBox.ZIndex = 3;
            LockText.ZIndex = 3;
        }

        else
        {
            Lock.Opacity = 0;
            LockBox.Opacity = 0;
            LockText.Opacity = 0;
            Lock.ZIndex = -1;
            LockBox.ZIndex = -1;
            LockText.ZIndex = -1;
        }
    }

    private void Threshold_OnGotFocus(object? sender, GotFocusEventArgs e)
    {
        Threshold.Foreground = Brushes.White;
    }

    private void Threshold_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        Threshold.Foreground = Brushes.Black;
    }

    private void Size_OnGotFocus(object? sender, GotFocusEventArgs e)
    {
        Size.Foreground = Brushes.White;
    }

    private void Size_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        Size.Foreground = Brushes.Black;
    }

    private void Frequency_OnGotFocus(object? sender, GotFocusEventArgs e)
    {
        Frequency.Foreground = Brushes.White;
    }

    private void Frequency_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        Frequency.Foreground = Brushes.Black;
    }
}
