using Microsoft.Extensions.Logging;
using Math.Services;

namespace Math
{
    public static class MauiProgram
    {
        public static IServiceProvider Services => _services ?? throw new InvalidOperationException("App not built yet");
        private static IServiceProvider? _services;

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton<IGameService, GameService>();
            builder.Services.AddSingleton<IStorageService, StorageService>();
            builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
            builder.Services.AddSingleton<Pages.LoginPage>();
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<Pages.StatsPage>();
            builder.Services.AddTransient<Pages.QuizPageFactory>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();
            _services = app.Services;            
            return app;
        }
    }
}
