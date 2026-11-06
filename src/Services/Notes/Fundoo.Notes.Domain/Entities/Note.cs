
namespace Fundoo.Notes.Domain.Entities;

public class Note
{
    public Guid Id { get; set; }

    // Identifies the owner from the User API.
    // No foreign key because users live in a separate database.
    public Guid UserId { get; set; }

    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string? Color { get; set; }

    public bool IsPinned { get; set; }
    public bool IsArchived { get; set; }
    public bool IsTrashed { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public List<NoteLabel> NoteLabels { get; set; } = new();
}
