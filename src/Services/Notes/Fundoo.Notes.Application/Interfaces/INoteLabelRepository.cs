
namespace Fundoo.Notes.Application.Interfaces;

public interface INoteLabelRepository
{
    Task<bool> AddLabelToNoteAsync(
        Guid noteId,
        Guid labelId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveLabelFromNoteAsync(
        Guid noteId,
        Guid labelId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
//This interface manages attaching and removing labels from notes. The implementation must verify that both the note and label belong to the same user.