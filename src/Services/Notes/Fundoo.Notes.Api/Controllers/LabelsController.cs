
using Fundoo.Notes.Api.Contracts;
using Fundoo.Notes.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Fundoo.Notes.Api.Controllers;

[ApiController]
[Route("api/labels")]
[Authorize]
public class LabelsController : ControllerBase
{
    private readonly ILabelService _labelService;

    public LabelsController(ILabelService labelService)
    {
        _labelService = labelService;
    }

    // GET: api/labels
    // Get all labels belonging to the logged-in user.
    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var labels = await _labelService.GetAllAsync(
            userId, cancellationToken);

        return Ok(labels.Select(label => new LabelResponse
        {
            Id = label.Id,
            Name = label.Name
        }));
    }

    // GET: api/labels/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var label = await _labelService.GetByIdAsync(
            id, userId, cancellationToken);

        if (label is null)
            return NotFound();

        return Ok(new LabelResponse
        {
            Id = label.Id,
            Name = label.Name
        });
    }

    // POST: api/labels
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateLabelRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var label = await _labelService.CreateAsync(
                userId, request.Name, cancellationToken);

            var response = new LabelResponse
            {
                Id = label.Id,
                Name = label.Name
            };

            return CreatedAtAction(
                nameof(GetById),
                new { id = label.Id },
                response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new MessageResponse
            {
                Message = ex.Message
            });
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            return Conflict(new MessageResponse
            {
                Message = "A label with this name already exists."
            });
        }
    }

    // PUT: api/labels/{id}
    // Rename an existing label.
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Rename(
        Guid id,
        [FromBody] UpdateLabelRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var updated = await _labelService.RenameAsync(
                id, userId, request.Name, cancellationToken);

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
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            return Conflict(new MessageResponse
            {
                Message = "A label with this name already exists."
            });
        }
    }

    // DELETE: api/labels/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var deleted = await _labelService.DeleteAsync(
            id, userId, cancellationToken);

        if (!deleted)
            return NotFound();

        return NoContent();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var userIdValue = User.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdValue, out userId);
    }
}
