namespace ConsoleApp1.Model.DTO.Rooms;

/// <summary>
/// Request model để submit user answer
/// </summary>
public class SubmitUserAnswerRequest
{
    public int UserId { get; set; }
    public int SessionId { get; set; }
    public int QuestionId { get; set; }
    public int AnswerId { get; set; }
    public double TimeToAnswer { get; set; }
}

/// <summary>
/// Request model để update answer score
/// </summary>
public class UpdateAnswerScoreRequest
{
    public int NewScore { get; set; }
    public string? Reason { get; set; }
}