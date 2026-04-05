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

## v2 Rebuild (EF Core 10+)

The v2 rebuild (`src/`) targets .NET 10 and EF Core 10.x with explicit,
contract-driven relationship semantics.

### Supported Relationship Patterns

| Pattern | Add | Update | Remove | Outcome |
|---------|-----|--------|--------|---------|
| Pure many-to-many (skip nav) | Link created | Properties updated | Link removed | Related entity preserved |
| Payload many-to-many (join entity) | Association inserted | Payload updated | Association deleted | Related entities preserved |
| Required one-to-one | Dependent inserted | Properties updated | Dependent deleted | Cascade delete |
| Optional one-to-one | Dependent inserted | Properties updated | FK nulled | Dependent preserved |

### Rejection Behavior

| Scenario | Exception |
|----------|-----------|
| Unsupported relationship mutated (e.g., one-to-many) | `UnsupportedNavigationMutatedException` |
| Unloaded navigation with mutations in updated graph | `UnloadedNavigationMutationException` |
| Mixed supported + unsupported mutations | `PartialMutationNotAllowedException` |
| Unsupported relationship unchanged | Silently skipped |

### v2 Usage

```csharp
var updated = BuildDesiredState(); // detached graph

var existing = await dbContext.Courses
    .Include(c => c.Tags)
    .Include(c => c.Policy)
    .Include(c => c.MentorAssignments)
    .FirstAsync(c => c.Id == id);

dbContext.InsertUpdateOrDeleteGraph(updated, existing);
await dbContext.SaveChangesAsync();
```

Please don't hesitate to contribute or give us your feedback and/or advice :rose: :rose:
