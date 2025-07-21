using ConsoleApp1.Model.DTO.Questions;
using ConsoleApp1.Model.DTO.Rooms.Games;
using ConsoleApp1.Model.Entity.Rooms;
using ConsoleApp1.Repository.Interface;
using ConsoleApp1.Service.Interface;
using System.Collections.Generic;

namespace ConsoleApp1.Service.Implement;

public class GameSessionServiceImplement : IGameSessionService
{
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly IGameQuestionRepository _gameQuestionRepository;
    private readonly IQuestionRepository _questionRepository;

    public GameSessionServiceImplement(
        IGameSessionRepository gameSessionRepository,
        IGameQuestionRepository gameQuestionRepository,
        IQuestionRepository questionRepository)
    {
        _gameSessionRepository = gameSessionRepository;
        _gameQuestionRepository = gameQuestionRepository;
        _questionRepository = questionRepository;
    }

    public async Task<GameSessionDTO> GetByIdAsync(int id)
    {
        var gameSession = await _gameSessionRepository.GetByIdAsync(id);
        if (gameSession == null)
            return null;

        return MapToDTO(gameSession);
    }

    public async Task<GameSessionDTO> GetByRoomIdAsync(int roomId)
    {
        var gameSession = await _gameSessionRepository.GetByRoomIdAsync(roomId);
        if (gameSession == null)
            return null;

        return MapToDTO(gameSession);
    }

    public async Task<int> CreateAsync(GameSession gameSession)
    {
        return await _gameSessionRepository.CreateAsync(gameSession);
    }

    public async Task<bool> UpdateAsync(GameSession gameSession)
    {
        return await _gameSessionRepository.UpdateAsync(gameSession);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _gameSessionRepository.DeleteAsync(id);
    }

    public async Task<bool> UpdateGameStateAsync(int id, string gameState)
    {
        return await _gameSessionRepository.UpdateGameStateAsync(id, gameState);
    }

    public async Task<bool> UpdateCurrentQuestionIndexAsync(int id, int questionIndex)
    {
        return await _gameSessionRepository.UpdateCurrentQuestionIndexAsync(id, questionIndex);
    }

    public async Task<bool> EndGameSessionAsync(int id)
    {
        return await _gameSessionRepository.EndGameSessionAsync(id, DateTime.Now);
    }

    public async Task<IEnumerable<GameQuestionDTO>> GetGameQuestionsAsync(int gameSessionId)
    {
        var gameQuestions = await _gameQuestionRepository.GetByGameSessionIdAsync(gameSessionId);
        return gameQuestions.Select(MapToDTO);
    }

    public async Task<bool> AddQuestionsToGameSessionAsync(int gameSessionId, IEnumerable<int> questionIds, int timeLimit)
    {
        var gameQuestions = new List<GameQuestion>();
        int order = 0;
        
        foreach (var questionId in questionIds)
        {
            gameQuestions.Add(new GameQuestion
            {
                GameSessionId = gameSessionId,
                QuestionId = questionId,
                QuestionOrder = order++,
                TimeLimit = timeLimit
            });
        }

        return await _gameQuestionRepository.CreateManyAsync(gameQuestions);
    }

    private GameSessionDTO MapToDTO(GameSession gameSession)
    {
        return new GameSessionDTO
        {
            Id = gameSession.Id,
            RoomId = gameSession.RoomId,
            GameState = gameSession.GameState,
            CurrentQuestionIndex = gameSession.CurrentQuestionIndex,
            StartTime = gameSession.StartTime,
            EndTime = gameSession.EndTime,
            TimeLimit = gameSession.TimeLimit,
            CreatedAt = gameSession.CreatedAt,
            UpdatedAt = gameSession.UpdatedAt
        };
    }

    private GameQuestionDTO MapToDTO(GameQuestion gameQuestion)
    {
        return new GameQuestionDTO
        {
            GameSessionId = gameQuestion.GameSessionId,
            QuestionId = gameQuestion.QuestionId,
            QuestionOrder = gameQuestion.QuestionOrder,
            TimeLimit = gameQuestion.TimeLimit,
            Question = gameQuestion.Question != null ? new QuestionDTO(
                gameQuestion.Question.Id,
                gameQuestion.Question.QuestionText,
                new List<AnswerDTO>(),
                gameQuestion.Question.TopicId ?? 0,
                gameQuestion.Question.QuestionTypeId ?? 0,
                gameQuestion.TimeLimit,
                100) : null
        };
    }
}