using AutoArbitrage_MVVM.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoArbitrage_MVVM.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        
        [ObservableProperty]
        private object _currentView;
        
        public TradeViewModel TradeViewModel { get; }
        public ProfileViewModel ProfileViewModel { get; }
        public NewsViewModel NewsViewModel { get; }
        public SettingsViewModel SettingsViewModel { get; }
        public TransactionsViewModel TransactionsViewModel { get; }
        public WalletViewModel WalletViewModel { get; }
        
        public MainWindowViewModel()
        {
            TradeViewModel = new TradeViewModel(); 
            ProfileViewModel = new ProfileViewModel();
            NewsViewModel = new NewsViewModel();
            SettingsViewModel = new SettingsViewModel();
            TransactionsViewModel = new TransactionsViewModel();
            WalletViewModel = new WalletViewModel();

            CurrentView = TradeViewModel;
        }
        
        [RelayCommand]
        public void OnNavigate(string destination)
        {
            CurrentView = destination switch
            {
                "Trade" => TradeViewModel,
                "Wallet" => WalletViewModel,
                "Profile" => ProfileViewModel, 
                "News" => NewsViewModel,
                "Settings" => SettingsViewModel,
                "Transactions" => TransactionsViewModel,
                _ => CurrentView
            };
        }
    }
}
