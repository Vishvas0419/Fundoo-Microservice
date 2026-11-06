using Fundoo.Notes.Domain.Entities;

namespace Fundoo.Notes.Application.Interfaces;

public interface ILabelService
{
    Task<IReadOnlyList<Label>> GetAllAsync(
        Guid userId, CancellationToken ct = default);

    Task<Label?> GetByIdAsync(
        Guid labelId, Guid userId, CancellationToken ct = default);

    Task<Label> CreateAsync(
        Guid userId, string name, CancellationToken ct = default);

    Task<bool> RenameAsync(
        Guid labelId, Guid userId, string name,
        CancellationToken ct = default);

    Task<bool> DeleteAsync(
        Guid labelId, Guid userId, CancellationToken ct = default);
}