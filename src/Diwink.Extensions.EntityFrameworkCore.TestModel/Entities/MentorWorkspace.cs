namespace Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;

public class MentorWorkspace
{
    public Guid Id { get; set; }
    public Guid? MentorId { get; set; }
    public string DeskCode { get; set; } = string.Empty;
    public string Building { get; set; } = string.Empty;

    public Mentor? Mentor { get; set; }
}
