
namespace Fundoo.Notes.Api.Contracts;

public class UpdateNoteRequest
{
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string? Color { get; set; }
    public bool IsPinned { get; set; }

}
