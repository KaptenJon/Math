using Math.Models;
using System;

namespace Math.Services;

public interface IGameService
{
    Player CurrentPlayer { get; }
    void SetPlayer(string name, int grade, string? avatar = null);
    IReadOnlyList<string> GetCategories();
    void AwardPoints(int points);
    IReadOnlyList<Question> GenerateQuestions(string category, int count = 10);
    void AdjustDifficulty(bool lastAnswerCorrect);
    int CurrentDifficulty { get; }
    int CurrentStreak { get; }
    event Action? AvatarUnlocked;
    IReadOnlyList<string> GetAllAvatars();
}

public class GameService : IGameService
{
    private static readonly Dictionary<int, string[]> GradeCategories = new()
    {
        {0, new[]{"Category_Subtraction","Category_Addition"}},
        {1, new[]{"Category_Subtraction","Category_Addition"}},
        {2, new[]{"Category_Subtraction","Category_Addition","Category_Division","Category_Multiplication"}},
        {3, new[]{"Category_Subtraction","Category_Division","Category_Multiplication","Category_Algebra","Category_ProblemSolving"}},
        {4, new[]{"Category_Division","Category_Multiplication","Category_Algebra","Category_Graphs","Category_ProblemSolving"}},
        {5, new[]{"Category_Division","Category_Multiplication","Category_Algebra","Category_Graphs","Category_ProblemSolving"}},
    };

    // Animal avatar image filenames (must exist in Resources/Images)
    private static readonly string[] BaseAvatars = new[]
    {
        "avatar_cat.png",
        "avatar_dog.png",
        "avatar_fox.png",
        "avatar_panda.png",
        "avatar_lion.png",
        "avatar_tiger.png",
        "avatar_penguin.png",
        "avatar_frog.png",
        "avatar_monkey.png",
        "avatar_unicorn.png"
    };

    private static readonly (int Points, string Avatar)[] Unlockables = new[]
    {
        (50, "avatar_dragon.png"),
        (100, "avatar_crown.png"),
        (150, "avatar_rocket.png"),
        (200, "avatar_star.png"),
    };

    private readonly ILocalizationService _localization;

    public Player CurrentPlayer { get; private set; } = new();
    private readonly Random _rng = new();

    private int _currentDifficulty = 1; // grows/shrinks based on performance
    public int CurrentDifficulty => _currentDifficulty;
    private int _streak = 0; // consecutive correct answers (never reset on difficulty increase)
    public int CurrentStreak => _streak;
    public event Action? AvatarUnlocked;

    public GameService(ILocalizationService localization)
    {
        ArgumentNullException.ThrowIfNull(localization);
        _localization = localization;
    }

    public void SetPlayer(string name, int grade, string? avatar = null)
    {
        CurrentPlayer.Name = name.Trim();
        CurrentPlayer.Grade = System.Math.Clamp(grade, 0, 5);
        if (CurrentPlayer.UnlockedAvatars.Count == 0)
        {
            CurrentPlayer.UnlockedAvatars.AddRange(BaseAvatars);
        }
        if (!string.IsNullOrWhiteSpace(avatar) && CurrentPlayer.UnlockedAvatars.Contains(avatar))
            CurrentPlayer.Avatar = avatar!;
        else
            CurrentPlayer.Avatar = CurrentPlayer.UnlockedAvatars.First();
        _currentDifficulty = 1;
        _streak = 0;
    }

    public IReadOnlyList<string> GetCategories()
    {
        if (GradeCategories.TryGetValue(CurrentPlayer.Grade, out var cats))
            return cats;
        return Array.Empty<string>();
    }

    public void AwardPoints(int points)
    {
        if (points <= 0) return;
        CurrentPlayer.Points += points;
        CheckUnlocks();
    }

    private void CheckUnlocks()
    {
        foreach (var (needed, avatar) in Unlockables)
        {
            if (CurrentPlayer.Points >= needed && !CurrentPlayer.UnlockedAvatars.Contains(avatar))
            {
                CurrentPlayer.UnlockedAvatars.Add(avatar);
                AvatarUnlocked?.Invoke();
            }
        }
    }

    public void AdjustDifficulty(bool lastAnswerCorrect)
    {
        if (lastAnswerCorrect)
        {
            _streak++;
            // Increase difficulty every 2 additional correct answers, without losing streak count
            if (_streak % 2 == 0)
            {
                _currentDifficulty = System.Math.Min(_currentDifficulty + 1, 10);
            }
        }
        else
        {
            _streak = 0;
            _currentDifficulty = System.Math.Max(1, _currentDifficulty - 1);
        }
    }

    public IReadOnlyList<Question> GenerateQuestions(string categoryKey, int count = 10)
    {
        // Map category key to behavior
        string behavior = categoryKey switch
        {
            "Category_Addition" => "Add",
            "Category_Subtraction" => "Sub",
            "Category_Multiplication" => "Mult",
            "Category_Division" => "Div",
            "Category_Algebra" => "Algebra",
            "Category_ProblemSolving" => "Problem",
            "Category_Graphs" => "Graph",
            _ => "Add"
        };

        var list = new List<Question>(count);
        for (int i = 0; i < count; i++)
        {
            list.Add(GenerateQuestion(behavior));
        }
        return list;
    }

    public IReadOnlyList<string> GetAllAvatars()
    {
        // Ensure base avatars are available before a player is fully set up
        if (CurrentPlayer.UnlockedAvatars.Count == 0)
        {
            CurrentPlayer.UnlockedAvatars.AddRange(BaseAvatars);
        }
        return CurrentPlayer.UnlockedAvatars;
    }

    private Question GenerateQuestion(string behavior)
    {
        // Adaptive base: grade + dynamic difficulty multiplier
        int baseMax = 10 + CurrentPlayer.Grade * 10 + _currentDifficulty * 5;
        int a = _rng.Next(0, baseMax);
        int b = _rng.Next(1, baseMax); // avoid zero for division denominator

        return behavior switch
        {
            var c when c.StartsWith("Add", StringComparison.OrdinalIgnoreCase) => new Question { Text = $"{a} + {b} = ?", Answer = a + b },
            var c when c.StartsWith("Sub", StringComparison.OrdinalIgnoreCase) => new Question { Text = $"{a} - {b} = ?", Answer = a - b },
            var c when c.StartsWith("Mult", StringComparison.OrdinalIgnoreCase) => new Question { Text = $"{a} × {b} = ?", Answer = a * b },
            var c when c.StartsWith("Div", StringComparison.OrdinalIgnoreCase) => new Question { Text = $"{a*b} ÷ {b} = ?", Answer = a },
            var c when c.StartsWith("Algebra", StringComparison.OrdinalIgnoreCase) => AlgebraQuestion(),
            var c when c.StartsWith("Problem", StringComparison.OrdinalIgnoreCase) => WordProblem(),
            var c when c.StartsWith("Graph", StringComparison.OrdinalIgnoreCase) => GraphQuestion(),
            _ => new Question { Text = $"{a} + {b} = ?", Answer = a + b }
        };
    }

    private Question AlgebraQuestion()
    {
        int x = _rng.Next(1, 12 + _currentDifficulty);
        int m = _rng.Next(1, 9 + _currentDifficulty);
        int b = _rng.Next(0, 20 + _currentDifficulty * 2);
        int y = m * x + b;
        return new Question { Text = _localization.Get("Question_Algebra", m, b, y), Answer = x };
    }

    private Question WordProblem()
    {
        int apples = _rng.Next(3, 15 + _currentDifficulty);
        int eaten = _rng.Next(1, apples);
        return new Question { Text = _localization.Get("Question_WordProblem", apples, eaten), Answer = apples - eaten };
    }

    private Question GraphQuestion()
    {
        int x1 = 0;
        int y1 = _rng.Next(0, 10 + _currentDifficulty);
        int x2 = _rng.Next(1, 10 + _currentDifficulty);
        int slope = _rng.Next(-5 - _currentDifficulty, 6 + _currentDifficulty);
        int y2 = y1 + slope * (x2 - x1);
        return new Question { Text = _localization.Get("Question_Graph", x1, y1, x2, y2), Answer = slope };
    }
}
