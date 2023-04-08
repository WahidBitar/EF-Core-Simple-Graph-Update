using System.ComponentModel.DataAnnotations;

namespace TestModel;

public class ValueObjectA
{
    [MaxLength(50)]
    public required string RequiredText { get; set; }
    public bool? OptionalBit { get; set; }
}