using Microsoft.AspNetCore.Mvc;

namespace PhotobookAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PhotosController : ControllerBase
{
    private static List<Photo> _photos = new();

    [HttpGet]
    public IActionResult GetPhotos()
    {
        return Ok(_photos);
    }

    [HttpGet("{id}")]
    public IActionResult GetPhoto(int id)
    {
        var photo = _photos.FirstOrDefault(p => p.Id == id);
        if (photo == null)
            return NotFound();
        return Ok(photo);
    }

    [HttpPost]
    public IActionResult CreatePhoto([FromBody] CreatePhotoRequest request)
    {
        var photo = new Photo
        {
            Id = _photos.Count + 1,
            Title = request.Title,
            Description = request.Description,
            Url = request.Url,
            CreatedAt = DateTime.UtcNow
        };

        _photos.Add(photo);
        return CreatedAtAction(nameof(GetPhoto), new { id = photo.Id }, photo);
    }

    [HttpDelete("{id}")]
    public IActionResult DeletePhoto(int id)
    {
        var photo = _photos.FirstOrDefault(p => p.Id == id);
        if (photo == null)
            return NotFound();

        _photos.Remove(photo);
        return NoContent();
    }
}

public class Photo
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreatePhotoRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
