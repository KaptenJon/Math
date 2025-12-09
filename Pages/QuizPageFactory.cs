using Math.Services;

namespace Math.Pages;

public class QuizPageFactory
{
    private readonly IGameService _gameService;
    private readonly IStorageService _storageService;
    public QuizPageFactory(IGameService gameService, IStorageService storageService)
    {
        _gameService = gameService;
        _storageService = storageService;
    }
    public Page Create(string category) => new QuizPage(_gameService, _storageService, category);
}
