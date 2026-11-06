
namespace Fundoo.Notes.Domain.Entities;

public class Label
{
    public Guid Id { get; set; }

    // Labels belong to a particular user.
    public Guid UserId { get; set; }

    public string Name { get; set; } = "";

    public List<NoteLabel> NoteLabels { get; set; } = new();
}
