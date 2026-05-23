namespace Suggestme.Infrastructure.Entities;

public class ResourceTagMapping
{
    public Guid ResourceId { get; set; }
    public int TagId { get; set; }

    public Resource Resource { get; set; } = null!;
    public ResourceTag Tag { get; set; } = null!;
}
