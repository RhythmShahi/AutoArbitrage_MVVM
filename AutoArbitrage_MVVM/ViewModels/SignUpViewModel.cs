using System;
using System.Threading.Tasks;
using AutoArbitrage_MVVM.Views;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;


namespace AutoArbitrage_MVVM.ViewModels;

public partial class SignUpViewModel : ObservableObject
{
    [ObservableProperty] 
    private string email;
    
    [ObservableProperty] 
    private string password;
    
    [ObservableProperty] 
    private string confirmPassword;
    
    [ObservableProperty] 
    private string binanceKey;
    
    [ObservableProperty] 
    private string bybitKey;

    [ObservableProperty] 
    private string errorMessage;

    private Window currentWindow;
    

    public SignUpViewModel(Window window)
    {
        currentWindow = window;
    }
    
    [RelayCommand]
    public void SignUp()
    {
        var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        
        if (string.IsNullOrEmpty(Email) ||
            string.IsNullOrEmpty(Password) ||
            string.IsNullOrEmpty(ConfirmPassword) ||
            string.IsNullOrEmpty(BinanceKey) ||
            string.IsNullOrEmpty(BybitKey))
        {
            ErrorMessage = "Please fill in all fields.";
            return;
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "The passwords do not match. Please try again.";
            return;
        }
        
        if (!emailRegex.IsMatch(Email))
        {
            ErrorMessage = "Please enter a valid email address.";
            return;
        }
        
        if (password.Length < 8)
        {
            ErrorMessage = "Password must be at least 8 characters long.";
            return;
        }
        
        
        ErrorMessage = string.Empty;
        var mainWindow = new MainWindow();
        mainWindow.Show();
        
        Dispatcher.UIThread.InvokeAsync(() => currentWindow.Close());
        
    }

    [RelayCommand]
    public void ToLogin()
    {
        var loginWindow = new Login();
        loginWindow.Show();

        Dispatcher.UIThread.InvokeAsync(() => currentWindow.Close());
    }
    
}