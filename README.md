# Digital Wink Entity Framework Extensions
In this simple project we're going to expose the helper extenasion methods that we're using in our company.
and as a starting point we'll start witht the Graph update method.
We'll update the [Nuget Package](https://www.nuget.org/packages/Diwink.Extensions.EntityFrameworkCore/) whenever we have a new version.

## Entity Framework Core Simple Graph Update

It's a simple update method that will help you to do a full update to an aggregate graph with all related entities in it.
the update method will take the loaded aggregate entity from the DB and the passed one that may come from the API layer.
Internally the method will update just the eager loaded entities in the aggregate "The included entities"

```csharp
var updatedSchool = mapper.Map<School>(apiModel);

var dbSchool = dbContext.Schools
    .Include(s => s.Classes)
    .ThenInclude(s => s.Students)
    .FirstOrDefault();

dbContext.InsertUpdateOrDeleteGraph(updatedSchool, dbSchool);

dbContext.SaveChanges();

```

Please don't hesitate to contribute or give us your feedback and/or advice :rose: :rose: