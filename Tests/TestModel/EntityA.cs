using System.ComponentModel.DataAnnotations;

namespace TestModel;

public class EntityA
{
    public required int Id { get; set; }
    [MaxLength(50)]
    public required string RequiredText { get; set; }
    public ValueObjectA? OptionalOwnedObject { get; set; }
    public List<ValueObjectB> ManyOwnedObjects { get; set; }
    public List<EntityB> ManyToManyEntitiesB { get; set; }
}