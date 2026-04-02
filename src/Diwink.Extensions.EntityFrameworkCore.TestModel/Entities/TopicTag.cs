namespace Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;

public class TopicTag
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;

    // Pure many-to-many (skip navigation)
    public ICollection<Course> Courses { get; set; } = [];
}
