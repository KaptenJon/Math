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
            // Difficulty increases more frequently for higher grades
            int streakThreshold = CurrentPlayer.Grade switch
            {
                0 or 1 => 3,  // Grade 0-1: increase every 3 correct answers
                2 or 3 => 2,  // Grade 2-3: increase every 2 correct answers
                _ => 1        // Grade 4-5: increase every correct answer
            };
            
            if (_streak % streakThreshold == 0)
            {
                // Max difficulty scales with grade: grade 0-1 max 6, grade 2-3 max 8, grade 4-5 max 10
                int maxDifficulty = 6 + (CurrentPlayer.Grade / 2) * 2;
                _currentDifficulty = System.Math.Min(_currentDifficulty + 1, maxDifficulty);
            }
        }
        else
        {
            _streak = 0;
            // Reduce difficulty but not as aggressively for higher grades
            int reduction = CurrentPlayer.Grade >= 3 ? 1 : 2;
            _currentDifficulty = System.Math.Max(1, _currentDifficulty - reduction);
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
        // Higher grades get bigger numbers and more challenging problems
        int gradeMultiplier = CurrentPlayer.Grade switch
        {
            0 => 10,   // Grade 0: numbers up to 10 + difficulty
            1 => 15,   // Grade 1: numbers up to 15 + difficulty
            2 => 20,   // Grade 2: numbers up to 20 + difficulty
            3 => 30,   // Grade 3: numbers up to 30 + difficulty
            4 => 50,   // Grade 4: numbers up to 50 + difficulty
            _ => 100   // Grade 5: numbers up to 100 + difficulty
        };
        
        int baseMax = gradeMultiplier + _currentDifficulty * 5;
        int a = _rng.Next(0, baseMax);
        int b = _rng.Next(1, baseMax); // avoid zero for division denominator

        return behavior switch
        {
            var c when c.StartsWith("Add", StringComparison.OrdinalIgnoreCase) => new Question { Text = $"{a} + {b} = ?", Answer = a + b },
            var c when c.StartsWith("Sub", StringComparison.OrdinalIgnoreCase) => new Question { Text = $"{a} - {b} = ?", Answer = a - b },
            var c when c.StartsWith("Mult", StringComparison.OrdinalIgnoreCase) => GenerateMultiplicationQuestion(a, b),
            var c when c.StartsWith("Div", StringComparison.OrdinalIgnoreCase) => GenerateDivisionQuestion(a, b),
            var c when c.StartsWith("Algebra", StringComparison.OrdinalIgnoreCase) => AlgebraQuestion(),
            var c when c.StartsWith("Problem", StringComparison.OrdinalIgnoreCase) => WordProblem(),
            var c when c.StartsWith("Graph", StringComparison.OrdinalIgnoreCase) => GraphQuestion(),
            _ => new Question { Text = $"{a} + {b} = ?", Answer = a + b }
        };
    }

    private Question GenerateMultiplicationQuestion(int a, int b)
    {
        // For lower grades and difficulties, use smaller numbers for multiplication
        if (CurrentPlayer.Grade <= 2 || _currentDifficulty <= 3)
        {
            // Multiplication tables (1-12)
            a = _rng.Next(1, System.Math.Min(12, 5 + _currentDifficulty));
            b = _rng.Next(1, System.Math.Min(12, 5 + _currentDifficulty));
        }
        else
        {
            // Higher grades can handle larger multiplications
            int maxVal = CurrentPlayer.Grade switch
            {
                3 => 15 + _currentDifficulty * 2,
                4 => 20 + _currentDifficulty * 3,
                _ => 30 + _currentDifficulty * 4
            };
            a = _rng.Next(1, maxVal);
            b = _rng.Next(1, maxVal);
        }
        return new Question { Text = $"{a} × {b} = ?", Answer = a * b };
    }

    private Question GenerateDivisionQuestion(int a, int b)
    {
        // Make sure division gives whole numbers
        // For lower grades, use multiplication tables
        if (CurrentPlayer.Grade <= 2 || _currentDifficulty <= 3)
        {
            a = _rng.Next(1, System.Math.Min(12, 5 + _currentDifficulty));
            b = _rng.Next(1, System.Math.Min(12, 5 + _currentDifficulty));
        }
        else
        {
            int maxVal = CurrentPlayer.Grade switch
            {
                3 => 15 + _currentDifficulty * 2,
                4 => 20 + _currentDifficulty * 3,
                _ => 30 + _currentDifficulty * 4
            };
            a = _rng.Next(1, maxVal);
            b = _rng.Next(1, maxVal);
        }
        return new Question { Text = $"{a * b} ÷ {b} = ?", Answer = a };
    }

    private Question AlgebraQuestion()
    {
        // Scale algebra complexity with grade and difficulty
        int maxX = CurrentPlayer.Grade switch
        {
            3 => 10 + _currentDifficulty,
            4 => 15 + _currentDifficulty * 2,
            _ => 20 + _currentDifficulty * 2
        };
        
        int maxM = CurrentPlayer.Grade switch
        {
            3 => 5 + _currentDifficulty,
            4 => 8 + _currentDifficulty,
            _ => 12 + _currentDifficulty * 2
        };
        
        int maxB = CurrentPlayer.Grade switch
        {
            3 => 10 + _currentDifficulty,
            4 => 20 + _currentDifficulty * 2,
            _ => 30 + _currentDifficulty * 3
        };

        int x = _rng.Next(1, maxX);
        int m = _rng.Next(1, maxM);
        int b = _rng.Next(0, maxB);
        int y = m * x + b;
        return new Question { Text = _localization.Get("Question_Algebra", m, b, y), Answer = x };
    }

    private Question WordProblem()
    {
        // Scale word problem complexity with grade and difficulty
        int maxApples = CurrentPlayer.Grade switch
        {
            3 => 10 + _currentDifficulty * 2,
            4 => 20 + _currentDifficulty * 3,
            _ => 30 + _currentDifficulty * 4
        };
        
        int apples = _rng.Next(3, maxApples);
        int eaten = _rng.Next(1, apples);
        return new Question { Text = _localization.Get("Question_WordProblem", apples, eaten), Answer = apples - eaten };
    }

    private Question GraphQuestion()
    {
        // Scale graph complexity with grade and difficulty
        int maxCoordinate = CurrentPlayer.Grade switch
        {
            4 => 10 + _currentDifficulty * 2,
            _ => 15 + _currentDifficulty * 3
        };
        
        int maxSlope = CurrentPlayer.Grade switch
        {
            4 => 5 + _currentDifficulty,
            _ => 8 + _currentDifficulty * 2
        };

        int x1 = 0;
        int y1 = _rng.Next(0, maxCoordinate);
        int x2 = _rng.Next(1, maxCoordinate);
        int slope = _rng.Next(-maxSlope, maxSlope + 1);
        int y2 = y1 + slope * (x2 - x1);
        return new Question { Text = _localization.Get("Question_Graph", x1, y1, x2, y2), Answer = slope };
    }
}
