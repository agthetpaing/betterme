using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BetterMe.Infrastructure.Data;
using BetterMe.Infrastructure.Entities;
using BetterMe.Shared.DTOs.Resources;
using BetterMe.Shared.Enums;
using System.Security.Claims;

namespace BetterMe.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ResourcesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ResourcesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetResources(
        [FromQuery] ResourceType? type = null,
        [FromQuery] string? tag = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Resources
            .Include(r => r.TagMappings).ThenInclude(m => m.Tag)
            .Include(r => r.Author)
            .Where(r => r.IsPublished)
            .AsQueryable();

        if (type.HasValue) query = query.Where(r => r.Type == type.Value);
        if (!string.IsNullOrWhiteSpace(tag))
            query = query.Where(r => r.TagMappings.Any(m => m.Tag.Slug == tag));

        var total = await query.CountAsync();
        var resources = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = resources.Select(MapToDto).ToList();
        return Ok(new PagedResult<ResourceDto>
        {
            Items = dtos,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetResource(Guid id)
    {
        var resource = await _db.Resources
            .Include(r => r.TagMappings).ThenInclude(m => m.Tag)
            .Include(r => r.Author)
            .FirstOrDefaultAsync(r => r.Id == id && r.IsPublished);

        if (resource == null) return NotFound();
        return Ok(MapToDto(resource));
    }

    [HttpPost]
    [Authorize(Roles = "Psychologist,Admin")]
    public async Task<IActionResult> CreateResource([FromBody] CreateResourceRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var authorId = GetUserId();
        var resource = new Resource
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            ContentUrl = request.ContentUrl,
            ThumbnailUrl = request.ThumbnailUrl,
            Type = request.Type,
            IsPublished = request.IsPublished,
            AuthorId = authorId
        };

        _db.Resources.Add(resource);

        foreach (var slug in request.TagSlugs)
        {
            var tag = await _db.ResourceTags.FirstOrDefaultAsync(t => t.Slug == slug);
            if (tag != null)
                _db.ResourceTagMappings.Add(new ResourceTagMapping { ResourceId = resource.Id, TagId = tag.Id });
        }

        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetResource), new { id = resource.Id }, MapToDto(resource));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Psychologist,Admin")]
    public async Task<IActionResult> UpdateResource(Guid id, [FromBody] CreateResourceRequest request)
    {
        var resource = await _db.Resources
            .Include(r => r.TagMappings)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (resource == null) return NotFound();

        resource.Title = request.Title;
        resource.Description = request.Description;
        resource.ContentUrl = request.ContentUrl;
        resource.ThumbnailUrl = request.ThumbnailUrl;
        resource.Type = request.Type;
        resource.IsPublished = request.IsPublished;
        resource.UpdatedAt = DateTime.UtcNow;

        _db.ResourceTagMappings.RemoveRange(resource.TagMappings);
        foreach (var slug in request.TagSlugs)
        {
            var tag = await _db.ResourceTags.FirstOrDefaultAsync(t => t.Slug == slug);
            if (tag != null)
                _db.ResourceTagMappings.Add(new ResourceTagMapping { ResourceId = resource.Id, TagId = tag.Id });
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Psychologist,Admin")]
    public async Task<IActionResult> DeleteResource(Guid id)
    {
        var resource = await _db.Resources.FindAsync(id);
        if (resource == null) return NotFound();

        _db.Resources.Remove(resource);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("tags")]
    public async Task<IActionResult> GetTags()
    {
        var tags = await _db.ResourceTags
            .Select(t => new ResourceTagDto { Id = t.Id, Name = t.Name, Slug = t.Slug })
            .ToListAsync();
        return Ok(tags);
    }

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? throw new UnauthorizedAccessException();

    private static ResourceDto MapToDto(Resource r) => new()
    {
        Id = r.Id,
        Title = r.Title,
        Description = r.Description,
        ContentUrl = r.ContentUrl,
        ThumbnailUrl = r.ThumbnailUrl,
        Type = r.Type,
        IsPublished = r.IsPublished,
        CreatedAt = r.CreatedAt,
        AuthorName = $"{r.Author?.FirstName} {r.Author?.LastName}".Trim(),
        Tags = r.TagMappings?.Select(m => m.Tag.Name).ToList() ?? new()
    };
}
