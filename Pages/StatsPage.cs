using Math.Models;
using Math.Services;

namespace Math.Pages;

public class StatsPage : ContentPage
{
    private readonly IGameService _gameService;
    private readonly ILocalizationService _loc;
    private readonly Label _headerLabel;
    private readonly Label _overallLabel;
    private readonly VerticalStackLayout _mainLayout;

    public StatsPage()
    {
        _gameService = MauiProgram.Services.GetService<IGameService>()!;
        _loc = MauiProgram.Services.GetService<ILocalizationService>()!;

        Title = _loc["Stats_Title"];
        BackgroundColor = Color.FromArgb("#B0E0E6");

        // Header
        _headerLabel = new Label
        {
            Text = _loc["Stats_Title"],
            FontSize = 28,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            Margin = new Thickness(20, 20, 20, 10)
        };

        // Overall stats header
        _overallLabel = new Label
        {
            Text = _loc["Stats_Overall"],
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#8B0000"),
            Margin = new Thickness(20, 20, 20, 10)
        };

        _mainLayout = new VerticalStackLayout
        {
            Spacing = 0,
            Children = { _headerLabel }
        };

        Content = new ScrollView
        {
            Content = _mainLayout,
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

        _loc.LanguageChanged += RefreshStats;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshStats();
    }

    private void RefreshStats()
    {
        var player = _gameService.CurrentPlayer;

        // Clear old cards (except header)
        while (_mainLayout.Children.Count > 1)
        {
            _mainLayout.Children.RemoveAt(1);
        }

        // Update header texts
        _headerLabel.Text = _loc["Stats_Title"];
        _overallLabel.Text = _loc["Stats_Overall"];

        // Create updated stat cards
        var todayCard = CreateStatCard(_loc["Stats_Today"], GetTodayStats(player));
        var weekCard = CreateStatCard(_loc["Stats_ThisWeek"], GetWeekStats(player));
        var monthCard = CreateStatCard(_loc["Stats_ThisMonth"], GetMonthStats(player));

        var totalSessions = player.SessionStats.Count;
        var totalQuestions = player.SessionStats.Sum(s => s.TotalQuestions);
        var totalCorrect = player.SessionStats.Sum(s => s.CorrectAnswers);
        var accuracy = totalQuestions > 0 ? (double)totalCorrect / totalQuestions * 100 : 0;

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
                    new Label { Text = _loc.Get("Stats_Sessions", totalSessions), FontSize = 18, TextColor = Color.FromArgb("#165B33") },
                    new Label { Text = _loc.Get("Stats_Questions", totalQuestions), FontSize = 18, TextColor = Color.FromArgb("#165B33") },
                    new Label { Text = _loc.Get("Stats_Accuracy", accuracy.ToString("F1")), FontSize = 18, TextColor = Color.FromArgb("#165B33") },
                    new Label { Text = _loc.Get("Stats_Points", player.Points), FontSize = 18, TextColor = Color.FromArgb("#165B33") }
                }
            }
        };

        // Add cards to layout
        _mainLayout.Children.Add(todayCard);
        _mainLayout.Children.Add(weekCard);
        _mainLayout.Children.Add(monthCard);
        _mainLayout.Children.Add(_overallLabel);
        _mainLayout.Children.Add(overallCard);
    }

    private Border CreateStatCard(string period, (int sessions, int questions, double accuracy) stats)
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
                    new Label { Text = _loc.Get("Stats_Sessions", stats.sessions), FontSize = 16, TextColor = Color.FromArgb("#165B33") },
                    new Label { Text = _loc.Get("Stats_Questions", stats.questions), FontSize = 16, TextColor = Color.FromArgb("#165B33") },
                    new Label { Text = _loc.Get("Stats_Accuracy", stats.accuracy.ToString("F1")), FontSize = 16, TextColor = Color.FromArgb("#165B33") }
                }
            }
        };
    }

    private (int sessions, int questions, double accuracy) GetTodayStats(Player player)
    {
        var today = DateTime.Now.Date;
        var todayStats = player.SessionStats.Where(s => s.CompletedAt.Date == today).ToList();
        var sessions = todayStats.Count;
        var questions = todayStats.Sum(s => s.TotalQuestions);
        var accuracy = questions > 0 
            ? (double)todayStats.Sum(s => s.CorrectAnswers) / questions * 100 
            : 0;
        return (sessions, questions, accuracy);
    }

    private (int sessions, int questions, double accuracy) GetWeekStats(Player player)
    {
        var weekAgo = DateTime.Now.AddDays(-7);
        var weekStats = player.SessionStats.Where(s => s.CompletedAt >= weekAgo).ToList();
        var sessions = weekStats.Count;
        var questions = weekStats.Sum(s => s.TotalQuestions);
        var accuracy = questions > 0 
            ? (double)weekStats.Sum(s => s.CorrectAnswers) / questions * 100 
            : 0;
        return (sessions, questions, accuracy);
    }

    private (int sessions, int questions, double accuracy) GetMonthStats(Player player)
    {
        var monthAgo = DateTime.Now.AddDays(-30);
        var monthStats = player.SessionStats.Where(s => s.CompletedAt >= monthAgo).ToList();
        var sessions = monthStats.Count;
        var questions = monthStats.Sum(s => s.TotalQuestions);
        var accuracy = questions > 0 
            ? (double)monthStats.Sum(s => s.CorrectAnswers) / questions * 100 
            : 0;
        return (sessions, questions, accuracy);
    }
}
