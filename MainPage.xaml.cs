using Math.Services;
using Microsoft.Maui.Controls.Shapes;

namespace Math
{
    public partial class MainPage : ContentPage
    {
        private readonly IGameService _gameService;
        private readonly Pages.QuizPageFactory _quizFactory;
        private readonly ILocalizationService _loc;
        private readonly Label _welcomeLabel;
        private readonly CollectionView _categoriesView;
        private readonly Label _pointsLabel;
        private readonly Image _avatarImage;

        public MainPage(IGameService gameService, Pages.QuizPageFactory quizFactory)
        {
            _gameService = gameService;
            _quizFactory = quizFactory;
            _loc = MauiProgram.Services.GetService<ILocalizationService>()!;

            _avatarImage = new Image { HeightRequest = 70, WidthRequest = 70, Aspect = Aspect.AspectFit, Margin = new Thickness(0,0,12,0) };
            _welcomeLabel = new Label { FontSize = 26, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#FFFAF0"), VerticalTextAlignment = TextAlignment.Center };
            _categoriesView = new CollectionView { SelectionMode = SelectionMode.Single };            
            _categoriesView.SelectionChanged += OnCategorySelected;
            _pointsLabel = new Label { FontAttributes = FontAttributes.Bold, FontSize = 18, TextColor = Color.FromArgb("#FFD700") };

            _categoriesView.ItemsLayout = new GridItemsLayout(2, ItemsLayoutOrientation.Vertical)
            {
                HorizontalItemSpacing = 12,
                VerticalItemSpacing = 12
            };

            _categoriesView.ItemTemplate = new DataTemplate(() =>
            {
                var label = new Label { FontSize = 20, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center, TextColor = Colors.White };
                label.SetBinding(Label.TextProperty, ".");
                var border = new Border
                {
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 },
                    Background = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 1),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop { Color = RandomChristmasColor(), Offset = 0.0f },
                            new GradientStop { Color = RandomChristmasColor(), Offset = 1.0f }
                        }
                    },
                    Stroke = new SolidColorBrush(Color.FromArgb("#FFD700")),
                    StrokeThickness = 2,
                    Padding = 8,
                    Content = new VerticalStackLayout
                    {
                        VerticalOptions = LayoutOptions.Center,
                        Children = { label }
                    },
                    HeightRequest = 110,
                    Shadow = new Shadow
                    {
                        Brush = new SolidColorBrush(Colors.Black),
                        Opacity = 0.4f,
                        Radius = 8,
                        Offset = new Point(4, 4)
                    }
                };
                return border;
            });

            var grid = new Grid
            {
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop { Color = Color.FromArgb("#1A4D2E"), Offset = 0.0f },  // Dark forest green
                        new GradientStop { Color = Color.FromArgb("#2C6B3F"), Offset = 0.5f },  // Medium green
                        new GradientStop { Color = Color.FromArgb("#8B0000"), Offset = 1.0f }   // Dark red
                    }
                },
                Padding = new Thickness(20,40,20,20),
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Star),
                    new RowDefinition(GridLength.Auto)
                }
            };

            var headerLayout = new HorizontalStackLayout
            {
                Spacing = 0,
                Children = { _avatarImage, _welcomeLabel }
            };
            grid.Add(headerLayout);
            Grid.SetRow(headerLayout,0);

            var subtitle = new Label 
            { 
                Text = _loc["Main_PickChallenge"], 
                FontSize = 22, 
                TextColor = Color.FromArgb("#FFFAF0"), 
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 10, 0, 0)
            };
            grid.Add(subtitle);
            Grid.SetRow(subtitle,1);
            grid.Add(_categoriesView);
            Grid.SetRow(_categoriesView,2);
            
            // Points display with festive styling
            var pointsBorder = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 15 },
                Background = new SolidColorBrush(Color.FromArgb("#8B0000")),
                Stroke = new SolidColorBrush(Color.FromArgb("#FFD700")),
                StrokeThickness = 2,
                Padding = new Thickness(15, 8),
                HorizontalOptions = LayoutOptions.Center,
                Content = _pointsLabel
            };
            grid.Add(pointsBorder);
            Grid.SetRow(pointsBorder,3);

            Content = grid;
            Title = _loc["Main_Categories_Title"];

            _loc.LanguageChanged += () =>
            {
                Title = _loc["Main_Categories_Title"];
                UpdateTexts();
            };
        }

        private Color RandomChristmasColor()
        {
            var rnd = System.Random.Shared;
            var christmasColors = new[] 
            { 
                "#C41E3A",  // Christmas red
                "#165B33",  // Christmas green
                "#FFD700",  // Gold
                "#8B0000",  // Dark red
                "#006400",  // Dark green
                "#FF6B6B",  // Light red
                "#43A047"   // Light green
            };
            return Color.FromArgb(christmasColors[rnd.Next(christmasColors.Length)]);
        }

        private Color RandomColor()
        {
            var rnd = System.Random.Shared;
            var palette = new[] { "#EF5350","#AB47BC","#5C6BC0","#29B6F6","#66BB6A","#FFA726","#EC407A","#FFCA28" };
            return Color.FromArgb(palette[rnd.Next(palette.Length)]);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            UpdateTexts();
        }

        private void UpdateTexts()
        {
            var player = _gameService.CurrentPlayer;
            _welcomeLabel.Text = string.IsNullOrWhiteSpace(player.Name) ? _loc["Main_Welcome"] : _loc.Get("Main_WelcomeWithName", player.Name, player.Grade);
            if (!string.IsNullOrEmpty(player.Avatar))
            {
                _avatarImage.Source = player.Avatar;
                _avatarImage.IsVisible = true;
            }
            else
            {
                _avatarImage.IsVisible = false;
            }
            var keys = _gameService.GetCategories();
            _categoriesView.ItemsSource = keys.Select(k => _loc[k]).ToList();
            _pointsLabel.Text = _loc.Get("Main_Points", player.Points);
        }

        private async void OnCategorySelected(object? sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is string localized)
            {
                // Map localized text back to key
                var key = _gameService.GetCategories().FirstOrDefault(k => _loc[k] == localized) ?? _gameService.GetCategories().First();
                var quizPage = _quizFactory.Create(key);
                var navRoute = nameof(Pages.QuizPage);
                Routing.RegisterRoute(navRoute, typeof(Pages.QuizPage));
                await Shell.Current.Navigation.PushAsync(quizPage);
                _categoriesView.SelectedItem = null;
            }
        }
    }
}
