using System;
using System.Diagnostics;
using AutoArbitrage_MVVM.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace AutoArbitrage_MVVM.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
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
    
    private void Trade_OnClick(object? sender, RoutedEventArgs e)
    {
        Trade.Background = new SolidColorBrush(Color.Parse("#173055"));
        Wallet.Background = Brushes.Transparent;
        PastTransactions.Background = Brushes.Transparent;
        Assistant.Background = Brushes.Transparent;
        Settings.Background = Brushes.Transparent;
        Profile.Background = Brushes.Transparent;
    }

    private void Wallet_OnClick(object? sender, RoutedEventArgs e)
    {
        Trade.Background = Brushes.Transparent;
        Wallet.Background = new SolidColorBrush(Color.Parse("#173055"));
        PastTransactions.Background = Brushes.Transparent;
        Assistant.Background = Brushes.Transparent;
        Settings.Background = Brushes.Transparent;;
        Profile.Background = Brushes.Transparent;
    }

    private void PastTransactions_OnClick(object? sender, RoutedEventArgs e)
    {
        Trade.Background = Brushes.Transparent;
        Wallet.Background = Brushes.Transparent;
        PastTransactions.Background = new SolidColorBrush(Color.Parse("#173055"));
        Assistant.Background = Brushes.Transparent;
        Settings.Background = Brushes.Transparent;;
        Profile.Background = Brushes.Transparent;
    }
    
    private void Assistant_OnClick(object? sender, RoutedEventArgs e)
    {
        
        Trade.Background = Brushes.Transparent;
        Wallet.Background = Brushes.Transparent;
        PastTransactions.Background = Brushes.Transparent;
        Assistant.Background = new SolidColorBrush(Color.Parse("#173055"));
        Settings.Background = Brushes.Transparent;
        Profile.Background = Brushes.Transparent;
    }

    private void Settings_OnClick(object? sender, RoutedEventArgs e)
    {
        Trade.Background = Brushes.Transparent;
        Wallet.Background = Brushes.Transparent;
        PastTransactions.Background = Brushes.Transparent;
        Assistant.Background = Brushes.Transparent;
        Settings.Background = new SolidColorBrush(Color.Parse("#173055"));
        Profile.Background = Brushes.Transparent;
    }

    private void Profile_OnClick(object? sender, RoutedEventArgs e)
    {
        
        Trade.Background = Brushes.Transparent;
        Wallet.Background = Brushes.Transparent;
        PastTransactions.Background = Brushes.Transparent;
        Assistant.Background = Brushes.Transparent;
        Settings.Background = Brushes.Transparent;
        Profile.Background = new SolidColorBrush(Color.Parse("#173055"));
    }
}