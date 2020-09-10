using System;
using System.Collections.Generic;


namespace FakeModel
{
    public class School
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public SchoolType Type { get; set; }
        public ICollection<string> Address { get; set; } = new HashSet<string>();
        public ICollection<Class> Classes { get; set; } = new HashSet<Class>();
    }



    public class Class
    {
        public int Id { get; set; }
        public int SchoolId { get; set; }
        public int Level { get; set; }
        public int Capacity { get; set; }
        public School School { get; set; }
        public ClassLaboratory Laboratory { get; set; }
        public ICollection<ClassTeacher> ClassTeachers { get; set; } = new HashSet<ClassTeacher>();
        public ICollection<Student> Students { get; set; } = new HashSet<Student>();
    }



    public class ClassLaboratory
    {
        public int ClassId { get; set; }
        public string Name { get; set; }
        public Class Class { get; set; }
    }



    public class Student
    {
        public Guid Id { get; set; }
        public int ClassId { get; set; }
        public string Name { get; set; }
        public DateTimeOffset DateOfBirth { get; set; }
        public Class Class { get; set; }
    }



    public class Teacher
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTimeOffset? DateOfBirth { get; set; }
        public ICollection<ClassTeacher> ClassTeachers { get; set; } = new HashSet<ClassTeacher>();
    }



    public class ClassTeacher
    {
        public int ClassId { get; set; }
        public Guid TeacherId { get; set; }
        public Class Class { get; set; }
        public Teacher Teacher { get; set; }
    }



    public enum SchoolType
    {
        Elementary = 1,
        Secondary = 2,
        HighSchool = 3
    }
}
