
using Fundoo.Notes.Domain.Entities;

namespace Fundoo.Notes.Application.Interfaces;

public interface ILabelRepository
{
    Task<IReadOnlyList<Label>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Label?> GetByIdAsync(
        Guid labelId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Label label,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        Label label,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid labelId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
