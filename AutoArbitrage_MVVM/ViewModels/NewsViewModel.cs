using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoArbitrage_MVVM.ViewModels;

public partial class NewsViewModel : ObservableObject
{
    private DateTime _nextTargetTime;
    
    private static readonly TimeSpan[] TargetTimes =
    {
        new TimeSpan(4, 0, 0),
        new TimeSpan(8,0,0),
        new TimeSpan(12, 0, 0),
        new TimeSpan(16,0,0),
        new TimeSpan(20, 0, 0),
        new TimeSpan(24,0,0)
    };
    
    [ObservableProperty] 
    private string _countdown;
    

    public NewsViewModel()
    {
        NewsTimer();
    }
    
    private void NewsTimer()
    {
        DispatcherTimer.Run(() =>
        {
            UpdateCountdown();
            return true; // Keep running
        }, TimeSpan.FromSeconds(1));
    }

    private void UpdateCountdown()
    {
        var now = DateTime.Now;
        var timeRemaining = _nextTargetTime - now;

        if (timeRemaining.TotalSeconds <= 0)
        {
            UpdateNextTargetTime();
            timeRemaining = _nextTargetTime - now;
        }

        Countdown = FormatTimeRemaining(timeRemaining);
    }

    private void UpdateNextTargetTime()
    {
        var now = DateTime.Now.TimeOfDay;
        foreach (var target in TargetTimes)
        {
            if (now < target)
            {
                _nextTargetTime = DateTime.Today.Add(target);
                return;
            }
        }

        _nextTargetTime = DateTime.Today.Add(TargetTimes[0]).AddDays(1);
    }

    private string FormatTimeRemaining(TimeSpan timeSpan)
    {
        return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
    }
}
