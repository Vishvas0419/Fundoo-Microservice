
using Fundoo.Notification.Api.Contracts;
using Fundoo.Notification.Application.Contracts;
using Fundoo.Notification.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fundoo.Notification.Api.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationController> _logger;

    private static int _testAttemptCount = 0;

    public NotificationController(
        INotificationService notificationService,
        ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpPost("email")]
    public async Task<IActionResult> SendEmail(
        [FromBody] SendEmailRequest request,
        CancellationToken cancellationToken)
    {
        try
        {

            //await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            //var attempt = Interlocked.Increment(ref _testAttemptCount);

            //if (attempt <= 2)
            //{
            //    return StatusCode(503, new MessageResponse
            //    {
            //        Message = "Service temporarily unavailable."
            //    });
            //}

            await _notificationService.SendEmailAsync(
                request.To,
                request.Subject,
                request.Body,
                cancellationToken);

            return Ok(new MessageResponse
            {
                Message = "Email sent successfully."
            });
            



            //only used for testing : retry and circuit breaker
            //return StatusCode(503, new MessageResponse
            //{
            //    Message = "Service temporarily unavailable."
            //});



        }
        catch (ArgumentException ex)
        {
            return BadRequest(new MessageResponse
            {
                Message = ex.Message
            });
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            return StatusCode(499, new MessageResponse
            {
                Message = "The request was cancelled."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send notification email.");

            return StatusCode(500, new MessageResponse
            {
                Message = "Unable to send email."
            });
        }
    }
}
