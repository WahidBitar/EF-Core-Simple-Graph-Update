using System;
using System.Collections.Generic;
using System.Linq;
using Diwink.Extensions.EntityFrameworkCore;
using FakeModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NUnit.Framework;


namespace UnitTests
{
    [TestFixture]
    public class GraphUpdateTests
    {
        private FakeSchoolsDbContext dbContext;
        private readonly ServiceProvider serviceProvider;
        private IServiceScope scope;
        private bool inMemoryDb;
        private School school;
        private List<Teacher> teachers;


        public GraphUpdateTests()
        {
            var configuration = TestHelpers.InitConfiguration();
            var services = new ServiceCollection();

            services.AddDbContext<FakeSchoolsDbContext>(options =>
                {
                    options.EnableDetailedErrors().EnableSensitiveDataLogging();
                    inMemoryDb = bool.Parse(configuration["InMemoryDB"]);
                    if (inMemoryDb)
                        options.UseInMemoryDatabase("FakeSchoolsDb");
                    else
                        options.UseSqlServer(configuration.GetConnectionString("FakeSchoolsDb"));
                }
            );

            serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Setup operation will create a new DbContext for each test method
        /// </summary>
        [SetUp]
        public void Setup()
        {
            teachers = new List<Teacher>()
            {
                new Teacher
                {
                    Name = "Ahmad",
                    Id = Guid.Parse("{EC13122E-3EC5-4698-B254-E660D01F37CA}"),
                },
                new Teacher
                {
                    Name = "Mohammad",
                    Id = Guid.Parse("{7AB15219-5FFA-406C-B092-94636B413E05}"),
                    DateOfBirth = new DateTimeOffset(1980, 9, 1, 03, 0, 0, 0, TimeSpan.Zero),
                }
            };
            var students = new List<Student>
            {
                new Student
                {
                    Name = "Saaid",
                    Id = Guid.Parse("{EF592B57-5691-415A-974E-D281B368545F}"),
                    DateOfBirth = DateTimeOffset.Now.AddYears(-6).Date
                },
                new Student
                {
                    Name = "Samir",
                    Id = Guid.Parse("{836FE019-6CEC-4F54-A39F-74448D6D86DC}"),
                    DateOfBirth = DateTimeOffset.Now.AddYears(-6).Date
                },
            };
            var classes = new List<Class>
            {
                new Class
                {
                    Capacity = 20,
                    Level = 1,
                    Students = students,
                },
                new Class
                {
                    Capacity = 10,
                    Level = 2,
                    Students = new List<Student>
                    {
                        new Student
                        {
                            Name = "Azoz",
                            Id = Guid.Parse("{35CE5E1C-AB25-448F-8CD9-E31DD3821DAD}"),
                            DateOfBirth = DateTimeOffset.Now.AddYears(-7).Date
                        },
                        new Student
                        {
                            Name = "Bakri",
                            Id = Guid.Parse("{587FF49B-0306-448E-80FD-071C46F0B488}"),
                            DateOfBirth = DateTimeOffset.Now.AddYears(-7).Date
                        },
                    }
                },
            };

            school = new School()
            {
                Name = "The First",
                Address = new List<string> {"Akdeniz", "Mersin"},
                Type = SchoolType.Elementary,
                Classes = classes,
            };

            scope = serviceProvider.CreateScope();
            dbContext = scope.ServiceProvider.GetService<FakeSchoolsDbContext>();
        }


        /// <summary>
        /// Disposing the scope and DbContext
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            scope.Dispose();
        }


        /// <summary>
        /// Clean up everything
        /// </summary>
        [Test]
        public void S000_Delete_Database()
        {
            dbContext.Database.EnsureDeleted();
        }


        /// <summary>
        /// Apply DB migrations if the test happens on real database
        /// </summary>
        [Test]
        public void S001_Apply_Migrations()
        {
            if (!inMemoryDb)
                dbContext.Database.Migrate();
        }


        /// <summary>
        /// Test insert new simple entities
        /// </summary>
        [Test]
        public void S002_Seed_Teachers()
        {
            foreach (var teacher in teachers)
            {
                dbContext.InsertUpdateOrDeleteGraph(teacher, null);
            }

            dbContext.SaveChanges();

            Assert.AreEqual(teachers.Count, dbContext.Teachers.Count());
        }


        /// <summary>
        /// Test insert more complex Entity
        /// </summary>
        [Test]
        public void S003_Add_The_School()
        {
            var dbSchool = dbContext.Schools.FirstOrDefault();
            dbContext.InsertUpdateOrDeleteGraph(school, dbSchool);
            dbContext.SaveChanges();

            var updatedDbSchool = dbContext.Schools
                .Include(s => s.Classes)
                .ThenInclude(x => x.Students)
                .FirstOrDefault();

            Assert.IsNotNull(updatedDbSchool);
            Assert.AreEqual(2, updatedDbSchool.Classes.First(c => c.Id == 1).Students.Count);
            Assert.AreEqual(2, updatedDbSchool.Classes.First(c => c.Id == 2).Students.Count);
        }


        /// <summary>
        /// Update an Aggregate by inserting one-to-one navigation property
        /// </summary>
        [Test]
        public void S004_Add_House_To_The_School_Should_Update_House_Navigation_Property()
        {
            var updatedSchool = JsonConvert.DeserializeObject<School>(@"
            {
				'Id': 1,
				'Name': 'The First',
				'Type': 1,
				'Address': [
				'Akdeniz',
				'Mersin'
					],
				'House':{
					'SchoolId': 1,
					'Capacity': 100
				}
            }");

            var dbSchool = dbContext.Schools
                .Include(s => s.House)
                .FirstOrDefault();

            dbContext.InsertUpdateOrDeleteGraph(updatedSchool, dbSchool);

            dbContext.SaveChanges();

            var updatedDbSchool = dbContext.Schools
                .Include(s => s.House)
                .FirstOrDefault();

            Console.WriteLine(JsonConvert.SerializeObject(updatedDbSchool, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented,
            }));

            Assert.NotNull(updatedDbSchool.House);
        }


        /// <summary>
        /// Update one-to-one navigation property in an Aggregate
        /// </summary>
        [Test]
        public void S005_Update_The_House_Of_The_School()
        {
            var updatedSchool = JsonConvert.DeserializeObject<School>(@"
            {
				'Id': 1,
				'Name': 'The First',
				'Type': 1,
				'Address': [
				'Akdeniz',
				'Mersin'
					],
				'House':{
					'SchoolId': 1,
					'Capacity': 150
				}
            }");

            var dbSchool = dbContext.Schools
                .Include(s => s.House)
                .FirstOrDefault();

            dbContext.InsertUpdateOrDeleteGraph(updatedSchool, dbSchool);

            dbContext.SaveChanges();

            var updatedDbSchool = dbContext.Schools
                .Include(s => s.House)
                .FirstOrDefault();

            Console.WriteLine(JsonConvert.SerializeObject(updatedDbSchool, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented,
            }));

            Assert.NotNull(updatedDbSchool.House);
            Assert.AreEqual(150, updatedDbSchool.House.Capacity);
        }


        /// <summary>
        /// Update an Aggregate by deleting one-to-one navigation property
        /// </summary>
        [Test]
        public void S006_Remove_The_House_From_The_School_Should_Make_The_House_Navigation_Null()
        {
            var updatedSchool = JsonConvert.DeserializeObject<School>(@"
            {
				'Id': 1,
				'Name': 'The First',
				'Type': 1,
				'Address': [
				'Akdeniz',
				'Mersin'
					]
            }");

            var dbSchool = dbContext.Schools
                .Include(s => s.House)
                .FirstOrDefault();

            dbContext.InsertUpdateOrDeleteGraph(updatedSchool, dbSchool);

            dbContext.SaveChanges();

            var updatedDbSchool = dbContext.Schools
                .Include(s => s.House)
                .FirstOrDefault();

            Console.WriteLine(JsonConvert.SerializeObject(updatedDbSchool, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented,
            }));

            Assert.Null(updatedDbSchool.House);
        }
        /// <summary>
        /// Update an Aggregate by deleting one-to-one navigation property
        /// </summary>
        [Test]
        public void S0061_Set_Classes_From_The_School_Null_Should_Make_The_Classes_Empty()
        {
            dbContext.InsertUpdateOrDeleteGraph(school, null);//Initialise school to have dbSchool not null
            dbContext.SaveChanges();
            var dbSchool = dbContext.Schools
                .Include(s => s.Classes)
                .FirstOrDefault();

            Assert.IsTrue(dbSchool.Classes.Count>0);

            var updatedSchool = JsonConvert.DeserializeObject<School>(@"
            {
				'Id': 1,
				'Name': 'The First',
				'Type': 1,
            }");
            updatedSchool.Classes=null;

            // Act
            dbContext.InsertUpdateOrDeleteGraph(updatedSchool, dbSchool);
            dbContext.SaveChanges();

            var updatedDbSchool = dbContext.Schools
                .Include(s => s.Classes)
                .FirstOrDefault();

            Console.WriteLine(JsonConvert.SerializeObject(updatedDbSchool, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented,
            }));

            Assert.IsTrue(updatedDbSchool.Classes.Count==0);
        }

        /// <summary>
        /// Update an inner Entity in the Aggregate should not affect the other sub Entities as they're not included in the changes
        /// </summary>
        [Test]
        public void S007_Update_Classes_Info_Should_Not_Change_Teachers()
        {
            var updatedSchool = JsonConvert.DeserializeObject<School>(@"{
            'Id': 1,
            'Name': 'The First',
            'Type': 1,
            'Address': [
            'Akdeniz',
            'Mersin'
                ],
            'Classes': [
            {
                'Id': 1,
                'SchoolId': 1,
                'Level': 3,
                'Capacity': 20,
                'ClassTeachers': [
                {
                    'ClassId': 1,
                    'TeacherId': 'ec13122e-3ec5-4698-b254-e660d01f37ca'
                },
                {
                    'ClassId': 1,
                    'TeacherId': '7ab15219-5ffa-406c-b092-94636b413e05'
                }
                ],
                'Students': [
                {
                    'Id': 'ef592b57-5691-415a-974e-d281b368545f',
                    'ClassId': 1,
                    'Name': 'Saaid',
                    'DateOfBirth': '2014-09-10T00:00:00+03:00'
                },
                {
                    'Id': '836fe019-6cec-4f54-a39f-74448d6d86dc',
                    'ClassId': 1,
                    'Name': 'Samir',
                    'DateOfBirth': '2014-09-10T00:00:00+03:00'
                }
                ]
            },
            {
                'Id': 2,
                'SchoolId': 1,
                'Level': 4,
                'Capacity': 10,
                'ClassTeachers': [
                ],
                'Students': [
                {
                    'Id': '35ce5e1c-ab25-448f-8cd9-e31dd3821dad',
                    'ClassId': 2,
                    'Name': 'Azoz',
                    'DateOfBirth': '2013-09-10T00:00:00+03:00',
                    'Class': null
                },
                {
                    'Id': '587ff49b-0306-448e-80fd-071c46f0b488',
                    'ClassId': 2,
                    'Name': 'Bakri',
                    'DateOfBirth': '2013-09-10T00:00:00+03:00'
                }
                ]
            }
            ]
        }");

            var dbSchool = dbContext.Schools
                .Include(s => s.Classes)
                .FirstOrDefault();

            Assert.True(dbSchool.Classes.All(c => c.Level <= 2), "Classes Levels before update should be less than 3");

            dbContext.InsertUpdateOrDeleteGraph(updatedSchool, dbSchool);

            dbContext.SaveChanges();

            var updatedDbSchool = dbContext.Schools
                .Include(s => s.Classes)
                .ThenInclude(x => x.ClassTeachers)
                .ThenInclude(x => x.Teacher)
                .Include(s => s.Classes)
                .ThenInclude(x => x.Students)
                .FirstOrDefault();

            Console.WriteLine(JsonConvert.SerializeObject(updatedDbSchool, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented,
            }));

            Assert.True(updatedDbSchool.Classes.All(c => c.Level > 2), "One or more Classes hasn't been updated");
            Assert.True(updatedDbSchool.Classes.All(c => !c.ClassTeachers.Any()), "The teachers has been updated for one or more Classes");
        }


        /// <summary>
        /// Update Many-to-Many relation in an Aggregate by adding relation to two existing entities
        /// </summary>
        [Test]
        public void S008_Update_Classes_Info_And_Add_Teachers_To_One_Of_Them()
        {
            var updatedSchool = JsonConvert.DeserializeObject<School>(@"{
            'Id': 1,
            'Name': 'The First',
            'Type': 1,
            'Address': [
            'Akdeniz',
            'Mersin'
                ],
            'Classes': [
            {
                'Id': 1,
                'SchoolId': 1,
                'Level': 1,
                'Capacity': 20,
                'ClassTeachers': [
                {
                    'ClassId': 1,
                    'TeacherId': 'ec13122e-3ec5-4698-b254-e660d01f37ca'
                },
                {
                    'ClassId': 1,
                    'TeacherId': '7ab15219-5ffa-406c-b092-94636b413e05'
                }
                ],
                'Students': [
                {
                    'Id': 'ef592b57-5691-415a-974e-d281b368545f',
                    'ClassId': 1,
                    'Name': 'Saaid',
                    'DateOfBirth': '2014-09-10T00:00:00+03:00'
                },
                {
                    'Id': '836fe019-6cec-4f54-a39f-74448d6d86dc',
                    'ClassId': 1,
                    'Name': 'Samir',
                    'DateOfBirth': '2014-09-10T00:00:00+03:00'
                }
                ]
            },
            {
                'Id': 2,
                'SchoolId': 1,
                'Level': 2,
                'Capacity': 10,
                'ClassTeachers': [
                ],
                'Students': [
                {
                    'Id': '35ce5e1c-ab25-448f-8cd9-e31dd3821dad',
                    'ClassId': 2,
                    'Name': 'Azoz',
                    'DateOfBirth': '2013-09-10T00:00:00+03:00',
                    'Class': null
                },
                {
                    'Id': '587ff49b-0306-448e-80fd-071c46f0b488',
                    'ClassId': 2,
                    'Name': 'Bakri',
                    'DateOfBirth': '2013-09-10T00:00:00+03:00'
                }
                ]
            }
            ]
        }");

            var dbSchool = dbContext.Schools
                .Include(s => s.Classes)
                .ThenInclude(s => s.ClassTeachers)
                .FirstOrDefault();

            Assert.True(dbSchool.Classes.All(c => c.Level > 2), "Classes Levels before update should be more than 2");

            dbContext.InsertUpdateOrDeleteGraph(updatedSchool, dbSchool);

            dbContext.SaveChanges();

            var updatedDbSchool = dbContext.Schools
                .Include(s => s.Classes)
                .ThenInclude(x => x.ClassTeachers)
                .ThenInclude(x => x.Teacher)
                .Include(s => s.Classes)
                .ThenInclude(x => x.Students)
                .FirstOrDefault();

            Console.WriteLine(JsonConvert.SerializeObject(updatedDbSchool, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented,
            }));

            Assert.True(updatedDbSchool.Classes.All(c => c.Level <= 2), "One or more Classes hasn't been updated");
            Assert.AreEqual(2, updatedDbSchool.Classes.SelectMany(c => c.ClassTeachers).Count());
        }


        /// <summary>
        /// Update Many-to-Many relation in an Aggregate by removing the relation from one existing entity
        /// </summary>
        [Test]
        public void S009_Update_First_Class_Teachers()
        {
            var updatedSchool = JsonConvert.DeserializeObject<School>(@"
                {
                  'Id': 1,
                  'Name': 'The First',
                  'Type': 1,
                  'Address': [
                    'Akdeniz',
                    'Mersin'
                  ],
                  'Classes': [
                    {
                      'Id': 1,
                      'SchoolId': 1,
                      'Level': 1,
                      'Capacity': 20,
                      'ClassTeachers': [
                        {
                          'ClassId': 1,
                          'TeacherId': 'ec13122e-3ec5-4698-b254-e660d01f37ca'
                        }
                      ],
                      'Students': [
                        {
                          'Id': '836fe019-6cec-4f54-a39f-74448d6d86dc',
                          'ClassId': 1,
                          'Name': 'Samir',
                          'DateOfBirth': '2014-09-10T00:00:00+03:00'
                        },
                        {
                          'Id': 'ef592b57-5691-415a-974e-d281b368545f',
                          'ClassId': 1,
                          'Name': 'Saaid',
                          'DateOfBirth': '2014-09-10T00:00:00+03:00'
                        }
                      ]
                    },
                    {
                      'Id': 2,
                      'SchoolId': 1,
                      'Level': 2,
                      'Capacity': 10,
                      'ClassTeachers': [],
                      'Students': [
                        {
                          'Id': '587ff49b-0306-448e-80fd-071c46f0b488',
                          'ClassId': 2,
                          'Name': 'Bakri',
                          'DateOfBirth': '2013-09-10T00:00:00+03:00'
                        },
                        {
                          'Id': '35ce5e1c-ab25-448f-8cd9-e31dd3821dad',
                          'ClassId': 2,
                          'Name': 'Azoz',
                          'DateOfBirth': '2013-09-10T00:00:00+03:00'
                        }
                      ]
                    }
                  ]
                }
");

            var dbSchool = dbContext.Schools
                .Include(s => s.Classes)
                .ThenInclude(s => s.ClassTeachers)
                .FirstOrDefault();

            dbContext.InsertUpdateOrDeleteGraph(updatedSchool, dbSchool);

            dbContext.SaveChanges();

            var updatedDbSchool = dbContext.Schools
                .Include(s => s.Classes)
                .ThenInclude(x => x.ClassTeachers)
                .ThenInclude(x => x.Teacher)
                .Include(s => s.Classes)
                .ThenInclude(x => x.Students)
                .FirstOrDefault();

            Console.WriteLine(JsonConvert.SerializeObject(updatedDbSchool, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented,
            }));

            Assert.AreEqual(1, updatedDbSchool.Classes.SelectMany(c => c.ClassTeachers).Count());
        }


        /// <summary>
        /// Update One-to-Many relation in an Aggregate
        /// </summary>
        [Test]
        public void S010_Update_Classes_Students()
        {
            var updatedSchool = JsonConvert.DeserializeObject<School>(@"
                    {
                      'Id': 1,
                      'Name': 'The First',
                      'Type': 1,
                      'Address': [
                        'Akdeniz',
                        'Mersin'
                      ],
                      'Classes': [
                        {
                          'Id': 1,
                          'SchoolId': 1,
                          'Level': 1,
                          'Capacity': 20,
                          'Students': [
                            {
                              'Id': '836fe019-6cec-4f54-a39f-74448d6d86dc',
                              'ClassId': 1,
                              'Name': 'Samir Matin',
                              'DateOfBirth': '2014-09-10T00:00:00+03:00'
                            }
                          ]
                        },
                        {
                          'Id': 2,
                          'SchoolId': 1,
                          'Level': 2,
                          'Capacity': 10,
                          'Students': [
                            {
                              'Id': '587ff49b-0306-448e-80fd-071c46f0b488',
                              'ClassId': 2,
                              'Name': 'Bakri',
                              'DateOfBirth': '2013-09-10T00:00:00+03:00'
                            },	
                            {
                              'Id': 'ef592b57-5691-415a-974e-d281b368545f',
                              'ClassId': 2,
                              'Name': 'Saaid',
                              'DateOfBirth': '2013-09-10T00:00:00+03:00'
                            }
                          ]
                        }
                      ]
                    }
");

            var dbSchool = dbContext.Schools
                .Include(s => s.Classes)
                .ThenInclude(s => s.Students)
                .FirstOrDefault();

            dbContext.InsertUpdateOrDeleteGraph(updatedSchool, dbSchool);

            dbContext.SaveChanges();

            var updatedDbSchool = dbContext.Schools
                .Include(s => s.Classes)
                .ThenInclude(x => x.ClassTeachers)
                .ThenInclude(x => x.Teacher)
                .Include(s => s.Classes)
                .ThenInclude(x => x.Students)
                .FirstOrDefault();

            Console.WriteLine(JsonConvert.SerializeObject(updatedDbSchool, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented,
            }));

            Assert.AreEqual(1, updatedDbSchool.Classes.First(c => c.Id == 1).Students.Count);
            Assert.AreEqual(2, updatedDbSchool.Classes.First(c => c.Id == 2).Students.Count);
        }


        /// <summary>
        /// Insert new simple Aggregate
        /// </summary>
        [Test]
        public void S011_Add_Degrees()
        {
            var highschool = new Degree()
            {
                Name = "High-School"
            };

            dbContext.InsertUpdateOrDeleteGraph(highschool, null);

            dbContext.SaveChanges();

            var updatedDbDegree = dbContext.Degrees
                .Include(s => s.Students)
                .FirstOrDefault();

            Console.WriteLine(JsonConvert.SerializeObject(updatedDbDegree, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented,
            }));

            Assert.IsNotNull(updatedDbDegree);
        }


        /// <summary>
        /// Update Aggregate with one-to-many relation to an Entity in another Aggregate
        /// </summary>
        [Test]
        public void S012_Add_Degrees_To_Students_Should_Update_The_Degree_Property_In_Students()
        {
            var updatedSchool = JsonConvert.DeserializeObject<School>(@"
                    {
                      'Id': 1,
                      'Name': 'The First',
                      'Type': 1,
                      'Address': [
                        'Akdeniz',
                        'Mersin'
                      ],
                      'Classes': [
                        {
                          'Id': 1,
                          'SchoolId': 1,
                          'Level': 1,
                          'Capacity': 20,
                          'Students': [
                            {
                              'Id': '836fe019-6cec-4f54-a39f-74448d6d86dc',
                              'ClassId': 1,
                              'DegreeId': 1,
                              'Name': 'Samir Matin',
                              'DateOfBirth': '2014-09-10T00:00:00+03:00'
                            }
                          ]
                        },
                        {
                          'Id': 2,
                          'SchoolId': 1,
                          'Level': 2,
                          'Capacity': 10,
                          'Students': [
                            {
                              'Id': '587ff49b-0306-448e-80fd-071c46f0b488',
                              'ClassId': 2,
                              'DegreeId': 1,
                              'Name': 'Bakri',
                              'DateOfBirth': '2013-09-10T00:00:00+03:00'
                            },	
                            {
                              'Id': 'ef592b57-5691-415a-974e-d281b368545f',
                              'ClassId': 2,
                              'DegreeId': 1,
                              'Name': 'Saaid',
                              'DateOfBirth': '2013-09-10T00:00:00+03:00'
                            }
                          ]
                        }
                      ]
                    }
");

            var dbSchool = dbContext.Schools
                .Include(s => s.Classes)
                .ThenInclude(s => s.Students)
                .FirstOrDefault();

            dbContext.InsertUpdateOrDeleteGraph(updatedSchool, dbSchool);

            dbContext.SaveChanges();

            var updatedDbSchool = dbContext.Schools
                .Include(s => s.Classes)
                .ThenInclude(x => x.Students)
                .ThenInclude(s => s.Degree)
                .FirstOrDefault();

            var updatedDbDegree = dbContext.Degrees
                .Include(s => s.Students)
                .FirstOrDefault();

            Console.WriteLine(JsonConvert.SerializeObject(updatedDbSchool, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented,
            }));
            Console.WriteLine("---------------------------------------");
            Console.WriteLine(JsonConvert.SerializeObject(updatedDbDegree, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented,
            }));

            Assert.AreEqual(1, updatedDbSchool.Classes.First(c => c.Id == 1).Students.Count);
            Assert.AreEqual(2, updatedDbSchool.Classes.First(c => c.Id == 2).Students.Count);
            Assert.AreEqual(3, updatedDbDegree.Students.Count);
            Assert.NotNull(updatedDbSchool.Classes.First(c => c.Id == 1).Students.First(s => s.Id == Guid.Parse("836fe019-6cec-4f54-a39f-74448d6d86dc")).Degree);
            Assert.NotNull(updatedDbSchool.Classes.First(c => c.Id == 2).Students.First(s => s.Id == Guid.Parse("587ff49b-0306-448e-80fd-071c46f0b488")).Degree);
            Assert.NotNull(updatedDbSchool.Classes.First(c => c.Id == 2).Students.First(s => s.Id == Guid.Parse("ef592b57-5691-415a-974e-d281b368545f")).Degree);
        }


        /// <summary>
        /// Update Entity with Optional one-to-many relation to an Entity in another Aggregate
        /// </summary>
        [Test]
        public void S013_Remove_Student_From_Degree_Should_NOT_Delete_The_Student_Entity()
        {
            var updatedDegree = JsonConvert.DeserializeObject<Degree>(@"
                                    {
                                      'Id': 1,
                                      'Name': 'High-School',
                                      'Students': [
                                        {
                                          'Id': '836fe019-6cec-4f54-a39f-74448d6d86dc',
                                          'DegreeId': 1,
                                          'ClassId': 1,
                                          'Name': 'Samir Matin',
                                          'DateOfBirth': '2014-09-10T00:00:00+03:00'
                                        },
                                        {
                                          'Id': '587ff49b-0306-448e-80fd-071c46f0b488',
                                          'DegreeId': 1,
                                          'ClassId': 2,
                                          'Name': 'Bakri',
                                          'DateOfBirth': '2013-09-10T00:00:00+03:00'
                                        }
                                      ]
                                    }");

            var dbDegree = dbContext.Degrees
                .Include(x => x.Students)
                .FirstOrDefault();

            dbContext.InsertUpdateOrDeleteGraph(updatedDegree, dbDegree);

            dbContext.SaveChanges();

            var updatedDbDegree = dbContext.Degrees
                .Include(s => s.Students)
                .FirstOrDefault();

            var updatedDbStudent = dbContext.Students
                .Include(s => s.Degree)
                .FirstOrDefault(x => x.Id == Guid.Parse("ef592b57-5691-415a-974e-d281b368545f"));

            Console.WriteLine(JsonConvert.SerializeObject(updatedDbDegree, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented,
            }));
            Console.WriteLine("------------------------------------");
            Console.WriteLine(JsonConvert.SerializeObject(updatedDbStudent, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented,
            }));

            Assert.AreEqual(2, updatedDegree.Students.Count);
            Assert.IsNotNull(updatedDbStudent);
            Assert.IsNull(updatedDbStudent.Degree);
            Assert.IsNull(updatedDbStudent.DegreeId);
        }


        [Test]
        public void S014_Add_Classes_Laboratory()
        {
            var updatedSchool = JsonConvert.DeserializeObject<School>(@"
                    {
                      'Id': 1,
                      'Name': 'The First',
                      'Type': 1,
                      'Address': [
                        'Akdeniz',
                        'Mersin'
                      ],
                      'Classes': [
                        {
                          'Id': 1,
                          'SchoolId': 1,
                          'Level': 1,
                          'Capacity': 20,
                           'Laboratory':{
                                'ClassId':1,
                                'Name': 'First Class Laboratory'
                            }
                        },
                        {
                          'Id': 2,
                          'SchoolId': 1,
                          'Level': 2,
                          'Capacity': 10,
                           'Laboratory':{
                                'ClassId':2,
                                'Name': 'Second Class Laboratory'
                            }
                        }
                      ]
                    }
");

            var dbSchool = dbContext.Schools
                .Include(s => s.Classes)
                .ThenInclude(s => s.Laboratory)
                .FirstOrDefault();

            dbContext.InsertUpdateOrDeleteGraph(updatedSchool, dbSchool);

            dbContext.SaveChanges();

            var updatedDbSchool = dbContext.Schools
                .Include(s => s.Classes)
                .ThenInclude(x => x.ClassTeachers)
                .ThenInclude(x => x.Teacher)
                .Include(s => s.Classes)
                .ThenInclude(x => x.Students)
                .FirstOrDefault();

            Console.WriteLine(JsonConvert.SerializeObject(updatedDbSchool, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented,
            }));

            Assert.True(updatedDbSchool.Classes.First(c => c.Id == 1).Laboratory != null);
            Assert.True(updatedDbSchool.Classes.First(c => c.Id == 2).Laboratory != null);
        }


        [Test]
        public void S015_Delete_First_Class_Laboratory()
        {
            var updatedSchool = JsonConvert.DeserializeObject<School>(@"
                    {
                      'Id': 1,
                      'Name': 'The First',
                      'Type': 1,
                      'Address': [
                        'Akdeniz',
                        'Mersin'
                      ],
                      'Classes': [
                        {
                          'Id': 1,
                          'SchoolId': 1,
                          'Level': 1,
                          'Capacity': 20
                        },
                        {
                          'Id': 2,
                          'SchoolId': 1,
                          'Level': 2,
                          'Capacity': 10,
                           'Laboratory':{
                                'ClassId':2,
                                'Name': 'Second Class Laboratory'
                            }
                        }
                      ]
                    }
");

            var dbSchool = dbContext.Schools
                .Include(s => s.Classes)
                .ThenInclude(s => s.Laboratory)
                .FirstOrDefault();
            var lll = dbContext
                .Entry(updatedSchool)
                .Navigations
                .Select(x => new
                {
                    MetadataName = x.Metadata.Name,
                    MetadataDeclaringEntityType = x.Metadata.DeclaringEntityType,
                    MetadataDeclaringTypeName = x.Metadata.DeclaringType.Name,
                    IsShadowProperty = x.Metadata.IsShadowProperty(),
                    IsDependentToPrincipal = x.Metadata.IsDependentToPrincipal(),
                    Inverse = x.Metadata.FindInverse(),
                    FirstClass = dbContext.Entry(updatedSchool.Classes.FirstOrDefault())
                        .Navigations
                        .Select(y => new
                        {
                            MetadataName = y.Metadata.Name,
                            MetadataDeclaringEntityType = y.Metadata.DeclaringEntityType,
                            MetadataDeclaringTypeName = y.Metadata.DeclaringType.Name,
                            IsShadowProperty = y.Metadata.IsShadowProperty(),
                            IsDependentToPrincipal = y.Metadata.IsDependentToPrincipal(),
                            Inverse = y.Metadata.FindInverse(),
                        }),
                    FirstClassLaboratory = updatedSchool.Classes.FirstOrDefault().Laboratory != null
                        ? dbContext.Entry(updatedSchool.Classes.FirstOrDefault().Laboratory)
                            .Navigations
                            .Select(z => new
                            {
                                MetadataName = z.Metadata.Name,
                                MetadataDeclaringEntityType = z.Metadata.DeclaringEntityType,
                                MetadataDeclaringTypeName = z.Metadata.DeclaringType.Name,
                                IsShadowProperty = z.Metadata.IsShadowProperty(),
                                IsDependentToPrincipal = z.Metadata.IsDependentToPrincipal(),
                                Inverse = z.Metadata.FindInverse(),
                            })
                        : null,
                }).ToList();

            dbContext.InsertUpdateOrDeleteGraph(updatedSchool, dbSchool);

            dbContext.SaveChanges();

            var updatedDbSchool = dbContext.Schools
                .Include(s => s.Classes)
                .ThenInclude(x => x.ClassTeachers)
                .ThenInclude(x => x.Teacher)
                .Include(s => s.Classes)
                .ThenInclude(x => x.Students)
                .FirstOrDefault();

            Console.WriteLine(JsonConvert.SerializeObject(updatedDbSchool, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented,
            }));

            Assert.True(updatedDbSchool.Classes.First(c => c.Id == 1).Laboratory == null);
            Assert.True(updatedDbSchool.Classes.First(c => c.Id == 2).Laboratory != null);
        }


        [Test]
        public void S016_Update_Class_Teachers_Of_Second_Class()
        {
            var updatedSchool = JsonConvert.DeserializeObject<School>(@"
                    {
                      'Id': 1,
                      'Name': 'The First',
                      'Type': 1,
                      'Address': [
                        'Akdeniz',
                        'Mersin'
                      ],
                      'Classes': [
                        {
                          'Id': 1,
                          'SchoolId': 1,
                          'Level': 1,
                          'Capacity': 20,
                          'ClassTeachers': [
                            {
                              'ClassId': 1,
                              'TeacherId': 'ec13122e-3ec5-4698-b254-e660d01f37ca'
                            }
                          ]
                        },
                        {
                          'Id': 2,
                          'SchoolId': 1,
                          'Level': 2,
                          'Capacity': 10,
                          'ClassTeachers': [
                            {
                              'ClassId': 2,
                              'TeacherId': 'ec13122e-3ec5-4698-b254-e660d01f37ca'
                            },
                            {
                              'ClassId': 2,
                              'TeacherId': '7AB15219-5FFA-406C-B092-94636B413E05'
                            }
                          ]
                        }
                      ]
                    }
");

            var dbSchool = dbContext.Schools
                .Include(s => s.Classes)
                .ThenInclude(s => s.ClassTeachers)
                .FirstOrDefault();

            dbContext.InsertUpdateOrDeleteGraph(updatedSchool, dbSchool);

            dbContext.SaveChanges();

            var updatedDbSchool = dbContext.Schools
                .Include(s => s.Classes)
                .ThenInclude(s => s.ClassTeachers)
                .FirstOrDefault();

            Console.WriteLine(JsonConvert.SerializeObject(updatedDbSchool, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented,
            }));

            Assert.AreEqual(1, updatedDbSchool.Classes.First(c => c.Id == 1).ClassTeachers.Count);
            Assert.AreEqual(2, updatedDbSchool.Classes.First(c => c.Id == 2).ClassTeachers.Count);
        }



        [Test]
        public void S017_Add_New_Teacher_To_A_Class()
        {
            var updatedSchool = JsonConvert.DeserializeObject<School>(@"
                    {
                      'Id': 1,
                      'Name': 'The First',
                      'Type': 1,
                      'Address': [
                        'Akdeniz',
                        'Mersin'
                      ],
                      'Classes': [
                        {
                          'Id': 1,
                          'SchoolId': 1,
                          'Level': 1,
                          'Capacity': 20,
                          'ClassTeachers': [
                            {
                              'ClassId': 1,
                              'TeacherId': 'ec13122e-3ec5-4698-b254-e660d01f37ca'
                            },
                            {
                              'ClassId': 1,
                              'TeacherId': 'D814E2DE-D097-40AE-BCCF-C4C5F1415D6B',
                              'Teacher':{
                                    'Id': 'D814E2DE-D097-40AE-BCCF-C4C5F1415D6B',
                                    'Name': 'Hasan'
                               }
                            }
                          ]
                        },
                        {
                          'Id': 2,
                          'SchoolId': 1,
                          'Level': 2,
                          'Capacity': 10,
                          'ClassTeachers': [
                            {
                              'ClassId': 2,
                              'TeacherId': 'ec13122e-3ec5-4698-b254-e660d01f37ca'
                            },
                            {
                              'ClassId': 2,
                              'TeacherId': '7AB15219-5FFA-406C-B092-94636B413E05'
                            }
                          ]
                        }
                      ]
                    }
");

            var dbSchool = dbContext.Schools
                .Include(s => s.Classes)
                .ThenInclude(s => s.ClassTeachers)
                .FirstOrDefault();

            dbContext.InsertUpdateOrDeleteGraph(updatedSchool, dbSchool);

            dbContext.SaveChanges();

            var updatedDbSchool = dbContext.Schools
                .Include(s => s.Classes)
                .ThenInclude(s => s.ClassTeachers)
                .FirstOrDefault();

            Console.WriteLine(JsonConvert.SerializeObject(updatedDbSchool, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented,
            }));

            Assert.AreEqual(2, updatedDbSchool.Classes.First(c => c.Id == 1).ClassTeachers.Count);
            Assert.AreEqual(2, updatedDbSchool.Classes.First(c => c.Id == 2).ClassTeachers.Count);
        }


        [Test]
        public void S018_Update_Teacher_Info_Through_The_Class()
        {
            var updatedSchool = JsonConvert.DeserializeObject<School>(@"
                    {
                      'Id': 1,
                      'Name': 'The First',
                      'Type': 1,
                      'Address': [
                        'Akdeniz',
                        'Mersin'
                      ],
                      'Classes': [
                        {
                          'Id': 1,
                          'SchoolId': 1,
                          'Level': 1,
                          'Capacity': 20,
                          'ClassTeachers': [
                            {
                              'ClassId': 1,
                              'TeacherId': 'ec13122e-3ec5-4698-b254-e660d01f37ca',
                              'Teacher':{
                                    'Id': 'ec13122e-3ec5-4698-b254-e660d01f37ca',
                                    'Name': 'Ahmad Osta',
                                    'DateOfBirth': '1980-10-01 03:00:00.0000000 +00:00'
                               }
                            },
                            {
                              'ClassId': 1,
                              'TeacherId': 'D814E2DE-D097-40AE-BCCF-C4C5F1415D6B',
                              'Teacher':{
                                    'Id': 'D814E2DE-D097-40AE-BCCF-C4C5F1415D6B',
                                    'Name': 'Hasan'
                               }
                            }
                          ]
                        },
                        {
                          'Id': 2,
                          'SchoolId': 1,
                          'Level': 2,
                          'Capacity': 10,
                          'ClassTeachers': [
                            {
                              'ClassId': 2,
                              'TeacherId': 'ec13122e-3ec5-4698-b254-e660d01f37ca',
                              'Teacher':{
                                    'Id': 'ec13122e-3ec5-4698-b254-e660d01f37ca',
                                    'Name': 'Ahmad Osta',
                                    'DateOfBirth': '1980-10-01 03:00:00.0000000 +00:00'
                               }
                            },
                            {
                              'ClassId': 2,
                              'TeacherId': '7AB15219-5FFA-406C-B092-94636B413E05',
                              'Teacher':{
                                    'Id': '7AB15219-5FFA-406C-B092-94636B413E05',
                                    'Name': 'Mohammad',
                                    'DateOfBirth': '1980-09-01 03:00:00.0000000 +00:00'
                               }
                            }
                          ]
                        }
                      ]
                    }
");

            var dbSchool = dbContext.Schools
                .Include(s => s.Classes)
                .ThenInclude(s => s.ClassTeachers)
                .ThenInclude(x => x.Teacher)
                .FirstOrDefault();

            dbContext.InsertUpdateOrDeleteGraph(updatedSchool, dbSchool);

            dbContext.SaveChanges();

            var updatedDbSchool = dbContext.Schools
                .Include(s => s.Classes)
                .ThenInclude(s => s.ClassTeachers)
                .ThenInclude(x => x.Teacher)
                .FirstOrDefault();

            Console.WriteLine(JsonConvert.SerializeObject(updatedDbSchool, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented,
            }));

            Assert.AreEqual(2, updatedDbSchool.Classes.First(c => c.Id == 1).ClassTeachers.Count);
            Assert.AreEqual(2, updatedDbSchool.Classes.First(c => c.Id == 2).ClassTeachers.Count);

            Assert.AreEqual("Ahmad Osta", updatedDbSchool
                .Classes.First(c => c.Id == 2)
                .ClassTeachers.First(t => t.TeacherId == Guid.Parse("ec13122e-3ec5-4698-b254-e660d01f37ca"))
                .Teacher.Name);

            Assert.IsNotNull(updatedDbSchool
                .Classes.First(c => c.Id == 2)
                .ClassTeachers.First(t => t.TeacherId == Guid.Parse("ec13122e-3ec5-4698-b254-e660d01f37ca"))
                .Teacher.DateOfBirth);
        }


        [Test]
        public void S019_Update_Teacher_Info_Directly()
        {
            var updatedTeacher = JsonConvert.DeserializeObject<Teacher>(@"
                 {
                    'Id': 'ec13122e-3ec5-4698-b254-e660d01f37ca',
                    'Name': 'Ahmad Bey',
                    'DateOfBirth': '1980-11-01 03:00:00.0000000 +00:00'
                 }");

            var dbTeacher = dbContext.Teachers
                .FirstOrDefault(x => x.Id == Guid.Parse("ec13122e-3ec5-4698-b254-e660d01f37ca"));

            dbContext.InsertUpdateOrDeleteGraph(updatedTeacher, dbTeacher);

            dbContext.SaveChanges();

            var updatedDbTeacher = dbContext.Teachers
                .FirstOrDefault(x => x.Id == Guid.Parse("ec13122e-3ec5-4698-b254-e660d01f37ca"));

            Console.WriteLine(JsonConvert.SerializeObject(updatedDbTeacher, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented,
            }));

            Assert.AreEqual("Ahmad Bey", updatedDbTeacher.Name);
            Assert.AreEqual(DateTimeOffset.Parse("1980-11-01 03:00:00.0000000 +00:00"), updatedDbTeacher.DateOfBirth);
        }


        [Test]
        public void S020_Remove_Teacher_From_Class_Should_Not_Delete_The_Teacher()
        {
            var updatedSchool = JsonConvert.DeserializeObject<School>(@"
                    {
                      'Id': 1,
                      'Name': 'The First',
                      'Type': 1,
                      'Address': [
                        'Akdeniz',
                        'Mersin'
                      ],
                      'Classes': [
                        {
                          'Id': 1,
                          'SchoolId': 1,
                          'Level': 1,
                          'Capacity': 20,
                          'ClassTeachers': [
                            {
                              'ClassId': 1,
                              'TeacherId': 'ec13122e-3ec5-4698-b254-e660d01f37ca'
                            }
                          ]
                        },
                        {
                          'Id': 2,
                          'SchoolId': 1,
                          'Level': 2,
                          'Capacity': 10,
                          'ClassTeachers': [
                            {
                              'ClassId': 2,
                              'TeacherId': 'ec13122e-3ec5-4698-b254-e660d01f37ca'
                            },
                            {
                              'ClassId': 2,
                              'TeacherId': '7AB15219-5FFA-406C-B092-94636B413E05'
                            }
                          ]
                        }
                      ]
                    }
");

            var dbSchool = dbContext.Schools
                .Include(s => s.Classes)
                .ThenInclude(s => s.ClassTeachers)
                .FirstOrDefault();

            dbContext.InsertUpdateOrDeleteGraph(updatedSchool, dbSchool);

            dbContext.SaveChanges();

            var updatedDbSchool = dbContext.Schools
                .Include(s => s.Classes)
                .ThenInclude(s => s.ClassTeachers)
                .FirstOrDefault();

            var updatedDbTeacher = dbContext.Teachers
                .Include(s => s.ClassTeachers)
                .ThenInclude(s => s.Class)
                .FirstOrDefault(t => t.Id == Guid.Parse("D814E2DE-D097-40AE-BCCF-C4C5F1415D6B"));

            Console.WriteLine(JsonConvert.SerializeObject(updatedDbSchool, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented,
            }));
            
            Console.WriteLine("-------------------------------------");

            Console.WriteLine(JsonConvert.SerializeObject(updatedDbTeacher, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented,
            }));

            Assert.AreEqual(1, updatedDbSchool.Classes.First(c => c.Id == 1).ClassTeachers.Count);
            Assert.AreEqual(2, updatedDbSchool.Classes.First(c => c.Id == 2).ClassTeachers.Count);
            Assert.IsNotNull(updatedDbTeacher);
            Assert.AreEqual(0, updatedDbTeacher.ClassTeachers.Count);
        }
    }
}
