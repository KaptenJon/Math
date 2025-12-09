using Math.Models;
using Math.Services;

namespace Math.Pages;

public class StatsPage : ContentPage
{
    private readonly IGameService _gameService;
    private readonly ILocalizationService _loc;

    public StatsPage()
    {
        _gameService = MauiProgram.Services.GetService<IGameService>()!;
        _loc = MauiProgram.Services.GetService<ILocalizationService>()!;

        Title = _loc["Stats_Title"];
        BackgroundColor = Color.FromArgb("#B0E0E6");

        var player = _gameService.CurrentPlayer;

        // Header
        var headerLabel = new Label
        {
            Text = $"?? {_loc["Stats_Title"]}",
            FontSize = 28,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            Margin = new Thickness(20, 20, 20, 10)
        };

        // Three stat cards: Today, This Week, This Month
        var todayCard = CreateStatCard(_loc["Stats_Today"], GetTodayStats(player));
        var weekCard = CreateStatCard(_loc["Stats_ThisWeek"], GetWeekStats(player));
        var monthCard = CreateStatCard(_loc["Stats_ThisMonth"], GetMonthStats(player));

        // Overall stats
        var overallLabel = new Label
        {
            Text = "Overall Stats",
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#8B0000"),
            Margin = new Thickness(20, 20, 20, 10)
        };

        var totalLessons = player.GetTotalLessons();
        var accuracy = totalLessons > 0 ? player.GetOverallAccuracy() : 0;

        var overallCard = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 15 },
            Background = new SolidColorBrush(Colors.White),
            Stroke = new SolidColorBrush(Color.FromArgb("#FFD700")),
            StrokeThickness = 2,
            Padding = new Thickness(20),
            Margin = new Thickness(20, 0, 20, 20),
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    new Label { Text = $"?? {_loc.Get("Stats_Lessons", totalLessons)}", FontSize = 18, TextColor = Color.FromArgb("#165B33") },
                    new Label { Text = $"?? {_loc.Get("Stats_Accuracy", accuracy.ToString("F1"))}", FontSize = 18, TextColor = Color.FromArgb("#165B33") },
                    new Label { Text = $"? {_loc.Get("Stats_Points", player.Points)}", FontSize = 18, TextColor = Color.FromArgb("#165B33") }
                }
            }
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 0,
                Children =
                {
                    headerLabel,
                    todayCard,
                    weekCard,
                    monthCard,
                    overallLabel,
                    overallCard
                }
            },
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop { Color = Color.FromArgb("#1A4D2E"), Offset = 0.0f },
                    new GradientStop { Color = Color.FromArgb("#2C6B3F"), Offset = 0.5f },
                    new GradientStop { Color = Color.FromArgb("#8B0000"), Offset = 1.0f }
                }
            }
        };

        _loc.LanguageChanged += () =>
        {
            Title = _loc["Stats_Title"];
            headerLabel.Text = $"?? {_loc["Stats_Title"]}";
            overallLabel.Text = _loc["Stats_Title"];
        };
    }

    private Border CreateStatCard(string period, (int lessons, double accuracy) stats)
    {
        return new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 15 },
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop { Color = Color.FromArgb("#FFFAF0"), Offset = 0.0f },
                    new GradientStop { Color = Color.FromArgb("#FFE4B5"), Offset = 1.0f }
                }
            },
            Stroke = new SolidColorBrush(Color.FromArgb("#C41E3A")),
            StrokeThickness = 2,
            Padding = new Thickness(20),
            Margin = new Thickness(20, 10, 20, 10),
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    new Label { Text = period, FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#8B0000") },
                    new Label { Text = $"{_loc.Get("Stats_Lessons", stats.lessons)}", FontSize = 16, TextColor = Color.FromArgb("#165B33") },
                    new Label { Text = $"{_loc.Get("Stats_Accuracy", stats.accuracy.ToString("F1"))}", FontSize = 16, TextColor = Color.FromArgb("#165B33") }
                }
            }
        };
    }

    private (int lessons, double accuracy) GetTodayStats(Player player)
    {
        var today = DateTime.Now.Date;
        var todayStats = player.SessionStats.Where(s => s.CompletedAt.Date == today).ToList();
        var lessons = todayStats.Count;
        var accuracy = todayStats.Count > 0 
            ? (double)todayStats.Sum(s => s.CorrectAnswers) / todayStats.Sum(s => s.TotalQuestions) * 100 
            : 0;
        return (lessons, accuracy);
    }

    private (int lessons, double accuracy) GetWeekStats(Player player)
    {
        var weekAgo = DateTime.Now.AddDays(-7);
        var weekStats = player.SessionStats.Where(s => s.CompletedAt >= weekAgo).ToList();
        var lessons = weekStats.Count;
        var accuracy = weekStats.Count > 0 
            ? (double)weekStats.Sum(s => s.CorrectAnswers) / weekStats.Sum(s => s.TotalQuestions) * 100 
            : 0;
        return (lessons, accuracy);
    }

    private (int lessons, double accuracy) GetMonthStats(Player player)
    {
        var monthAgo = DateTime.Now.AddDays(-30);
        var monthStats = player.SessionStats.Where(s => s.CompletedAt >= monthAgo).ToList();
        var lessons = monthStats.Count;
        var accuracy = monthStats.Count > 0 
            ? (double)monthStats.Sum(s => s.CorrectAnswers) / monthStats.Sum(s => s.TotalQuestions) * 100 
            : 0;
        return (lessons, accuracy);
    }
}
