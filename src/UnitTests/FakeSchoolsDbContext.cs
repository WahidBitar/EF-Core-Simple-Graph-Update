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
        public DbSet<SchoolHouse> SchoolHouses { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<Degree> Degrees { get; set; }
        public DbSet<ClassLaboratory> ClassLaboratories { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Student> Students { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<School>(configureSchool);
            builder.Entity<SchoolHouse>(configureSchoolHouses);
            builder.Entity<Class>(configureClass);
            builder.Entity<ClassLaboratory>(configureClassLaboratory);
            builder.Entity<Teacher>(configureTeacher);
            builder.Entity<ClassTeacher>(configureClassTeacher);
            builder.Entity<Student>(configureStudent);
            builder.Entity<Degree>(configureDegree);
        }


        private void configureSchool(EntityTypeBuilder<School> builder)
        {
            builder.Property(x => x.Address).HasConversion(
                    v => string.Join(";", v.Select(r => r)),
                    y => y.Split(';', StringSplitOptions.RemoveEmptyEntries))
                .HasMaxLength(200);

            builder.HasOne(x => x.House)
                .WithOne(x => x.School)
                .HasForeignKey<SchoolHouse>(x => x.SchoolId)
                .OnDelete(DeleteBehavior.Cascade);
        }


        private void configureSchoolHouses(EntityTypeBuilder<SchoolHouse> builder)
        {
            builder.HasKey(x => x.SchoolId);
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


        private void configureStudent(EntityTypeBuilder<Student> builder)
        {
        }


        private void configureDegree(EntityTypeBuilder<Degree> builder)
        {
            builder.HasMany(x => x.Students)
                .WithOne(x => x.Degree)
                .HasForeignKey(x => x.DegreeId)
                .IsRequired(false);
        }
    }
}