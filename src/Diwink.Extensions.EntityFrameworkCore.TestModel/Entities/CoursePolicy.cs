namespace Diwink.Extensions.EntityFrameworkCore.V2.TestModel.Entities;

public class CoursePolicy
{
    public Guid CourseId { get; set; }
    public string PolicyVersion { get; set; } = string.Empty;
    public bool IsMandatory { get; set; }

    public Course Course { get; set; } = null!;
}
