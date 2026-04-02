namespace Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;

public class Course
{
    public Guid Id { get; set; }
    public Guid CatalogId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public LearningCatalog Catalog { get; set; } = null!;

    // Required one-to-one
    public CoursePolicy? Policy { get; set; }

    // Many-to-many with payload
    public ICollection<CourseMentorAssignment> MentorAssignments { get; set; } = [];

    // Pure many-to-many (skip navigation)
    public ICollection<TopicTag> Tags { get; set; } = [];
}
