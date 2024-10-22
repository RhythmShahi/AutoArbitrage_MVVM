using AutoArbitrage_MVVM.Views;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoArbitrage_MVVM.ViewModels;

public partial class LoginViewModel : ObservableObject
{

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
    public void Login()
    {
        if (string.IsNullOrEmpty(Email) ||
            string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "Please fill in all fields.";
            return;
        }
        
        ErrorMessage = string.Empty;
        var mainWindow = new MainWindow();
        mainWindow.Show();
        
        Dispatcher.UIThread.InvokeAsync(() => currentWindow.Close());
        
    }
    
    public void ToSignUp()
    {
        var signupWindow = new SignUp();
        signupWindow.Show();

        Dispatcher.UIThread.InvokeAsync(() => currentWindow.Close());
    }

}