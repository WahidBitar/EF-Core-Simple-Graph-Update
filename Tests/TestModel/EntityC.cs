using System.ComponentModel.DataAnnotations;

namespace TestModel;

public class EntityC
{
    public required int AggregateBId { get; set; }
    [MaxLength(50)]
    public required string RequiredText { get; set; }
    public required ValueObjectA RequiredOwnedObject { get; set; }
}