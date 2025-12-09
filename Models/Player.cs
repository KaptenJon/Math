namespace Math.Models;

public class Player
{
    public string Name { get; set; } = string.Empty;
    public int Grade { get; set; } // 0-5
    public int Points { get; set; }
    // Avatar now refers to an image file (in Resources/Images)
    public string Avatar { get; set; } = "avatar_cat.svg";
    public List<string> UnlockedAvatars { get; } = new();
    // Preferred UI language (culture name like "en", "sv-SE"). Empty means system default
    public string Language { get; set; } = string.Empty;
    
    // Statistics tracking
    public List<SessionStat> SessionStats { get; } = new();
    
    public int GetTotalLessons() => SessionStats.Sum(s => s.TotalQuestions);
    public int GetTotalCorrect() => SessionStats.Sum(s => s.CorrectAnswers);
    public double GetOverallAccuracy() => GetTotalLessons() == 0 ? 0 : (double)GetTotalCorrect() / GetTotalLessons() * 100;
}

public class SessionStat
{
    public DateTime CompletedAt { get; set; }
    public string Category { get; set; } = string.Empty;
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public int PointsEarned { get; set; }
    public double Accuracy => TotalQuestions == 0 ? 0 : (double)CorrectAnswers / TotalQuestions * 100;
}
