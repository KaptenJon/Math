namespace Math
{
    public partial class App : Application
    {
        private readonly Services.IStorageService _storage;
        private readonly Services.IGameService _game;
        private readonly Services.ILocalizationService _loc;
        private bool _hasProfile;

        public App()
        {
            _storage = MauiProgram.Services.GetService<Services.IStorageService>()!;
            _game = MauiProgram.Services.GetService<Services.IGameService>()!;
            _loc = MauiProgram.Services.GetService<Services.ILocalizationService>()!;
            TryLoadProfile();
        }

        private void TryLoadProfile()
        {
            try
            {
                var existing = _storage.LoadPlayerAsync().GetAwaiter().GetResult();
                if (existing != null && !string.IsNullOrWhiteSpace(existing.Name))
                {
                    _hasProfile = true;
                    _game.SetPlayer(existing.Name, existing.Grade, existing.Avatar);
                    _game.AwardPoints(existing.Points - _game.CurrentPlayer.Points);
                    if (!string.IsNullOrWhiteSpace(existing.Language))
                    {
                        try { _loc.SetCulture(new System.Globalization.CultureInfo(existing.Language)); } catch { }
                    }
                }
            }
            catch { _hasProfile = false; }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var shell = new AppShell();
            // Select the correct root tab
            if (shell.Items.FirstOrDefault(i => i.Route == (_hasProfile ? "MainTab" : "LoginTab")) is ShellItem item)
                shell.CurrentItem = item;

            // Ensure navigation stack reflects chosen root
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try { shell.GoToAsync(_hasProfile ? "//MainPage" : "//LoginPage"); } catch { }
            });

            return new Window(shell);
        }
    }
}