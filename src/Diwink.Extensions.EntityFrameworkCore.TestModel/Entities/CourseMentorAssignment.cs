namespace Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;

public class CourseMentorAssignment
{
    public Guid CourseId { get; set; }
    public Guid MentorId { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime AssignedOnUtc { get; set; }
    public decimal AllocationPercent { get; set; }

    public Course Course { get; set; } = null!;
    public Mentor Mentor { get; set; } = null!;
}
