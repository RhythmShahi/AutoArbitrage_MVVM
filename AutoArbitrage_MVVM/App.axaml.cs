using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AutoArbitrage_MVVM.ViewModels;
using AutoArbitrage_MVVM.Views;

namespace AutoArbitrage_MVVM;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var startUpWindow = new Login();
            desktop.MainWindow = startUpWindow;
            startUpWindow.DataContext = new LoginViewModel(startUpWindow);
            
            desktop.MainWindow.Show();
        }

        base.OnFrameworkInitializationCompleted();
    }
    
}