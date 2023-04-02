using System.ComponentModel.DataAnnotations;

namespace TestModel;


public class AggregateA
{
    public required int Id { get; set; }
    [MaxLength(50)]
    public required string RequiredText { get; set; }
    
    public ICollection<CompositeEntity> CompositeEntities { get; set; } = new HashSet<CompositeEntity>();
    public ICollection<AggregateB> RelatedAggregates { get; set; } = new HashSet<AggregateB>();
}

public class CompositeEntity
{
    public required int AggregateAId { get; set; }
    public required int SomeId { get; set; }

    [MaxLength(50)] public required string RequiredText { get; set; }

    public required AggregateA RelatedAggregate { get; set; }
}

public class AggregateB
{
    public required int Id { get; set; }
    public int? AggregateAId { get; set; }

    public string? OptionalText { get; set; }
    public double? OptionalNumber { get; set; }
    public DateTimeOffset? OptionalDateTimeOffset { get; set; }
    [MaxLength(50)] public required string RequiredText { get; set; }
    public required DateTimeOffset RequiredDateTimeOffset { get; set; }
    public AggregateA? RelatedAggregate { get; set; }
    public required EntityC OneToOneSubEntity { get; set; }
    public ICollection<EntityA>? ManySubEntities { get; set; }
}

public class EntityA
{
    public required int Id { get; set; }
    [MaxLength(50)]
    public required string RequiredText { get; set; }
    public ValueObjectA? OptionalOwnedObject { get; set; }
    public List<ValueObjectB> ManyOwnedObjects { get; set; }
    public List<EntityB> ManyToManyEntitiesB { get; set; }
}

public class EntityB
{
    public required int Id { get; set; }
    [MaxLength(50)]
    public required string RequiredText { get; set; }
    public List<EntityA> ManyToManyEntitiesA { get; set; } = new();
}

public class EntityC
{
    public required int AggregateBId { get; set; }
    [MaxLength(50)]
    public required string RequiredText { get; set; }
    public required ValueObjectA RequiredOwnedObject { get; set; }
}

public class ValueObjectA
{
    [MaxLength(50)]
    public required string RequiredText { get; set; }
    public bool? OptionalBit { get; set; }
}

public class ValueObjectB
{
    [MaxLength(50)]
    public required string RequiredText { get; set; }
    public int? OptionalInt { get; set; }
}

