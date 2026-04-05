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

    /// <summary>
    /// Creates a TestDbContext configured with the specified EF Core options.
    /// </summary>
    /// <param name="options">The EF Core <see cref="DbContextOptions{TestDbContext}"/> used to configure the context (provider, connection string, and other behaviors).</param>
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Applies all entity type configurations defined in the TestDbContext assembly to the provided model.
    /// </summary>
    /// <param name="modelBuilder">The <see cref="ModelBuilder"/> used to configure the EF Core model.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TestDbContext).Assembly);
    }
}
