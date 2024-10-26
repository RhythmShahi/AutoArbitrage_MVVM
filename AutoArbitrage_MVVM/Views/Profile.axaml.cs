using System.Linq;
using AutoArbitrage_MVVM.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace AutoArbitrage_MVVM.Views;

public partial class Profile : UserControl
{
    public Profile()
    {
        InitializeComponent();
        DataContext = new ProfileViewModel();
    }

    private void EditInfo_OnClick(object? sender, RoutedEventArgs e)
    {
        foreach (var textBox in this.GetVisualDescendants().OfType<TextBox>())
        {
            textBox.Opacity = 1;
        }

        NameLabel.Opacity = 1;
        BiSecLabel.Opacity = 1;
        BySecLabel.Opacity = 1;
        
        Email.Opacity = 0;
        Phone.Opacity = 0;
        BinanceKey.Opacity = 0;
        BybitKey.Opacity = 0;

        SaveInfo.ZIndex = 1;
        SaveInfo.Opacity = 1;
        EditInfo.ZIndex = 0;
        EditInfo.Opacity = 0;
    }

    private void SaveInfo_OnClick(object? sender, RoutedEventArgs e)
    {
        foreach (var textBox in this.GetVisualDescendants().OfType<TextBox>())
        {
            textBox.Opacity = 0;
        }
        
        NameLabel.Opacity = 0;
        BiSecLabel.Opacity = 0;
        BySecLabel.Opacity = 0;
        
        Email.Opacity = 1;
        Phone.Opacity = 1;
        BinanceKey.Opacity = 1;
        BybitKey.Opacity = 1;

        SaveInfo.ZIndex = 0;
        SaveInfo.Opacity = 0;
        EditInfo.ZIndex = 1;
        EditInfo.Opacity = 1;
    }
}