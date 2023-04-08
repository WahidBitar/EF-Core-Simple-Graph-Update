using System.ComponentModel.DataAnnotations;

namespace TestModel;

public class CompositeEntity
{
    public required int AggregateAId { get; set; }
    public required int SomeId { get; set; }

    [MaxLength(50)] public required string RequiredText { get; set; }

    public AggregateA? RelatedAggregate { get; set; }
}