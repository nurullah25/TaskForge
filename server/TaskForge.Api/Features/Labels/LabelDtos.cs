using System.ComponentModel.DataAnnotations;

namespace TaskForge.Api.Features.Labels;

public record LabelDto(int Id, string Name, string Color);

public class SaveLabelRequest
{
    [Required, MaxLength(30)]
    public string Name { get; set; } = "";

    [Required, RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Color must be a hex value like #2563eb.")]
    public string Color { get; set; } = "";
}

public class SetTaskLabelsRequest
{
    public List<int> LabelIds { get; set; } = [];
}
