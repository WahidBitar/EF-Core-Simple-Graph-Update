namespace Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;

public class LearningCatalog
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // One-to-many (unsupported in v2 — used for FR-018/FR-019 testing)
    public ICollection<Course> Courses { get; set; } = [];

    // Pure many-to-many (skip navigation)
    public ICollection<TopicTag> Tags { get; set; } = [];
}
