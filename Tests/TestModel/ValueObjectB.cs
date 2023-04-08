using System.ComponentModel.DataAnnotations;

namespace TestModel;

public class ValueObjectB
{
    [MaxLength(50)]
    public required string RequiredText { get; set; }
    public int? OptionalInt { get; set; }
}