using AutoArbitrage_MVVM.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace AutoArbitrage_MVVM.Views;

public partial class Login : Window
{
    public Login()
    {
        InitializeComponent();
        DataContext = new LoginViewModel(this);
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
}