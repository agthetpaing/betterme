namespace BetterMe.Infrastructure.Entities;

public class ResourceTag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    public ICollection<ResourceTagMapping> ResourceMappings { get; set; } = new List<ResourceTagMapping>();
}
