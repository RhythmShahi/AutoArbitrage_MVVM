using System;
using System.Threading;
using System.Threading.Tasks;
using AutoArbitrage_MVVM.ViewModels;
using Avalonia;
using Avalonia.Controls;


namespace AutoArbitrage_MVVM.Views;

public partial class Wallet : UserControl
{
    public Wallet()
    {
        InitializeComponent();
        DataContext = new WalletViewModel();
    }
            
}