namespace NicaRunner.Domain.Entities;

public class RaceJudge
{
    public int Id { get; set; }
    public int RaceId { get; set; }
    public int UserId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public Race Race { get; set; } = null!;
    public User User { get; set; } = null!;
}
