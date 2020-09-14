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
        private School school;
        private List<Teacher> teachers;
        private ICollection<Class> classes;
        private List<Student> students;


        public GraphUpdateTests()
        {
            var configuration = TestHelpers.InitConfiguration();
            var services = new ServiceCollection();

            services.AddDbContext<FakeSchoolsDbContext>(options => options
                .EnableDetailedErrors()
                .EnableSensitiveDataLogging() /*
                .UseSqlServer(configuration.GetConnectionString("FakeSchoolsDb"))*/
                .UseInMemoryDatabase("FakeSchoolsDb"));

            serviceProvider = services.BuildServiceProvider();
        }


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
            students = new List<Student>
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
            classes = new List<Class>
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


        [TearDown]
        public void TearDown()
        {
            scope.Dispose();
        }


        [Test]
        public void S000_Delete_Database()
        {
            dbContext.Database.EnsureDeleted();
        }


        [Test]
        public void S001_Apply_Migrations()
        {
            //dbContext.Database.Migrate();
        }


        [Test]
        public void S002_Seed_Teachers()
        {
            dbContext.Teachers.AddRange(teachers);
            dbContext.SaveChanges();
        }


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


        [Test]
        public void S004_Add_House_To_The_School()
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


        [Test]
        public void S006_Remove_The_House_From_The_School()
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
                          'ClassTeachers': [],
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

            Assert.AreEqual(1, updatedDbSchool.Classes.First(c => c.Id == 1).Students.Count());
            Assert.AreEqual(2, updatedDbSchool.Classes.First(c => c.Id == 2).Students.Count());
        }


        [Test]
        public void S011_Add_Classes_Laboratory()
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
        public void S012_Delete_First_Class_Laboratory()
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
        public void S013_Update_Class_Teachers_Of_Second_Class()
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

            Assert.AreEqual(1, updatedDbSchool.Classes.First(c => c.Id == 1).ClassTeachers.Count());
            Assert.AreEqual(2, updatedDbSchool.Classes.First(c => c.Id == 2).ClassTeachers.Count());
        }



        [Test]
        public void S014_Add_New_Teacher_To_A_Class()
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

            Assert.AreEqual(2, updatedDbSchool.Classes.First(c => c.Id == 1).ClassTeachers.Count());
            Assert.AreEqual(2, updatedDbSchool.Classes.First(c => c.Id == 2).ClassTeachers.Count());
        }


        [Test]
        public void S015_Update_Teacher_Info_Through_The_Class()
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

            Assert.AreEqual(2, updatedDbSchool.Classes.First(c => c.Id == 1).ClassTeachers.Count());
            Assert.AreEqual(2, updatedDbSchool.Classes.First(c => c.Id == 2).ClassTeachers.Count());

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
        public void S016_Update_Teacher_Info_Directly()
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
    }
}