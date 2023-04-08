using System.ComponentModel.DataAnnotations;

namespace TestModel;


public class AggregateA
{
    public int Id { get; set; }
    [MaxLength(50)] public required string RequiredText { get; set; }
    public ICollection<CompositeEntity> CompositeEntities { get; set; } = new HashSet<CompositeEntity>();
    public ICollection<AggregateB> RelatedAggregates { get; set; } = new HashSet<AggregateB>();
}