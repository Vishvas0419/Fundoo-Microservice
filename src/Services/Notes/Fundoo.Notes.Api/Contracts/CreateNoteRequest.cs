
namespace Fundoo.Notes.Api.Contracts;

public class CreateNoteRequest
{
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string? Color { get; set; }
}
