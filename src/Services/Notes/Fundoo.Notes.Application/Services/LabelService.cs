using Fundoo.Notes.Application.Interfaces;
using Fundoo.Notes.Domain.Entities;

namespace Fundoo.Notes.Application.Services;

public class LabelService : ILabelService
{
    private readonly ILabelRepository _repository;

    public LabelService(ILabelRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<Label>> GetAllAsync(
        Guid userId, CancellationToken ct = default)
        => _repository.GetAllAsync(userId, ct);

    public Task<Label?> GetByIdAsync(
        Guid labelId, Guid userId, CancellationToken ct = default)
        => _repository.GetByIdAsync(labelId, userId, ct);

    public async Task<Label> CreateAsync(
        Guid userId, string name, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.");

        name = ValidateName(name);

        var label = new Label
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name
        };

        await _repository.AddAsync(label, ct);
        return label;
    }

    public async Task<bool> RenameAsync(
        Guid labelId, Guid userId, string name,
        CancellationToken ct = default)
    {
        name = ValidateName(name);

        var label = await _repository.GetByIdAsync(labelId, userId, ct);
        if (label is null)
            return false;

        label.Name = name;
        return await _repository.UpdateAsync(label, ct);
    }

    public Task<bool> DeleteAsync(
        Guid labelId, Guid userId, CancellationToken ct = default)
        => _repository.DeleteAsync(labelId, userId, ct);

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Label name is required.");

        name = name.Trim();

        if (name.Length > 100)
            throw new ArgumentException(
                "Label name cannot exceed 100 characters.");

        return name;
    }
}