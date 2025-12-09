using Math.Services;
using System.Text;
using Microsoft.Maui.Controls.Shapes;
using System.Globalization;

namespace Math.Pages;

public partial class LoginPage : ContentPage
{
    private class AvatarItem
    {
        public string File { get; set; } = string.Empty;
        public string DisplaySource => File; // use original resource name
    }

    private static readonly string[] ExpectedAvatars = new[]
    {
        "avatar_cat.png","avatar_dog.png","avatar_fox.png","avatar_panda.png","avatar_lion.png","avatar_tiger.png","avatar_penguin.png","avatar_frog.png","avatar_monkey.png","avatar_unicorn.png"
    };

    private readonly IGameService _game;
    private readonly IStorageService _storage;
    private readonly ILocalizationService _loc;
    private readonly Entry _nameEntry;
    private readonly Slider _gradeSlider;
    private readonly Label _gradeLabel;
    private readonly Image _previewImage;
    private readonly Border _previewBorder;
    private readonly CollectionView _avatarCollection;
    private readonly Button _actionBtn; // Start or Save depending on state
    private readonly Picker _languagePicker;
    private List<AvatarItem> _avatars = new();
    private string? _selectedAvatar;
#if DEBUG
    private readonly Label _diagLabel;
    private readonly Image _diagTestImage;
    private readonly Button _copyDiagBtn;
#endif

    public LoginPage(IGameService game, IStorageService storage)
    {
        _game = game;
        _storage = storage;
        _loc = MauiProgram.Services.GetService<ILocalizationService>()!;
        BackgroundColor = Color.FromArgb("#00897B");

        _nameEntry = new Entry { Placeholder = _loc["Placeholder_PlayerName"], FontSize = 20, ClearButtonVisibility = ClearButtonVisibility.WhileEditing };
        _gradeSlider = new Slider { Minimum = 0, Maximum = 5, Value = 0, ThumbColor = Colors.Yellow, MaximumTrackColor = Colors.LightGray, MinimumTrackColor = Colors.Gold };
        _gradeSlider.ValueChanged += OnGradeChanged;
        _gradeLabel = new Label { Text = string.Format(_loc["Label_Grade"], 0), FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Colors.White };

        _previewImage = new Image { HeightRequest = 120, WidthRequest = 120, Aspect = Aspect.AspectFit };
        _previewBorder = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(80) },
            Stroke = new SolidColorBrush(Colors.Gold),
            Background = new SolidColorBrush(Colors.White),
            Padding = 12,
            HorizontalOptions = LayoutOptions.Center,
            Content = _previewImage
        };

        _avatarCollection = new CollectionView
        {
            ItemsLayout = new GridItemsLayout(5, ItemsLayoutOrientation.Vertical) { HorizontalItemSpacing = 8, VerticalItemSpacing = 8 },
            SelectionMode = SelectionMode.Single,
            HeightRequest = 240
        };
        _avatarCollection.SelectionChanged += OnAvatarSelected;
        _avatarCollection.ItemTemplate = new DataTemplate(() =>
        {
            var img = new Image { Aspect = Aspect.AspectFit, HeightRequest = 56, WidthRequest = 56, BackgroundColor = Colors.Transparent };
            img.SetBinding(Image.SourceProperty, nameof(AvatarItem.DisplaySource));
            var border = new Border
            {
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(16) },
                Background = new SolidColorBrush(Color.FromArgb("#13FFFFFF")),
                StrokeThickness = 2,
                Stroke = new SolidColorBrush(Colors.Transparent),
                Padding = 4,
                WidthRequest = 64,
                HeightRequest = 64,
                Content = img
            };
            border.HandlerChanged += (_, _) => ApplySelectionVisual(border);
            return border;
        });

#if DEBUG
        _diagLabel = new Label { TextColor = Colors.Yellow, FontSize = 12, LineBreakMode = LineBreakMode.CharacterWrap };
        _diagTestImage = new Image { Source = "avatar_cat.png", WidthRequest = 80, HeightRequest = 80, Aspect = Aspect.AspectFit, BackgroundColor = Colors.White };
        _copyDiagBtn = new Button { Text = "Copy Diagnostics", FontSize = 12, Padding = new Thickness(8,4) };
        _copyDiagBtn.Clicked += (_, _) => CopyDiagnostics();
#endif

        LoadAvatars();
        UpdateAvatarPreview();

        _actionBtn = new Button { Text = _loc["Button_Start"], FontSize = 24, CornerRadius = 24, BackgroundColor = Colors.Orange, TextColor = Colors.White, Padding = new Thickness(20,14) };
        _actionBtn.Clicked += OnAction;

        _languagePicker = new Picker { Title = _loc["Label_Language"] };
        var langs = _loc.SupportedCultures.ToList();
        _languagePicker.ItemsSource = new[] { _loc["Option_SystemLanguage"] }.Concat(langs.Select(c => c.NativeName)).ToList();
        _languagePicker.SelectedIndexChanged += (_, _) => OnLanguageChanged(langs);

        var title = new Label { Text = _loc["App_Title"], FontSize = 44, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center, TextColor = Colors.White };
        var avatarLabel = new Label { Text = _loc["Label_PickAvatar"], FontAttributes = FontAttributes.Bold, TextColor = Colors.White, FontSize = 20 };

        var mainStack = new VerticalStackLayout
        {
            Padding = new Thickness(30,60,30,30),
            Spacing = 28,
            Children = { title, new Label { Text = _loc["Label_PlayerName"], FontAttributes = FontAttributes.Bold, TextColor = Colors.White, FontSize=20 }, _nameEntry, new Label { Text = _loc["Label_ChooseGrade"], FontAttributes = FontAttributes.Bold, TextColor = Colors.White, FontSize=20 }, _gradeSlider, _gradeLabel, new HorizontalStackLayout { Spacing = 12, Children = { new Label{ Text = _loc["Label_Language"], TextColor = Colors.White, VerticalTextAlignment = TextAlignment.Center }, _languagePicker } }, avatarLabel, _avatarCollection, _previewBorder, _actionBtn }
        };
#if DEBUG
        mainStack.Children.Add(new Label { Text = "Diagnostics (DEBUG only):", TextColor = Colors.White, FontAttributes = FontAttributes.Bold, FontSize = 14 });
        var diagRow = new HorizontalStackLayout { Spacing = 12, Children = { _diagTestImage, new ScrollView { Content = _diagLabel, WidthRequest = 220, Orientation = ScrollOrientation.Vertical } } };
        mainStack.Children.Add(diagRow);
        mainStack.Children.Add(_copyDiagBtn);
#endif
        Content = new ScrollView { Content = mainStack };
        Title = _loc["Login_Title"];

        _loc.LanguageChanged += () =>
        {
            // Refresh all labels
            Title = _game.CurrentPlayer.Name.Length > 0 ? _loc["Profile_Title"] : _loc["Login_Title"];
            _nameEntry.Placeholder = _loc["Placeholder_PlayerName"];
            _gradeLabel.Text = string.Format(_loc["Label_Grade"], (int)_gradeSlider.Value);
            _actionBtn.Text = string.IsNullOrWhiteSpace(_game.CurrentPlayer.Name) ? _loc["Button_Start"] : _loc["Button_Save"];
        };
#if DEBUG
        MainThread.BeginInvokeOnMainThread(RunDiagnostics);
#endif
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Determine if we already have a player => switch to Profile mode
        bool hasPlayer = !string.IsNullOrWhiteSpace(_game.CurrentPlayer.Name);
        if (hasPlayer)
        {
            EnterProfileMode();
        }
        else
        {
            EnterLoginMode();
        }

        // Initialize language picker selection
        var saved = _game.CurrentPlayer.Language;
        if (string.IsNullOrWhiteSpace(saved)) _languagePicker.SelectedIndex = 0; // system
        else
        {
            var idx = _loc.SupportedCultures.ToList().FindIndex(c => string.Equals(c.Name, saved, StringComparison.OrdinalIgnoreCase));
            _languagePicker.SelectedIndex = idx >= 0 ? idx + 1 : 0;
        }
    }

    private void OnLanguageChanged(List<CultureInfo> list)
    {
        int idx = _languagePicker.SelectedIndex;
        if (idx <= 0)
        {
            // system default
            _game.CurrentPlayer.Language = string.Empty;
            _loc.SetCulture(CultureInfo.CurrentUICulture);
        }
        else
        {
            var culture = list[idx - 1];
            _game.CurrentPlayer.Language = culture.Name;
            _loc.SetCulture(culture);
        }
        _ = _storage.SavePlayerAsync(_game.CurrentPlayer);
        UpdateTabTitle();
    }

    private void EnterProfileMode()
    {
        Title = _loc["Profile_Title"];
        _actionBtn.Text = _loc["Button_Save"];
        var player = _game.CurrentPlayer;
        _nameEntry.Text = player.Name;
        _gradeSlider.Value = player.Grade;
        _gradeLabel.Text = string.Format(_loc["Label_Grade"], player.Grade);
        if (!string.IsNullOrWhiteSpace(player.Avatar))
        {
            _selectedAvatar = player.Avatar;
            UpdateAvatarPreview();
            RefreshAvatarItems();
        }
        UpdateTabTitle();
    }

    private void EnterLoginMode()
    {
        Title = _loc["Login_Title"];
        _actionBtn.Text = _loc["Button_Start"];
        UpdateTabTitle();
    }

    private void UpdateTabTitle()
    {
        // Attempt to update parent Tab title dynamically
        Element? p = this;
        while (p != null && p is not Tab) p = p.Parent;
        if (p is Tab tab)
        {
            tab.Title = Title;
        }
    }

    private void LoadAvatars()
    {
        _avatars = _game.GetAllAvatars().Select(a => new AvatarItem { File = a }).ToList();
        if (_avatars.Count > 0 && _selectedAvatar == null) _selectedAvatar = _avatars[0].File;
        _avatarCollection.ItemsSource = _avatars;
    }

    private void OnAvatarSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is AvatarItem item)
        {
            _selectedAvatar = item.File;
            UpdateAvatarPreview();
            RefreshAvatarItems();
        }
    }

    private void RefreshAvatarItems()
    {
        var items = _avatars;
        _avatarCollection.ItemsSource = null;
        _avatarCollection.ItemsSource = items;
    }

    private void ApplySelectionVisual(Border border)
    {
        if (border.BindingContext is AvatarItem ai)
        {
            bool selected = ai.File == _selectedAvatar;
            border.Stroke = new SolidColorBrush(selected ? Colors.Gold : Colors.Transparent);
            border.Background = new SolidColorBrush(selected ? Color.FromArgb("#55FFFFFF") : Color.FromArgb("#13FFFFFF"));
        }
    }

    private void UpdateAvatarPreview() => _previewImage.Source = _selectedAvatar;

    private void OnGradeChanged(object? sender, ValueChangedEventArgs e) => _gradeLabel.Text = string.Format(_loc["Label_Grade"], (int)e.NewValue);

    private async void OnAction(object? sender, EventArgs e)
    {
        var name = _nameEntry.Text ?? string.Empty;
        var grade = (int)_gradeSlider.Value;
        var avatar = _selectedAvatar;
        bool isNew = string.IsNullOrWhiteSpace(_game.CurrentPlayer.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlertAsync(_loc["Alert_NameRequired_Title"], _loc["Alert_NameRequired_Message"], _loc["Alert_OK"]);
            return;
        }
        _game.SetPlayer(name, grade, avatar);
        _game.CurrentPlayer.Language = _game.CurrentPlayer.Language; // keep selected
        await _storage.SavePlayerAsync(_game.CurrentPlayer);
        if (isNew)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
        else
        {
            await DisplayAlertAsync(_loc["Alert_Saved_Title"], _loc["Alert_Saved_Message"], _loc["Alert_OK"]);
        }
    }

#if DEBUG
    private string _lastDiagnostics = string.Empty;
    private async void RunDiagnostics()
    {
        var sb = new StringBuilder();
        foreach (var f in ExpectedAvatars)
        {
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync(f);
                sb.AppendLine($"{f}: OK ({stream.Length} bytes)");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"{f}: FAIL {ex.GetType().Name} - {ex.Message}");
            }
        }
        _diagTestImage.Source = "avatar_cat.png";
        _lastDiagnostics = sb.ToString();
        _diagLabel.Text = _lastDiagnostics;
    }

    private async void CopyDiagnostics()
    {
        if (!string.IsNullOrEmpty(_lastDiagnostics))
        {
            await Clipboard.Default.SetTextAsync(_lastDiagnostics);
            await DisplayAlertAsync("Copied","Diagnostics copied to clipboard","OK");
        }
    }
#endif
}
