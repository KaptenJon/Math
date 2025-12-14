using System.Globalization;
using System.Resources;

namespace Math.Services;

public interface ILocalizationService
{
    CultureInfo CurrentCulture { get; }
    IReadOnlyList<CultureInfo> SupportedCultures { get; }
    void SetCulture(CultureInfo culture, bool setAsDefault = true);
    string this[string key] { get; }
    string Get(string key, params object[] args);
    event Action? LanguageChanged;
}

public sealed class LocalizationService : ILocalizationService
{
    // Prefer in-app dictionaries; attempt to read from RESX if present (future-proof)
    private readonly ResourceManager _rm = new("Math.Resources.Strings.AppResources", typeof(LocalizationService).Assembly);

    private readonly Dictionary<string, Dictionary<string, string>> _strings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["App_Title"] = "Math Quest",
            ["Login_Title"] = "Login",
            ["Profile_Title"] = "Profile",
            ["Home_Title"] = "Home",
            ["Button_Start"] = "Start Adventure!",
            ["Button_Save"] = "Save",
            ["Label_PlayerName"] = "Player Name",
            ["Placeholder_PlayerName"] = "Your awesome name",
            ["Label_ChooseGrade"] = "Choose Grade (0-5)",
            ["Label_Grade"] = "Grade: {0}",
            ["Label_PickAvatar"] = "Pick Your Avatar",
            ["Label_Language"] = "Language",
            ["Option_SystemLanguage"] = "Device language",
            ["Alert_NameRequired_Title"] = "Hold on!",
            ["Alert_NameRequired_Message"] = "Please enter a name to begin your quest.",
            ["Alert_OK"] = "OK",
            ["Alert_Saved_Title"] = "Saved",
            ["Alert_Saved_Message"] = "Profile updated",
            ["Main_PickChallenge"] = "Pick a Challenge!",
            ["Main_Categories_Title"] = "Categories",
            ["Main_Welcome"] = "Welcome",
            ["Main_WelcomeWithName"] = "Welcome {0}! (Grade {1})",
            ["Main_Points"] = "Points: {0}",
            ["Quiz_Submit"] = "OK",
            ["Quiz_Clear"] = "CLR",
            ["Quiz_Progress"] = "Question {0} of {1}",
            ["Quiz_Difficulty"] = "Diff: {0}",
            ["Quiz_Streak"] = "Streak: {0}",
            ["Quiz_EnterAnswer_Title"] = "Oops",
            ["Quiz_EnterAnswer_Message"] = "Please enter an answer",
            ["Quiz_Answer"] = "Answer: {0}",
            ["Quiz_Finished_Title"] = "Finished",
            ["Quiz_Finished_Message"] = "You answered {0} / {1}! Points total: {2}",
            ["Quiz_Continue"] = "Continue",
            ["Quiz_GoBack"] = "Go Back",
            ["Question_Algebra"] = "If y = {0}x + {1} and y = {2}, what is x?",
            ["Question_WordProblem"] = "You have {0} apples and eat {1}. How many left?",
            ["Question_Graph"] = "Line through ({0},{1}) and ({2},{3}). What is the slope?",
            ["CheerMessages"] = "Great!|Awesome!|You rock!|Math star!|Super!|Brilliant!|Yes!|Keep going!",
            ["Category_Addition"] = "Addition",
            ["Category_Subtraction"] = "Subtraction",
            ["Category_Division"] = "Division",
            ["Category_Multiplication"] = "Multiplication",
            ["Category_Algebra"] = "Algebra",
            ["Category_ProblemSolving"] = "Problem Solving",
            ["Category_Graphs"] = "Graphs",
            ["Stats_Title"] = "Statistics",
            ["Stats_Overall"] = "Overall Stats",
            ["Stats_Today"] = "Today",
            ["Stats_ThisWeek"] = "This Week",
            ["Stats_ThisMonth"] = "This Month",
            ["Stats_Sessions"] = "Sessions: {0}",
            ["Stats_Questions"] = "Questions: {0}",
            ["Stats_Lessons"] = "Lessons: {0}",
            ["Stats_Accuracy"] = "Accuracy: {0}%",
            ["Stats_Points"] = "Points: {0}",
            ["Stats_NoData"] = "No data yet. Start a quiz to see your stats!",
        },
        ["sv"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["App_Title"] = "Math Quest",
            ["Login_Title"] = "Logga in",
            ["Profile_Title"] = "Profil",
            ["Home_Title"] = "Hem",
            ["Button_Start"] = "Börja äventyret!",
            ["Button_Save"] = "Spara",
            ["Label_PlayerName"] = "Spelarnamn",
            ["Placeholder_PlayerName"] = "Ditt fantastiska namn",
            ["Label_ChooseGrade"] = "Välj årskurs (0-5)",
            ["Label_Grade"] = "Årskurs: {0}",
            ["Label_PickAvatar"] = "Välj din avatar",
            ["Label_Language"] = "Språk",
            ["Option_SystemLanguage"] = "Enhetens språk",
            ["Alert_NameRequired_Title"] = "Vänta!",
            ["Alert_NameRequired_Message"] = "Ange ett namn för att starta.",
            ["Alert_OK"] = "OK",
            ["Alert_Saved_Title"] = "Sparat",
            ["Alert_Saved_Message"] = "Profilen uppdaterad",
            ["Main_PickChallenge"] = "Välj en utmaning!",
            ["Main_Categories_Title"] = "Kategorier",
            ["Main_Welcome"] = "Välkommen",
            ["Main_WelcomeWithName"] = "Välkommen {0}! (Årskurs {1})",
            ["Main_Points"] = "Poäng: {0}",
            ["Quiz_Submit"] = "OK",
            ["Quiz_Clear"] = "Rensa",
            ["Quiz_Progress"] = "Fråga {0} av {1}",
            ["Quiz_Difficulty"] = "Svår: {0}",
            ["Quiz_Streak"] = "Streak: {0}",
            ["Quiz_EnterAnswer_Title"] = "Hoppsan",
            ["Quiz_EnterAnswer_Message"] = "Ange ett svar",
            ["Quiz_Answer"] = "Svar: {0}",
            ["Quiz_Finished_Title"] = "Klart",
            ["Quiz_Finished_Message"] = "Du svarade {0} / {1}! Totalt: {2}",
            ["Quiz_Continue"] = "Fortsätt",
            ["Quiz_GoBack"] = "Gå tillbaka",
            ["Question_Algebra"] = "Om y = {0}x + {1} och y = {2}, vad är x?",
            ["Question_WordProblem"] = "Du har {0} äpplen och äter {1}. Hur många blir kvar?",
            ["Question_Graph"] = "Linje genom ({0},{1}) och ({2},{3}). Vad är lutningen?",
            ["CheerMessages"] = "Bra!|Grymt!|Du är bäst!|Mattestjärna!|Super!|Briljant!|Yes!|Fortsätt!",
            ["Category_Addition"] = "Addition",
            ["Category_Subtraction"] = "Subtraktion",
            ["Category_Division"] = "Division",
            ["Category_Multiplication"] = "Multiplikation",
            ["Category_Algebra"] = "Algebra",
            ["Category_ProblemSolving"] = "Problemlösning",
            ["Category_Graphs"] = "Grafer",
            ["Stats_Title"] = "Statistik",
            ["Stats_Overall"] = "Totalt",
            ["Stats_Today"] = "Idag",
            ["Stats_ThisWeek"] = "Denna vecka",
            ["Stats_ThisMonth"] = "Denna månad",
            ["Stats_Sessions"] = "Sessioner: {0}",
            ["Stats_Questions"] = "Frågor: {0}",
            ["Stats_Lessons"] = "Lektioner: {0}",
            ["Stats_Accuracy"] = "Rätt svar: {0}%",
            ["Stats_Points"] = "Poäng: {0}",
            ["Stats_NoData"] = "Ingen data än. Börja ett quiz för att se dina statistik!",
        }
    };

    public event Action? LanguageChanged;

    public CultureInfo CurrentCulture { get; private set; } = CultureInfo.CurrentUICulture;

    private static readonly List<CultureInfo> _supported =
    [
        new CultureInfo("en"),
        new CultureInfo("sv"),
    ];

    public IReadOnlyList<CultureInfo> SupportedCultures => _supported;

    public void SetCulture(CultureInfo culture, bool setAsDefault = true)
    {
        CurrentCulture = culture;
        if (setAsDefault)
        {
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
        }
        LanguageChanged?.Invoke();
    }

    public string this[string key] => Lookup(key);

    public string Get(string key, params object[] args)
    {
        var fmt = Lookup(key);
        return args is { Length: > 0 } ? string.Format(CurrentCulture, fmt, args) : fmt;
    }

    private string Lookup(string key)
    {
        var lang = CurrentCulture.TwoLetterISOLanguageName.ToLowerInvariant();
        if (_strings.TryGetValue(lang, out var map) && map.TryGetValue(key, out var value))
            return value;
        // try neutral English fallback
        if (_strings["en"].TryGetValue(key, out var en)) return en;
        // try RESX as ultimate fallback (if added later)
        try { return _rm.GetString(key, CurrentCulture) ?? key; } catch { return key; }
    }
}
