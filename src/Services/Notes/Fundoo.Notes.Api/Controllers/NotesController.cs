using Fundoo.Notes.Api.Contracts;
using Fundoo.Notes.Application.Interfaces;
using Fundoo.Notes.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fundoo.Notes.Api.Controllers;

[ApiController]
[Route("api/notes")]
[Authorize]
public class NotesController : ControllerBase
{
    private readonly INoteService _noteService;
    private readonly INoteLabelRepository _noteLabelRepository;

    public NotesController(
        INoteService noteService,
        INoteLabelRepository noteLabelRepository)
    {
        _noteService = noteService;
        _noteLabelRepository = noteLabelRepository;
    }

    // GET: api/notes
    // Returns active notes only.
    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var notes = await _noteService.GetByCategoryAsync(
            userId, "active", cancellationToken);

        return Ok(notes.Select(MapToResponse));
    }

    // GET: api/notes/archived
    [HttpGet("archived")]
    public async Task<IActionResult> GetArchived(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var notes = await _noteService.GetByCategoryAsync(
            userId, "archived", cancellationToken);

        return Ok(notes.Select(MapToResponse));
    }

    // GET: api/notes/trash
    [HttpGet("trash")]
    public async Task<IActionResult> GetTrash(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var notes = await _noteService.GetByCategoryAsync(
            userId, "trashed", cancellationToken);

        return Ok(notes.Select(MapToResponse));
    }

    // GET: api/notes/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var note = await _noteService.GetByIdAsync(
            id, userId, cancellationToken);

        if (note is null)
            return NotFound();

        return Ok(MapToResponse(note));
    }

    // POST: api/notes
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateNoteRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var note = await _noteService.CreateAsync(
                userId,
                request.Title,
                request.Content,
                request.Color,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = note.Id },
                MapToResponse(note));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new MessageResponse
            {
                Message = ex.Message
            });
        }
    }

    // PUT: api/notes/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateNoteRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var updated = await _noteService.UpdateAsync(
                id,
                userId,
                request.Title,
                request.Content,
                request.Color,
                cancellationToken);

            if (!updated)
                return NotFound();

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new MessageResponse
            {
                Message = ex.Message
            });
        }
    }

    // PUT: api/notes/{id}/pin?isPinned=true|false
    [HttpPut("{id:guid}/pin")]
    public async Task<IActionResult> SetPinned(
        Guid id,
        [FromQuery] bool isPinned,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var updated = await _noteService.SetPinnedAsync(
            id, userId, isPinned, cancellationToken);

        if (!updated)
            return NotFound();

        return NoContent();
    }

    // PUT: api/notes/{id}/archive
    [HttpPut("{id:guid}/archive")]
    public async Task<IActionResult> Archive(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var updated = await _noteService.SetArchivedAsync(
            id, userId, true, cancellationToken);

        if (!updated)
            return NotFound();

        return NoContent();
    }

    // PUT: api/notes/{id}/unarchive
    [HttpPut("{id:guid}/unarchive")]
    public async Task<IActionResult> Unarchive(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var updated = await _noteService.SetArchivedAsync(
            id, userId, false, cancellationToken);

        if (!updated)
            return NotFound();

        return NoContent();
    }

    // PUT: api/notes/{id}/trash
    [HttpPut("{id:guid}/trash")]
    public async Task<IActionResult> MoveToTrash(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var updated = await _noteService.SetTrashedAsync(
            id, userId, true, cancellationToken);

        if (!updated)
            return NotFound();

        return NoContent();
    }

    // PUT: api/notes/{id}/restore
    [HttpPut("{id:guid}/restore")]
    public async Task<IActionResult> Restore(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var updated = await _noteService.SetTrashedAsync(
            id, userId, false, cancellationToken);

        if (!updated)
            return NotFound();

        return NoContent();
    }

    // DELETE: api/notes/{id}
    // Repository must only permanently delete notes already in trash.
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var deleted = await _noteService.DeleteAsync(
            id, userId, cancellationToken);

        if (!deleted)
        {
            return NotFound(new MessageResponse
            {
                Message = "Note not found or is not in trash."
            });
        }

        return NoContent();
    }

    // PUT: api/notes/{noteId}/labels/{labelId}
    [HttpPut("{noteId:guid}/labels/{labelId:guid}")]
    public async Task<IActionResult> AddLabel(
        Guid noteId,
        Guid labelId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var added = await _noteLabelRepository.AddLabelToNoteAsync(
            noteId, labelId, userId, cancellationToken);

        if (!added)
        {
            return NotFound(new MessageResponse
            {
                Message = "Note or label not found, or label already attached."
            });
        }

        return NoContent();
    }

    // DELETE: api/notes/{noteId}/labels/{labelId}
    [HttpDelete("{noteId:guid}/labels/{labelId:guid}")]
    public async Task<IActionResult> RemoveLabel(
        Guid noteId,
        Guid labelId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var removed = await _noteLabelRepository.RemoveLabelFromNoteAsync(
            noteId, labelId, userId, cancellationToken);

        if (!removed)
        {
            return NotFound(new MessageResponse
            {
                Message = "Note, label, or their association was not found."
            });
        }

        return NoContent();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var userIdValue = User.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdValue, out userId);
    }

    private static NoteResponse MapToResponse(Note note)
    {
        return new NoteResponse
        {
            Id = note.Id,
            Title = note.Title,
            Content = note.Content,
            Color = note.Color,
            IsPinned = note.IsPinned,
            IsArchived = note.IsArchived,
            IsTrashed = note.IsTrashed,
            CreatedAtUtc = note.CreatedAtUtc,
            UpdatedAtUtc = note.UpdatedAtUtc
        };
    }
}
