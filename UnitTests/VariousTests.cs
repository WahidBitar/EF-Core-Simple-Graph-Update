using System;
using System.Linq;
using Diwink.Extensions.EntityFrameworkCore;
using FakeModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;


namespace UnitTests
{
    [TestFixture]
    public class VariousTests
    {
        private ServiceProvider serviceProvider;
        private IServiceScope scope;
        private FakeSchoolsDbContext dbContext;


        public VariousTests()
        {
            var configuration = TestHelpers.InitConfiguration();
            var services = new ServiceCollection();

            services.AddDbContext<FakeSchoolsDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("FakeSchoolsDb")));

            serviceProvider = services.BuildServiceProvider();
        }


        [SetUp]
        public void Setup()
        {
            scope = serviceProvider.CreateScope();
            dbContext = scope.ServiceProvider.GetService<FakeSchoolsDbContext>();
        }


        [TearDown]
        public void TearDown()
        {
            scope.Dispose();
        }


        [Test]
        public void Different_Composite_Key_Values_Should_Not_Equal()
        {
            var firstComposition = new ClassTeacher()
            {
                ClassId = 1,
                TeacherId = Guid.NewGuid(),
            };
            var secondComposition = new ClassTeacher()
            {
                ClassId = 1,
                TeacherId = Guid.NewGuid(),
            };

            var firstKey = dbContext.Entry(firstComposition).GetPrimaryKeyValues();
            var secondKey = dbContext.Entry(secondComposition).GetPrimaryKeyValues();

            Assert.False(firstKey.SequenceEqual(secondKey));
        }


        [Test]
        public void Same_Composite_Key_Values_Should_Be_Equal()
        {
            var firstComposition = new ClassTeacher()
            {
                ClassId = 1,
                TeacherId = Guid.Parse("{6D83B2B3-F28E-4D2D-8671-93F8E6AB08C1}"),
            };
            var secondComposition = new ClassTeacher()
            {
                TeacherId = Guid.Parse("{6D83B2B3-F28E-4D2D-8671-93F8E6AB08C1}"),
                ClassId = 1,
            };

            var firstKey = dbContext.Entry(firstComposition).GetPrimaryKeyValues();
            var secondKey = dbContext.Entry(secondComposition).GetPrimaryKeyValues();

            Assert.True(firstKey.SequenceEqual(secondKey));
        }
    }
}