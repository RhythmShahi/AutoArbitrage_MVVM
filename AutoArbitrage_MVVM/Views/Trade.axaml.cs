using AutoArbitrage_MVVM.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AutoArbitrage_MVVM.Views;

public partial class Trade : UserControl
{
    public Trade()
    {
        InitializeComponent();
        DataContext = new TradeViewModel();
    }
    
}