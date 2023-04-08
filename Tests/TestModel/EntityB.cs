using System.ComponentModel.DataAnnotations;

namespace TestModel;

public class EntityB
{
    public required int Id { get; set; }
    [MaxLength(50)]
    public required string RequiredText { get; set; }
    public List<EntityA> ManyToManyEntitiesA { get; set; } = new();
}