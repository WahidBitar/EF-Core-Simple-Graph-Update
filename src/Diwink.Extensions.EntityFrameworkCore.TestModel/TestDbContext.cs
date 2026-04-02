using Diwink.Extensions.EntityFrameworkCore.V2.TestModel.Entities;
using Microsoft.EntityFrameworkCore;

namespace Diwink.Extensions.EntityFrameworkCore.V2.TestModel;

public class V2TestDbContext : DbContext
{
    public DbSet<LearningCatalog> LearningCatalogs => Set<LearningCatalog>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<TopicTag> TopicTags => Set<TopicTag>();
    public DbSet<Mentor> Mentors => Set<Mentor>();
    public DbSet<CourseMentorAssignment> CourseMentorAssignments => Set<CourseMentorAssignment>();
    public DbSet<CoursePolicy> CoursePolicies => Set<CoursePolicy>();
    public DbSet<MentorWorkspace> MentorWorkspaces => Set<MentorWorkspace>();

    public V2TestDbContext(DbContextOptions<V2TestDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(V2TestDbContext).Assembly);
    }
}
