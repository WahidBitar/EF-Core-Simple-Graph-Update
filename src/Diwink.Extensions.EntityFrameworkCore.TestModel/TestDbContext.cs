using Diwink.Extensions.EntityFrameworkCore.TestModel.Entities;
using Microsoft.EntityFrameworkCore;

namespace Diwink.Extensions.EntityFrameworkCore.TestModel;

public class TestDbContext : DbContext
{
    public DbSet<LearningCatalog> LearningCatalogs => Set<LearningCatalog>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<TopicTag> TopicTags => Set<TopicTag>();
    public DbSet<Mentor> Mentors => Set<Mentor>();
    public DbSet<CourseMentorAssignment> CourseMentorAssignments => Set<CourseMentorAssignment>();
    public DbSet<CoursePolicy> CoursePolicies => Set<CoursePolicy>();
    public DbSet<MentorWorkspace> MentorWorkspaces => Set<MentorWorkspace>();

    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TestDbContext).Assembly);
    }
}
