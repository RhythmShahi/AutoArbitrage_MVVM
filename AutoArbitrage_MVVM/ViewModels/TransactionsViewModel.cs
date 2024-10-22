using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoArbitrage_MVVM.ViewModels;

public class Order
{
    public string Price { get; set; }
    public string Quantity { get; set; }
    public string Exchange { get; set; }
    public string Currency { get; set; }
    public string Action { get; set; }
    public string Profit { get; set; }
    public string Date { get; set; }
    public string Time { get; set; }
}


public partial class TransactionsViewModel : ObservableObject
{
    public ObservableCollection<Order> Orders { get; set; }
    
    public TransactionsViewModel()
    {
        Orders = new ObservableCollection<Order>
        {
            new Order{Price = "12000", Quantity = "100", Exchange = "Binance", Currency = "BTCUSDT", Action = "Long", Profit = "487", Date = "Wednesday", Time = "12:00"},
            new Order{Price = "12000", Quantity = "100", Exchange = "Binance", Currency = "d", Action = "Long", Profit = "487", Date = "Wednesday", Time = "12:00"},
            new Order{Price = "12000", Quantity = "100", Exchange = "Binance", Currency = "BTCUSDT", Action = "Long", Profit = "487", Date = "Wednesday", Time = "12:00"},
            new Order{Price = "12000", Quantity = "100", Exchange = "Binance", Currency = "dTCUSDT", Action = "Long", Profit = "487", Date = "Wednesday", Time = "12:00"},
            new Order{Price = "12000", Quantity = "100", Exchange = "Binance", Currency = "ddddasTCUSDT", Action = "Long", Profit = "487", Date = "Wednesday", Time = "12:00"},
            new Order{Price = "12000", Quantity = "100", Exchange = "Binance", Currency = "BTCUSDasddT", Action = "Long", Profit = "487", Date = "Wednesday", Time = "12:00"},
            new Order{Price = "12000", Quantity = "100", Exchange = "Binance", Currency = "BTCUSDTasdsada", Action = "Long", Profit = "487", Date = "Wednesday", Time = "12:00"},
            new Order{Price = "12000", Quantity = "100", Exchange = "Binance", Currency = "BTCUasasdsadSDT", Action = "Long", Profit = "487", Date = "Wednesday", Time = "12:00"},
            new Order{Price = "12000", Quantity = "100", Exchange = "Binance", Currency = "BTCUSDT", Action = "Long", Profit = "487", Date = "Wednesday", Time = "12:00"},
            new Order{Price = "12000", Quantity = "100", Exchange = "Binance", Currency = "BTCUSDT", Action = "Lonzxg", Profit = "487", Date = "Wednesday", Time = "12:00"},
            new Order{Price = "12000", Quantity = "100", Exchange = "Binance", Currency = "BTCUSDT", Action = "Lodassdng", Profit = "487", Date = "Wednesday", Time = "12:00"},
            new Order{Price = "12000", Quantity = "100", Exchange = "Binance", Currency = "BTCUSDT", Action = "Long", Profit = "4dsad87", Date = "Wednesday", Time = "12:00"},
            new Order{Price = "12000", Quantity = "100", Exchange = "Binance", Currency = "BTCUSDT", Action = "Lonasdsad", Profit = "487", Date = "Wednesday", Time = "12:00"},
            new Order{Price = "12000", Quantity = "100", Exchange = "Binance", Currency = "BTCUSDT", Action = "Long", Profit = "48asdsad7", Date = "Wednesday", Time = "12:00"}
            
        };
        
        
        Console.WriteLine($"Orders count: {Orders.Count}");
    }
    
    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}