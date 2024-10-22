using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AutoArbitrage_MVVM.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace AutoArbitrage_MVVM.Views;

public partial class News : UserControl
{
    public News()
    {
        InitializeComponent();
        DataContext = new NewsViewModel();
        Dispatcher.UIThread.InvokeAsync(() => RunPython());
    }
    private async Task RunPython()
            {
                // Path to the Python executable
                string pythonPath = @"D:\JetBrains Rider\Projects\AutoArbitrage_MVVM\venv\Scripts\python.exe"; 
                // Path to your Python script
                string scriptPath = @"D:\JetBrains Rider\Projects\AutoArbitrage_MVVM\AutoArbitrage_MVVM\Assets\Model.py"; 

                try
                {
                    // Create a new process start info
                    ProcessStartInfo start = new ProcessStartInfo
                    {
                        FileName = pythonPath,
                        Arguments = $"\"{scriptPath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    // Start the process
                    using (Process process = Process.Start(start))
                    {
                        // Read the output
                        string output = await process.StandardOutput.ReadToEndAsync();
                        string error = await process.StandardError.ReadToEndAsync();
                        process.WaitForExit();

                        // Print the output
                        Console.WriteLine("Output from Python script:");
                        Console.WriteLine(output);

                        // Check for errors
                        if (!string.IsNullOrEmpty(error))
                        {
                            Console.WriteLine("Error occurred:");
                            Console.WriteLine(error);
                        }

                        // Process the JSON output
                        if (!string.IsNullOrEmpty(output))
                        {
                            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(output);

                            // Display news in the ListBox
                            await DisplayNews(jsonResponse);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred while running the Python script:");
                    Console.WriteLine(ex.Message);
                }
            }

            private async Task DisplayNews(JsonElement jsonResponse)
            {
                int newsLimit = 50; // Limit to 50 news items
                int count = 0;

                try
                {
                    // Assuming the JSON is a list of articles
                    foreach (var article in jsonResponse.EnumerateArray())
                    {
                        if (count >= newsLimit)
                            break;

                        string url = article.GetProperty("url").GetString();
                        string title = article.GetProperty("title").GetString();
                        string description = article.GetProperty("description").GetString();
                        string img = article.GetProperty("thumbnail").GetString();
                        string createdAt = article.GetProperty("createdAt").GetString();
                        string sentiment = article.GetProperty("sentiment").GetString();
                        decimal score = article.GetProperty("score").GetDecimal();
                        
                        DateTime dateTime;
                        string formattedDate = createdAt; // Default to the raw string in case of parsing failure
                        if (DateTime.TryParse(createdAt, out dateTime))
                        {
                            string daySuffix = GetDaySuffix(dateTime.Day);
                            formattedDate = dateTime.ToString($"ddd, d'{daySuffix}' MMMM yyyy, HH:mm");
                        }

                        var listBoxItem = new ListBoxItem
                        {
                            Width = 1020,
                            Background = new SolidColorBrush(Color.Parse("#0E0E22")),
                            Margin = new Thickness(0, 15, 0, 0),
                            CornerRadius = new CornerRadius(15),
                        };

                        var grid = new Grid();
                        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(500)));
                        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

                        var stackPanel = new StackPanel { Margin = new Thickness(170, 0, 0, 0) };

                        var titleTextBlock = new TextBlock
                        {
                            Foreground = Brushes.White,
                            FontSize = 18,
                            TextWrapping = TextWrapping.Wrap,
                            Width = 500,
                            FontWeight = FontWeight.Bold,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            Text = title
                        };

                        var descriptionTextBlock = new TextBlock
                        {
                            Foreground = Brushes.Gray,
                            Margin = new Thickness(0, 10, 0, 0),
                            FontSize = 11,
                            Width = 480,
                            TextWrapping = TextWrapping.Wrap,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            Text = description
                        };

                        var sentimentTextBlock = new TextBlock
                        {
                            FontWeight = FontWeight.Bold,
                            Foreground = Brushes.White,
                            FontSize = 11,
                            Margin = new Thickness(0, 10, 0, 0),
                            Text = $"{char.ToUpper(sentiment[0]) + sentiment[1..].ToLower()}"
                        };

                        if (sentiment.ToLower().Equals("negative"))
                        {
                            sentimentTextBlock.Foreground = Brushes.Red;
                        }
                        else if (sentiment.ToLower().Equals("positive"))
                        {
                            sentimentTextBlock.Foreground = Brushes.Green;
                        }
                        else if (sentiment.ToLower().Equals("neutral"))
                        {
                            sentimentTextBlock.Foreground = Brushes.Orange;
                        }
                        else
                        {
                            sentimentTextBlock.Foreground = Brushes.White;
                        }

                        // Add elements to the stack panel
                        stackPanel.Children.Add(titleTextBlock);
                        stackPanel.Children.Add(descriptionTextBlock);
                        stackPanel.Children.Add(sentimentTextBlock);

                        // Add the new grid for the accuracy bar below the sentiment
                        var accuracyGrid = new Grid();

                        var backgroundBar = new Rectangle
                        {
                            Fill = new SolidColorBrush(Color.Parse("#323949")),
                            Width = 300,
                            Height = 20,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            RadiusX = 10,
                            RadiusY = 10,
                            Margin = new Thickness(-5, 4, 0, 0),
                        };

                        double barFill = (double) score * 300;

                        var accBar = new Rectangle
                        {
                            Fill = new LinearGradientBrush
                            {
                                GradientStops = new GradientStops
                                {
                                    new GradientStop { Offset = 0.1, Color = Color.Parse("#173055") }, 
                                    new GradientStop { Offset = 1, Color = Color.Parse("#3C5780") }
                                }
                            },
                            Width = barFill,
                            Height = 20,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            RadiusX = 10,
                            RadiusY = 10,
                            Margin = new Thickness(-5, 4, 0, 0),
                        };

                        var zeroTextBlock = new TextBlock
                        {
                            Foreground = Brushes.White,
                            Margin = new Thickness(1, 8, 0, 0),
                            Text = "0%",
                            FontSize = 10,
                            FontWeight = FontWeight.SemiBold
                        };

                        var hundredTextBlock = new TextBlock
                        {
                            Foreground = Brushes.White,
                            Margin = new Thickness(265, 8, 0, 0),
                            Text = "100%",
                            FontSize = 10,
                            FontWeight = FontWeight.SemiBold
                        };

                        var accuracyTextBlock = new TextBlock
                        {
                            Foreground = Brushes.White,
                            Margin = new Thickness(113, 8, 0, 0),
                            Text = "Accuracy",
                            FontSize = 10,
                            FontWeight = FontWeight.SemiBold
                        };

                        // Add elements to the accuracy grid
                        accuracyGrid.Children.Add(backgroundBar);
                        accuracyGrid.Children.Add(accBar);
                        accuracyGrid.Children.Add(zeroTextBlock);
                        accuracyGrid.Children.Add(hundredTextBlock);
                        accuracyGrid.Children.Add(accuracyTextBlock);

                        var dateTextBlock = new TextBlock
                        {
                            Foreground = Brushes.White,
                            FontSize = 11,
                            Margin = new Thickness(0, 10, 0, 0),
                            Text = formattedDate
                        };
                        
                        stackPanel.Children.Add(accuracyGrid);
                        stackPanel.Children.Add(dateTextBlock);

                        Grid.SetColumn(stackPanel, 0);
                        grid.Children.Add(stackPanel);

                        // Download image from URL and set it to the Image control
                        var newsImage = new Image
                        {
                            Width = 140,
                            Height = 140,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(175, 0, 0, 0)
                        };

                        // Download image in a background thread to avoid blocking the UI
                        var imageBytes = await DownloadImageAsync(img);
                        if (imageBytes != null)
                        {
                            // Ensure this UI update happens on the UI thread
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                using (var stream = new MemoryStream(imageBytes))
                                {
                                    newsImage.Source = new Bitmap(stream);
                                }
                            });
                        }

                        Grid.SetColumn(newsImage, 1);
                        grid.Children.Add(newsImage);

                        listBoxItem.Content = grid;

                        listBoxItem.PointerReleased += async (s, e) =>
                        {
                            // Open the URL in the default web browser
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = url,
                                UseShellExecute = true
                            });
                        };

                        // Add the ListBoxItem to the ListBox
                        await Dispatcher.UIThread.InvokeAsync(() => { NewsList.Items.Add(listBoxItem); });
                        count++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error displaying news: {ex.Message}");
                }
            }

            // New method to download image bytes
            private async Task<byte[]> DownloadImageAsync(string imageUrl)
            {
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        return await httpClient.GetByteArrayAsync(imageUrl);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load image from {imageUrl}: {ex.Message}");
                    return null;
                }
            }
            
            private string GetDaySuffix(int day)
            {
                if (day >= 11 && day <= 13)
                {
                    return "th";
                }
                
                switch (day % 10)
                {
                    case 1: return "st";
                    case 2: return "nd";
                    case 3: return "rd";
                    default: return "th";
                }
            }
            
}
