namespace Math
{
    public partial class AppShell : Shell
    {
        private readonly Services.ILocalizationService _loc = MauiProgram.Services.GetService<Services.ILocalizationService>()!;
        private Tab _profileTab = null!;
        private Tab _mainTab = null!;
        private Tab _statsTab = null!;

        public AppShell()
        {
            // Register routes explicitly and set initial tab dynamically later.
            Routing.RegisterRoute("ProfilePage", typeof(Pages.LoginPage));
            Routing.RegisterRoute("MainPage", typeof(MainPage));
            Routing.RegisterRoute("StatsPage", typeof(Pages.StatsPage));

            // Profile Tab (previously Login)
            _profileTab = new Tab
            {
                Route = "ProfileTab",
                Title = _loc["Profile_Title"],
                Items =
                {
                    new ShellContent
                    {
                        Route = "ProfilePage",
                        Title = _loc["Profile_Title"],
                        ContentTemplate = new DataTemplate(() => MauiProgram.Services.GetService<Pages.LoginPage>()!)
                    }
                }
            };
            
            // Main/Home Tab
            _mainTab = new Tab
            {
                Route = "MainTab",
                Title = _loc["Home_Title"],
                Items =
                {
                    new ShellContent
                    {
                        Route = "MainPage",
                        Title = _loc["Home_Title"],
                        ContentTemplate = new DataTemplate(() => MauiProgram.Services.GetService<MainPage>()!)
                    }
                }
            };
            
            // Stats Tab
            _statsTab = new Tab
            {
                Route = "StatsTab",
                Title = _loc["Stats_Title"],
                Items =
                {
                    new ShellContent
                    {
                        Route = "StatsPage",
                        Title = _loc["Stats_Title"],
                        ContentTemplate = new DataTemplate(() => MauiProgram.Services.GetService<Pages.StatsPage>()!)
                    }
                }
            };

            Items.Add(_profileTab);
            Items.Add(_mainTab);
            Items.Add(_statsTab);

            _loc.LanguageChanged += () =>
            {
                _profileTab.Title = _loc["Profile_Title"];
                if (_profileTab.Items.FirstOrDefault() is ShellContent sc1) sc1.Title = _loc["Profile_Title"];
                _mainTab.Title = _loc["Home_Title"];
                if (_mainTab.Items.FirstOrDefault() is ShellContent sc2) sc2.Title = _loc["Home_Title"];
                _statsTab.Title = _loc["Stats_Title"];
                if (_statsTab.Items.FirstOrDefault() is ShellContent sc3) sc3.Title = _loc["Stats_Title"];
            };
        }
    }
}
