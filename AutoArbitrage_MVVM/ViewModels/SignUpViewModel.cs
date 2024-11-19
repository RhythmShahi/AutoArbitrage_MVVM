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
using BCrypt.Net;

namespace AutoArbitrage_MVVM.ViewModels
{
    public partial class SignUpViewModel : ObservableObject
    {
        private string connectionString = "Server=autoarbitrage.cri2yu04sa9j.me-central-1.rds.amazonaws.com;Database=userdb;User ID=admin;Password=autoarbitrage12;";

        [ObservableProperty] private string email;

        [ObservableProperty] private string password;

        [ObservableProperty] private string confirmPassword;

        [ObservableProperty] private string fullName;

        [ObservableProperty] private string phoneNumber;

        [ObservableProperty] private string errorMessage;

        private Window currentWindow;

        public SignUpViewModel(Window window)
        {
            currentWindow = window;
        }

        [RelayCommand]
        public async Task SignUp()
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password) ||
                string.IsNullOrEmpty(ConfirmPassword) ||
                string.IsNullOrEmpty(FullName) || string.IsNullOrEmpty(PhoneNumber))
            {
                ErrorMessage = "Please fill in all fields.";
                return;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "The passwords do not match. Please try again.";
                return;
            }

            if (Password.Length < 8)
            {
                ErrorMessage = "Password must be at least 8 characters long.";
                return;
            }

            try
            {
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Check if the email already exists
                string checkUserQuery = "SELECT COUNT(*) FROM users WHERE email = @Email";
                using (var checkCmd = new MySqlCommand(checkUserQuery, connection))
                {
                    checkCmd.Parameters.AddWithValue("@Email", Email);
                    var count = (long)await checkCmd.ExecuteScalarAsync();
                    if (count > 0)
                    {
                        ErrorMessage = "Email already exists. Please use a different email.";
                        return;
                    }
                }

                // Generate a salt and hash the password
                var salt = GenerateSalt();
                var hashedPassword = HashPassword(Password, salt);

                // Insert the new user with the hashed password and salt
                string insertUserQuery = @"INSERT INTO users (email, password_hash, salt, fullname, phone) 
                                           VALUES (@Email, @PasswordHash, @Salt, @FullName, @PhoneNumber)";
                using (var insertCmd = new MySqlCommand(insertUserQuery, connection))
                {
                    insertCmd.Parameters.AddWithValue("@Email", Email);
                    insertCmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                    insertCmd.Parameters.AddWithValue("@Salt", salt);
                    insertCmd.Parameters.AddWithValue("@FullName", FullName);
                    insertCmd.Parameters.AddWithValue("@PhoneNumber", PhoneNumber);
                    await insertCmd.ExecuteNonQueryAsync();
                }

                // Clear error message and open main window after successful registration
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
            
        }

        // Generate a unique salt for each user
        private static string GenerateSalt()
        {
            var rng = new RNGCryptoServiceProvider();
            byte[] saltBytes = new byte[32];
            rng.GetBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }

        // Hash the password using SHA256 with the provided salt
        private static string HashPassword(string password, string salt)
        {
            using var sha256 = SHA256.Create();
            var saltedPassword = salt + password;
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }


        [RelayCommand]
        public void ToLogin()
        {
            var loginWindow = new Login();
            loginWindow.Show();

            Dispatcher.UIThread.InvokeAsync(() => currentWindow.Close());
        }

    }
}
