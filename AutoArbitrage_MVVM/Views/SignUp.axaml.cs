using AutoArbitrage_MVVM.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace AutoArbitrage_MVVM.Views;

public partial class SignUp : Window
{
    public SignUp()
    {
        InitializeComponent();
        DataContext = new SignUpViewModel(this);
    }

    private void Min_OnClick(object? sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    private void Stack_OnClick(object? sender, RoutedEventArgs e)
    {
        if (this.WindowState == WindowState.Maximized)
        {
            this.WindowState = WindowState.Normal;
        }
        else
        {
            this.WindowState = WindowState.Maximized;
        }
    }

    private void Close_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
    
    private void Email_OnGotFocus(object? sender, GotFocusEventArgs e)
    {
        Email.Foreground = Brushes.White;
    }

    private void Email_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        Email.Foreground = Brushes.Black;
    }

    private void Password_OnGotFocus(object? sender, GotFocusEventArgs e)
    {
        Password.Foreground = Brushes.White;
    }

    private void Password_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        Password.Foreground = Brushes.Black;
    }

    private void ConfirmPassword_OnGotFocus(object? sender, GotFocusEventArgs e)
    {
        ConfirmPassword.Foreground = Brushes.White;
    }

    private void ConfirmPassword_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        ConfirmPassword.Foreground = Brushes.Black;
    }

    private void FullName_OnGotFocus(object? sender, GotFocusEventArgs e)
    {
        FullName.Foreground = Brushes.White;
    }

    private void FullName_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        FullName.Foreground = Brushes.Black;
    }

    private void PhoneNumber_OnGotFocus(object? sender, GotFocusEventArgs e)
    {
        PhoneNumber.Foreground = Brushes.White;
    }

    private void PhoneNumber_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        PhoneNumber.Foreground = Brushes.Black;
    }
}