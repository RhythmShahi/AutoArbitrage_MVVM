namespace AutoArbitrage_MVVM.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MySql.Data.MySqlClient;

public class ProfileViewModel
{
    private string connectionString = "Server=database-1.c1auqyeukz94.me-central-1.rds.amazonaws.com;Database=userdb;User ID=admin;Password=autoarbitrage12;";
    
    [ObservableProperty] private string email;

    [ObservableProperty] private string name;

    [ObservableProperty] private string joinedDate;

    [ObservableProperty] private string country;

    [ObservableProperty] private string binanceKey;

    [ObservableProperty] private string bybitKey;
    
}