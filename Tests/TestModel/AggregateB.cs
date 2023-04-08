using System.ComponentModel.DataAnnotations;

namespace TestModel;

public class AggregateB
{
    public int Id { get; set; }
    public int? AggregateAId { get; set; }

    public string? OptionalText { get; set; }
    public double? OptionalNumber { get; set; }
    public DateTimeOffset? OptionalDateTimeOffset { get; set; }
    [MaxLength(50)] public required string RequiredText { get; set; }
    public required DateTimeOffset RequiredDateTimeOffset { get; set; }
    public AggregateA? RelatedAggregate { get; set; }
    public EntityC? OneToOneSubEntity { get; set; }
    public ICollection<EntityA>? ManySubEntities { get; set; }
}