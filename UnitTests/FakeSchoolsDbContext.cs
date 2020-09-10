using System;
using FakeModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Linq;


namespace UnitTests
{
    public class FakeSchoolsDbContext : DbContext
    {
        public FakeSchoolsDbContext(DbContextOptions<FakeSchoolsDbContext> options) : base(options)
        {
            
        }


        public DbSet<School> Schools { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<ClassLaboratory> ClassLaboratories { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Student> Students { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<School>(configureSchool);
            builder.Entity<Class>(configureClass);
            builder.Entity<ClassLaboratory>(configureClassLaboratory);
            builder.Entity<Teacher>(configureTeacher);
            builder.Entity<ClassTeacher>(configureClassTeacher);
            builder.Entity<Student>(configureConfigureStudent);
        }


        private void configureSchool(EntityTypeBuilder<School> builder)
        {
            builder.Property(x => x.Address).HasConversion(
                    v => string.Join(";", v.Select(r => r)),
                    y => y.Split(';', StringSplitOptions.RemoveEmptyEntries))
                .HasMaxLength(200);
        }


        private void configureClass(EntityTypeBuilder<Class> builder)
        {
            builder.HasOne(x => x.Laboratory)
                .WithOne(x => x.Class)
                .HasForeignKey<ClassLaboratory>(x => x.ClassId)
                .OnDelete(DeleteBehavior.Cascade);
        }


        private void configureClassLaboratory(EntityTypeBuilder<ClassLaboratory> builder)
        {
            builder.HasKey(x => x.ClassId);
        }


        private void configureTeacher(EntityTypeBuilder<Teacher> builder)
        {
        }


        private void configureClassTeacher(EntityTypeBuilder<ClassTeacher> builder)
        {
            builder.HasKey(x => new {x.ClassId, x.TeacherId});
        }


        private void configureConfigureStudent(EntityTypeBuilder<Student> builder)
        {
        }


        public override void Dispose()
        {
            base.Dispose();
        }
    }
}