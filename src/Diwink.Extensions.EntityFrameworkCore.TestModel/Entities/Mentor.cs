namespace Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;

public class Mentor
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    // Many-to-many with payload
    public ICollection<CourseMentorAssignment> CourseAssignments { get; set; } = [];

    // Optional one-to-one
    public MentorWorkspace? Workspace { get; set; }
}
