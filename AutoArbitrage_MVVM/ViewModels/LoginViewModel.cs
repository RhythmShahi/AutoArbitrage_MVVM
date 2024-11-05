using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AutoArbitrage_MVVM.Services;
using AutoArbitrage_MVVM.Views;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MySql.Data.MySqlClient;

namespace AutoArbitrage_MVVM.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private string connectionString = "Server=autoarbitrage.cri2yu04sa9j.me-central-1.rds.amazonaws.com;Database=userdb;User ID=admin;Password=autoarbitrage12;";
    
    [ObservableProperty] 
    private string email;

    [ObservableProperty] 
    private string password;
    
    [ObservableProperty] 
    private string errorMessage;
    
    private Window currentWindow;
    
    public LoginViewModel(Window window)
    {
        currentWindow = window;
    }

    [RelayCommand]
    public async Task Login()
    {
        if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "Please fill in all fields.";
            return;
        }

        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            // Retrieve the salt and password hash from the database
            string query = "SELECT password_hash, salt FROM users WHERE email = @Email";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Email", Email);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
            {
                ErrorMessage = "Invalid email or password.";
                return;
            }

            await reader.ReadAsync();
            var storedHash = reader.GetString(reader.GetOrdinal("password_hash"));
            var storedSalt = reader.GetString(reader.GetOrdinal("salt"));

            // Hash the entered password with the stored salt
            var enteredHash = HashPassword(Password, storedSalt);

            if (enteredHash != storedHash)
            {
                ErrorMessage = "Invalid email or password.";
                return;
            }

            // Successful login
            ErrorMessage = string.Empty;
            var mainWindow = new MainWindow();
            mainWindow.Show();
            Dispatcher.UIThread.InvokeAsync(() => currentWindow.Close());
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        
        UserService.Instance.Email = this.Email;
        Console.WriteLine(UserService.Instance.Email);
    }

    // Hash the password with the salt
    private static string HashPassword(string password, string salt)
    {
        using var sha256 = SHA256.Create();
        var saltedPassword = salt + password;
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }
    
    public void ToSignUp()
    {
        var signupWindow = new SignUp();
        signupWindow.Show();

        Dispatcher.UIThread.InvokeAsync(() => currentWindow.Close());
    }

}