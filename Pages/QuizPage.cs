using Math.Models;
using Math.Services;
using Microsoft.Maui.Devices; // For Vibration
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls.Shapes;
using System.Globalization;
using Microsoft.Maui.Layouts;

namespace Math.Pages;

public class QuizPage : ContentPage
{
    private readonly IGameService _gameService;
    private readonly IStorageService _storage;
    private readonly ILocalizationService _loc = MauiProgram.Services.GetService<ILocalizationService>()!;
    private readonly string _categoryKey;
    private IReadOnlyList<Question> _questions;
    private int _index;
    private int _correct;

    private readonly Label _header = new() { FontAttributes = FontAttributes.Bold, FontSize = 20, TextColor = Color.FromArgb("#8B0000") };
    private readonly Image _avatarImage = new() { HeightRequest = 36, WidthRequest = 36, Aspect = Aspect.AspectFit, Margin = new Thickness(0, 0, 8, 0) };
    private readonly Label _progress = new() { FontAttributes = FontAttributes.Bold, FontSize = 14, TextColor = Color.FromArgb("#165B33") };
    private readonly Border _questionBubble;
    private readonly Label _questionLabel = new()
    {
        FontSize = 22,
        FontAttributes = FontAttributes.Bold,
        HorizontalTextAlignment = TextAlignment.Start,
        HorizontalOptions = LayoutOptions.StartAndExpand,
        TextColor = Color.FromArgb("#2C1810"),
        LineBreakMode = LineBreakMode.WordWrap
    };
    private readonly Label _equalsLabel = new() { Text = " = ", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#8B0000"), VerticalTextAlignment = TextAlignment.Center };
    private readonly Entry _answerEntry = new() { Keyboard = Keyboard.Numeric, FontSize = 22, WidthRequest = 100, HorizontalTextAlignment = TextAlignment.Center, BackgroundColor = Colors.White, TextColor = Color.FromArgb("#165B33") };
    private readonly ProgressBar _bar = new() { HeightRequest = 10, ProgressColor = Color.FromArgb("#C41E3A"), BackgroundColor = Color.FromArgb("#E8F5E9") };
    private readonly Button _submitBtn;
    private readonly Label _feedback = new() { FontSize = 18, FontAttributes = FontAttributes.Italic, HorizontalTextAlignment = TextAlignment.Center };
    private readonly Label _difficultyLabel = new() { FontSize = 12, TextColor = Color.FromArgb("#8B0000"), HorizontalTextAlignment = TextAlignment.End };
    private readonly Label _streakLabel = new() { FontSize = 12, TextColor = Color.FromArgb("#FFD700") };

    // Tree progress visuals
    private AbsoluteLayout _treeLayout = null!;
    private readonly List<Point> _baublePositions = new();
    private Image _santa = null!;
    private Frame _santaFrame = null!;
    private int _santaStep = 0;
    private readonly List<BoxView> _presents = new();

    private string[] CheerMessages => (_loc["CheerMessages"] ?? "Great!|Awesome!").Split('|');

    public QuizPage(IGameService gameService, IStorageService storage, string categoryKey)
    {
        _gameService = gameService;
        _storage = storage;
        _categoryKey = categoryKey;
        _questions = _gameService.GenerateQuestions(categoryKey);
        Title = _loc[categoryKey];
        BackgroundColor = Color.FromArgb("#F0F8FF"); // Light winter blue

        var questionRow = new HorizontalStackLayout
        {
            Spacing = 8,
            VerticalOptions = LayoutOptions.Start,
            Children = { _questionLabel, _equalsLabel, _answerEntry }
        };

        _questionBubble = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 },
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop { Color = Color.FromArgb("#FFFAF0"), Offset = 0.0f },  // Floral white
                    new GradientStop { Color = Color.FromArgb("#FFE4B5"), Offset = 1.0f }   // Moccasin
                }
            },
            Stroke = new SolidColorBrush(Color.FromArgb("#C41E3A")),
            StrokeThickness = 3,
            Padding = 16,
            Content = questionRow,
            HorizontalOptions = LayoutOptions.Fill,
            MaximumWidthRequest = 450,
            Shadow = new Shadow
            {
                Brush = new SolidColorBrush(Colors.Black),
                Opacity = 0.3f,
                Radius = 10,
                Offset = new Point(4, 4)
            }
        };

        // Build Christmas tree progress view
        var treeView = BuildTreeProgressView();
        treeView.HorizontalOptions = LayoutOptions.Start;

        _submitBtn = new Button 
        { 
            Text = _loc["Quiz_Submit"], 
            FontSize = 20, 
            CornerRadius = 12, 
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop { Color = Color.FromArgb("#165B33"), Offset = 0.0f },  // Christmas green
                    new GradientStop { Color = Color.FromArgb("#43A047"), Offset = 1.0f }   // Lighter green
                }
            },
            TextColor = Colors.White,
            BorderColor = Color.FromArgb("#FFD700"),
            BorderWidth = 2,
            HeightRequest = 44,
            HorizontalOptions = LayoutOptions.Start,
            Shadow = new Shadow
            {
                Brush = new SolidColorBrush(Colors.Black),
                Opacity = 0.4f,
                Radius = 6,
                Offset = new Point(3, 3)
            }
        };
        _submitBtn.Clicked += OnSubmit;

        _answerEntry.Completed += OnSubmit; // allow Enter/Done to submit

        AutomationProperties.SetName(_submitBtn, "Submit Answer");
        AutomationProperties.SetName(_answerEntry, "Answer Entry");

        var headerGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        
        headerGrid.Add(_avatarImage);
        Grid.SetColumn(_avatarImage, 0);
        headerGrid.Add(_header);
        Grid.SetColumn(_header, 1);
        headerGrid.Add(_difficultyLabel);
        Grid.SetColumn(_difficultyLabel, 2);

        var keypad = BuildKeypad();
        keypad.HorizontalOptions = LayoutOptions.Start;

        var keypadAndTree = new HorizontalStackLayout
        {
            Spacing = 16,
            HorizontalOptions = LayoutOptions.Start,
            Children = { keypad, treeView }
        };

        var leftContent = new VerticalStackLayout
        {
            Spacing = 10,
            HorizontalOptions = LayoutOptions.Start,
            Children =
            {
                _questionBubble,
                keypadAndTree,
                _submitBtn,
                _feedback
            }
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16, 20),
                Spacing = 10,
                HorizontalOptions = LayoutOptions.Start,
                Children =
                {
                    headerGrid,
                    _streakLabel,
                    _progress,
                    _bar,
                    leftContent
                }
            },
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop { Color = Color.FromArgb("#B0E0E6"), Offset = 0.0f },  // Powder blue (sky)
                    new GradientStop { Color = Color.FromArgb("#F0F8FF"), Offset = 0.3f },  // Alice blue
                    new GradientStop { Color = Color.FromArgb("#FFFAFA"), Offset = 1.0f }   // Snow white
                }
            }
        };

        _loc.LanguageChanged += () =>
        {
            Title = _loc[_categoryKey];
            _submitBtn.Text = _loc["Quiz_Submit"];
            ShowCurrent();
        };

        ShowCurrent();
    }

    private View BuildTreeProgressView()
    {
        // AbsoluteLayout with christmas tree image background, baubles, and a Santa marker
        _treeLayout = new AbsoluteLayout { HeightRequest = 240, WidthRequest = 160, BackgroundColor = Colors.Transparent };

        // Tree image as background
        var treeImage = new Image 
        { 
            Source = "christmas_tree.png", 
            Aspect = Aspect.AspectFit,
            WidthRequest = 160,
            HeightRequest = 240
        };
        AbsoluteLayout.SetLayoutBounds(treeImage, new Rect(0, 0, 160, 240));
        AbsoluteLayout.SetLayoutFlags(treeImage, AbsoluteLayoutFlags.None);
        _treeLayout.Children.Add(treeImage);

        // Define bauble absolute positions (adjusted for 160x240 size - scaled down)
        _baublePositions.Clear();
        // bottom row (4)
        _baublePositions.AddRange(new[]{ new Point(35,200), new Point(62,200), new Point(98,200), new Point(125,200)});
        // row 2 (3)
        _baublePositions.AddRange(new[]{ new Point(48,162), new Point(80,162), new Point(112,162)});
        // row 3 (2)
        _baublePositions.AddRange(new[]{ new Point(62,124), new Point(98,124)});
        // top (1) - star position
        _baublePositions.Add(new Point(80,86));

        // Render baubbles on top of the tree image
        for (int i = 0; i < _baublePositions.Count; i++)
        {
            var p = _baublePositions[i];
            
            // Top position gets a star instead of a bauble
            if (i == _baublePositions.Count - 1)
            {
                var star = new Polygon
                {
                    Fill = new RadialGradientBrush
                    {
                        Center = new Point(0.5, 0.5),
                        Radius = 0.8,
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop { Color = Colors.White, Offset = 0.0f },
                            new GradientStop { Color = Color.FromArgb("#FFD700"), Offset = 0.5f },
                            new GradientStop { Color = Color.FromArgb("#FFA500"), Offset = 1.0f }
                        }
                    },
                    Stroke = new SolidColorBrush(Color.FromArgb("#FFD700")),
                    StrokeThickness = 2,
                    Points = CreateStarPoints(p.X, p.Y, 16, 7)
                };
                AbsoluteLayout.SetLayoutBounds(star, new Rect(p.X - 16, p.Y - 16, 32, 32));
                AbsoluteLayout.SetLayoutFlags(star, AbsoluteLayoutFlags.None);
                _treeLayout.Children.Add(star);
                continue;
            }
            
            var color = i switch
            {
                0 or 3 or 5 or 8 => Color.FromArgb("#C41E3A"),  // Christmas red
                1 or 4 or 6 => Color.FromArgb("#FFD700"),       // Gold
                2 or 7 => Color.FromArgb("#4169E1"),            // Royal blue
                _ => Color.FromArgb("#50C878")                   // Emerald green
            };
            var bauble = new Ellipse
            {
                Fill = new RadialGradientBrush
                {
                    Center = new Point(0.3, 0.3),
                    Radius = 0.8,
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop { Color = Colors.White.WithAlpha(0.8f), Offset = 0.0f },
                        new GradientStop { Color = color, Offset = 0.5f },
                        new GradientStop { Color = color.WithLuminosity(0.3f), Offset = 1.0f }
                    }
                },
                Stroke = new SolidColorBrush(Color.FromArgb("#FFD700")),
                StrokeThickness = 1.5,
                WidthRequest = 22,
                HeightRequest = 22
            };
            AbsoluteLayout.SetLayoutBounds(bauble, new Rect(p.X - 11, p.Y - 11, 22, 22));
            AbsoluteLayout.SetLayoutFlags(bauble, AbsoluteLayoutFlags.None);
            _treeLayout.Children.Add(bauble);
        }

        // Santa marker as image
        var santaImage = new Image
        {
            Source = "santa_elf.png",
            Aspect = Aspect.AspectFit,
            WidthRequest = 40,
            HeightRequest = 40
        };
        
        _santa = new Image(); // dummy placeholder
        _santaFrame = new Frame
        {
            Padding = 0,
            BackgroundColor = Colors.Transparent,
            BorderColor = Colors.Transparent,
            HasShadow = true,
            Content = santaImage
        };
        
        AbsoluteLayout.SetLayoutBounds(_santaFrame, new Rect(_baublePositions[0].X - 20, _baublePositions[0].Y - 20, 40, 40));
        AbsoluteLayout.SetLayoutFlags(_santaFrame, AbsoluteLayoutFlags.None);
        _treeLayout.Children.Add(_santaFrame);

        return new Frame
        {
            Padding = new Thickness(6),
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop { Color = Color.FromArgb("#F0F8FF"), Offset = 0.0f },  // Sky blue
                    new GradientStop { Color = Colors.White, Offset = 1.0f }
                }
            },
            BorderColor = Color.FromArgb("#C41E3A"),
            HasShadow = true,
            Content = _treeLayout,
            VerticalOptions = LayoutOptions.Start,
            CornerRadius = 15
        };
    }

    private View BuildKeypad()
    {
        var grid = new Grid { ColumnSpacing = 8, RowSpacing = 8, HorizontalOptions = LayoutOptions.Center };
        for (int c = 0; c < 3; c++) grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        for (int r = 0; r < 4; r++) grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        string[] keys = ["1","2","3","4","5","6","7","8","9","±","0","?"];
        for (int i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            var btn = new Button
            {
                Text = key,
                FontSize = 20,
                CornerRadius = 12,
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop { Color = Color.FromArgb("#8B0000"), Offset = 0.0f },  // Dark red
                        new GradientStop { Color = Color.FromArgb("#C41E3A"), Offset = 1.0f }   // Christmas red
                    }
                },
                TextColor = Colors.White,
                HeightRequest = 44,
                WidthRequest = 72,
                BorderColor = Color.FromArgb("#FFD700"),
                BorderWidth = 1,
                Shadow = new Shadow
                {
                    Brush = new SolidColorBrush(Colors.Black),
                    Opacity = 0.3f,
                    Radius = 4,
                    Offset = new Point(2, 2)
                }
            };
            btn.Clicked += (_, _) => OnKey(key);
            grid.Add(btn, i % 3, i / 3);
        }
        return new Grid { Padding = new Thickness(0, 0, 0, 4), Children = { grid } };
    }

    private void OnKey(string key)
    {
        var txt = _answerEntry.Text ?? string.Empty;
        switch (key)
        {
            case "?":
                if (txt.Length > 0) txt = txt[..^1];
                break;
            case "±":
                if (txt.StartsWith("-")) txt = txt[1..];
                else txt = string.IsNullOrEmpty(txt) ? "-" : "-" + txt.TrimStart('-');
                break;
            default:
                if (txt.Length < 12) txt += key;
                break;
        }
        _answerEntry.Text = txt;
        _answerEntry.CursorPosition = _answerEntry.Text?.Length ?? 0;
    }

    private void ShowCurrent()
    {
        if (_index >= _questions.Count)
        {
            Finish();
            return;
        }
        
        var player = _gameService.CurrentPlayer;
        if (!string.IsNullOrEmpty(player.Avatar))
        {
            _avatarImage.Source = player.Avatar;
            _avatarImage.IsVisible = true;
        }
        else
        {
            _avatarImage.IsVisible = false;
        }
        
        _header.Text = string.IsNullOrWhiteSpace(player.Name) 
            ? _loc[_categoryKey] 
            : $"{player.Name} - {_loc[_categoryKey]}";
            
        _progress.Text = _loc.Get("Quiz_Progress", _index + 1, _questions.Count);
        _difficultyLabel.Text = _loc.Get("Quiz_Difficulty", _gameService.CurrentDifficulty);
        _streakLabel.Text = _gameService.CurrentStreak > 0 ? _loc.Get("Quiz_Streak", _gameService.CurrentStreak) : string.Empty;
        _bar.Progress = (double)_index / _questions.Count;

        var text = _questions[_index].Text;
        if (text.EndsWith(" = ?", StringComparison.Ordinal))
            text = text[..^4];
        else if (text.EndsWith("?", StringComparison.Ordinal) && text.Contains("=", StringComparison.Ordinal))
            text = text.TrimEnd('?');

        _questionLabel.Text = text;
        _questionLabel.Scale = 0.8;
        _ = _questionLabel.FadeToAsync(1, 150);
        _ = _questionLabel.ScaleToAsync(1, 150, Easing.CubicOut);

        if (_index == 0)
        {
            _santaStep = 0;
            MoveSantaToStep(0, animate:false);
        }

        _answerEntry.Text = string.Empty;
        MainThread.BeginInvokeOnMainThread(() => _answerEntry.Focus());
        _feedback.Text = string.Empty;
    }

    private async void OnSubmit(object? sender, EventArgs e)
    {
        if (_index >= _questions.Count) return;
        var answerText = _answerEntry.Text;
        if (!double.TryParse(answerText, NumberStyles.Float, CultureInfo.CurrentCulture, out var userAnswer))
        {
            await DisplayAlertAsync(_loc["Quiz_EnterAnswer_Title"], _loc["Quiz_EnterAnswer_Message"], _loc["Alert_OK"]);
            return;
        }
        var q = _questions[_index];
        bool correct = System.Math.Abs(q.Answer - userAnswer) < 0.0001;
        if (correct) _correct++;
        int streakBefore = _gameService.CurrentStreak;
        _gameService.AdjustDifficulty(correct);
        int basePoints = correct ? 1 : 0;
        int bonus = 0;
        if (correct)
        {
            int streak = _gameService.CurrentStreak;
            if (streak >= 3 && streak < 5) bonus = 1; else if (streak >= 5 && streak < 7) bonus = 2; else if (streak >= 7) bonus = 3;
        }
        int total = basePoints + bonus;
        if (total > 0) _gameService.AwardPoints(total);

        await _storage.LogAnswerAsync(_categoryKey, q.Text, q.Answer, userAnswer, correct, _gameService.CurrentDifficulty, streakBefore, total);
        await _storage.SavePlayerAsync(_gameService.CurrentPlayer);

        if (correct)
        {
            TryVibrate();
            await AnimateCorrectAsync();
            _feedback.TextColor = Colors.DarkGreen;
            var cheers = CheerMessages;
            var cheer = cheers[new Random().Next(cheers.Length)];
            _feedback.Text = bonus > 0 ? $"{cheer} +{basePoints} (+{bonus})" : $"{cheer} +{basePoints}";

            if (_santaStep < _baublePositions.Count - 1)
            {
                _santaStep++;
                await MoveSantaToStepAsync(_santaStep);
            }
        }
        else
        {
            _feedback.TextColor = Colors.DarkRed;
            _feedback.Text = _loc.Get("Quiz_Answer", q.Answer);
            await SantaFallAsync();
            _santaStep = 0;
            await MoveSantaToStepAsync(_santaStep);
        }
        _index++;
        await Task.Delay(400);
        ShowCurrent();
    }

    private async Task MoveSantaToStepAsync(int step)
    {
        step = System.Math.Clamp(step, 0, _baublePositions.Count - 1);
        var p = _baublePositions[step];
        await _santaFrame.TranslateTo(0, 0, 0);
        AbsoluteLayout.SetLayoutBounds(_santaFrame, new Rect(p.X - 18, p.Y - 18, 36, 36));
        AbsoluteLayout.SetLayoutFlags(_santaFrame, AbsoluteLayoutFlags.None);
        await _santaFrame.ScaleToAsync(1.2, 150, Easing.BounceOut);
        await _santaFrame.ScaleToAsync(1.0, 100);

        // Check if Santa reached the top
        if (step == _baublePositions.Count - 1)
        {
            await ShowPresentsAsync();
        }
    }

    private void MoveSantaToStep(int step, bool animate)
    {
        if (animate)
            _ = MoveSantaToStepAsync(step);
        else
        {
            step = System.Math.Clamp(step, 0, _baublePositions.Count - 1);
            var p = _baublePositions[step];
            AbsoluteLayout.SetLayoutBounds(_santaFrame, new Rect(p.X - 18, p.Y - 18, 36, 36));
            AbsoluteLayout.SetLayoutFlags(_santaFrame, AbsoluteLayoutFlags.None);
        }
    }

    private async Task SantaFallAsync()
    {
        try
        {
            await _santaFrame.RotateToAsync(20, 100);
            await _santaFrame.RotateToAsync(-20, 100);
            await _santaFrame.RotateToAsync(0, 100);
            await _santaFrame.TranslateToAsync(0, 40, 250);
            await _santaFrame.TranslateToAsync(0, 0, 250);
            
            // Remove all presents when Santa falls
            RemovePresents();
        }
        catch { }
    }

    private async Task ShowPresentsAsync()
    {
        // Define present positions under the tree (3 presents) - moved higher to be visible
        var presentData = new[]
        {
            new { X = 26, Y = 210, Width = 22, Height = 20, Color = Colors.Red },
            new { X = 66, Y = 215, Width = 26, Height = 24, Color = Colors.Blue },
            new { X = 110, Y = 213, Width = 20, Height = 18, Color = Colors.Gold }
        };

        foreach (var data in presentData)
        {
            var present = new BoxView
            {
                Color = data.Color,
                CornerRadius = 4,
                Opacity = 0,
                WidthRequest = data.Width,
                HeightRequest = data.Height
            };

            // Add a ribbon/bow on top
            var ribbon = new BoxView
            {
                Color = Colors.White,
                WidthRequest = data.Width,
                HeightRequest = 3
            };

            var bow = new Ellipse
            {
                Fill = new SolidColorBrush(Colors.White),
                WidthRequest = 8,
                HeightRequest = 8
            };

            // Container for present with ribbon
            var presentContainer = new AbsoluteLayout
            {
                WidthRequest = data.Width,
                HeightRequest = data.Height
            };

            AbsoluteLayout.SetLayoutBounds(present, new Rect(0, 0, data.Width, data.Height));
            presentContainer.Children.Add(present);

            AbsoluteLayout.SetLayoutBounds(ribbon, new Rect(0, data.Height / 2 - 1.5, data.Width, 3));
            presentContainer.Children.Add(ribbon);

            AbsoluteLayout.SetLayoutBounds(bow, new Rect(data.Width / 2 - 4, -4, 8, 8));
            presentContainer.Children.Add(bow);

            AbsoluteLayout.SetLayoutBounds(presentContainer, new Rect(data.X, data.Y, data.Width, data.Height));
            AbsoluteLayout.SetLayoutFlags(presentContainer, AbsoluteLayoutFlags.None);

            _treeLayout.Children.Add(presentContainer);
            _presents.Add(present);

            // Animate present appearing (drop in from above with bounce)
            presentContainer.TranslationY = -50;
            await Task.WhenAll(
                presentContainer.TranslateToAsync(0, 0, 400, Easing.BounceOut),
                present.FadeToAsync(1, 300)
            );

            await Task.Delay(100); // Stagger the presents
        }

        // Celebration animation - make presents wiggle
        await Task.Delay(200);
        foreach (var present in _presents)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await present.RotateToAsync(5, 100);
                    await present.RotateToAsync(-5, 100);
                    await present.RotateToAsync(0, 100);
                }
                catch { }
            });
        }
        
        // Show congratulations message
        await Task.Delay(500);
        await ShowCongratulationsAsync();
    }

    private void RemovePresents()
    {
        // Remove all present views from the tree layout
        var toRemove = _treeLayout.Children
            .Where(c => c is AbsoluteLayout && _presents.Any(p => ((AbsoluteLayout)c).Children.Contains(p)))
            .ToList();

        foreach (var present in toRemove)
        {
            _treeLayout.Children.Remove(present);
        }

        _presents.Clear();
    }

    private async Task ShowCongratulationsAsync()
    {
        var result = await DisplayAlertAsync(
            "?? BRA JOBBAT! ??",
            _loc.Get("Quiz_Finished_Message", _correct, _questions.Count, _gameService.CurrentPlayer.Points),
            _loc["Quiz_Continue"],
            _loc["Quiz_GoBack"]
        );

        if (result)
        {
            // Continue - reset and start new quiz
            _index = 0;
            _correct = 0;
            _santaStep = 0;
            RemovePresents();
            _questions = _gameService.GenerateQuestions(_categoryKey);
            _submitBtn.IsEnabled = true;
            MoveSantaToStep(0, animate: false);
            ShowCurrent();
        }
        else
        {
            // Go back to main page
            await Shell.Current.GoToAsync("..");
        }
    }

    private async Task AnimateCorrectAsync()
    {
        await _questionBubble.ScaleToAsync(1.08, 120);
        await _questionBubble.ScaleToAsync(1, 120);
        await _answerEntry.ScaleToAsync(1.15, 120);
        await _answerEntry.ScaleToAsync(1, 120);
        await _answerEntry.ColorTo(Colors.DarkBlue, Colors.ForestGreen, c => _answerEntry.TextColor = c, 180);
        await _answerEntry.ColorTo(Colors.ForestGreen, Colors.DarkBlue, c => _answerEntry.TextColor = c, 180);
    }

    private void TryVibrate()
    {
        try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(50)); } catch { }
    }

    private PointCollection CreateStarPoints(double centerX, double centerY, double outerRadius, double innerRadius)
    {
        var points = new PointCollection();
        for (int i = 0; i < 10; i++)
        {
            double angle = i * 36 - 90; // 36 degrees between points, start at top
            double radius = i % 2 == 0 ? outerRadius : innerRadius;
            double x = centerX + radius * System.Math.Cos(angle * System.Math.PI / 180);
            double y = centerY + radius * System.Math.Sin(angle * System.Math.PI / 180);
            points.Add(new Point(x, y));
        }
        return points;
    }

    private async void Finish()
    {
        _submitBtn.IsEnabled = false;
        _bar.Progress = 1;
        
        // Save session stats
        var sessionStat = new Models.SessionStat
        {
            CompletedAt = DateTime.Now,
            Category = _categoryKey,
            TotalQuestions = _questions.Count,
            CorrectAnswers = _correct,
            PointsEarned = _gameService.CurrentPlayer.Points - GetPreviousPoints()
        };
        _gameService.CurrentPlayer.SessionStats.Add(sessionStat);
        await _storage.SavePlayerAsync(_gameService.CurrentPlayer);
        
        var msg = _loc.Get("Quiz_Finished_Message", _correct, _questions.Count, _gameService.CurrentPlayer.Points);
        await DisplayAlertAsync(_loc["Quiz_Finished_Title"], msg, _loc["Alert_OK"]);
        await Shell.Current.GoToAsync(".." );
    }
    
    private int GetPreviousPoints()
    {
        // For simplicity, assume we start at 0 for this session
        // In a real app, you might track session start points
        return 0;
    }
}

internal static class ColorAnimationExtensions
{
    public static Task ColorTo(this VisualElement self, Color fromColor, Color toColor, Action<Color> callback, uint length = 250, Easing? easing = null)
    {
        Color Transform(double t) => Color.FromRgba(
            fromColor.Red + (toColor.Red - fromColor.Red) * t,
            fromColor.Green + (toColor.Green - fromColor.Green) * t,
            fromColor.Blue + (toColor.Blue - fromColor.Blue) * t,
            fromColor.Alpha + (toColor.Alpha - fromColor.Alpha) * t);

        var taskCompletionSource = new TaskCompletionSource<bool>();
        var animation = new Animation(t => callback(Transform(t)));
        animation.Commit(self, Guid.NewGuid().ToString(), 16, length, easing ?? Easing.Linear, (v, c) => taskCompletionSource.SetResult(true));
        return taskCompletionSource.Task;
    }
}
