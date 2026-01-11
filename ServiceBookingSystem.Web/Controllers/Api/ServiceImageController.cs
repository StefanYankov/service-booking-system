using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBookingSystem.Application.Interfaces;
using ServiceBookingSystem.Core.Constants;

namespace ServiceBookingSystem.Web.Controllers.Api;

[Authorize]
[Route("api/services/{serviceId}/images")]
public class ServiceImageController : BaseApiController
{
    private readonly IServiceService serviceService;
    private readonly ILogger<ServiceImageController> logger;

    public ServiceImageController(
        IServiceService serviceService,
        ILogger<ServiceImageController> logger)
    {
        this.serviceService = serviceService;
        this.logger = logger;
    }

    /// <summary>
    /// Uploads an image for a specific service.
    /// </summary>
    /// <param name="serviceId">The ID of the service.</param>
    /// <param name="file">The image file to upload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The URL of the uploaded image.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<string>> UploadImage(int serviceId, IFormFile file, CancellationToken cancellationToken)
    {
        var userId = this.GetCurrentUserId();
        logger.LogDebug("API: UploadImage request for Service {ServiceId} by User {UserId}", serviceId, userId);

        if (userId == null) return Unauthorized();

        // --- Validation Logic ---
        
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        if (file.Length > ValidationConstraints.Image.MaxFileSize)
        {
            return BadRequest($"File size exceeds the limit of {ValidationConstraints.Image.MaxFileSize / 1024 / 1024} MB.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!ValidationConstraints.Image.AllowedExtensions.Contains(extension))
        {
            return BadRequest("Invalid file type. Only JPG, JPEG, PNG, and WEBP are allowed.");
        }

        // --- End Validation ---

        var imageUrl = await this.serviceService.AddImageAsync(serviceId, userId, file, cancellationToken);
        
        // Return 201 Created with the URL
        return Created(imageUrl, new { Url = imageUrl });
    }

    /// <summary>
    /// Deletes an image from a service.
    /// </summary>
    /// <param name="serviceId">The ID of the service.</param>
    /// <param name="imageId">The ID of the image to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No Content.</returns>
    [HttpDelete("{imageId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteImage(int serviceId, int imageId, CancellationToken cancellationToken)
    {
        var userId = this.GetCurrentUserId();
        logger.LogDebug("API: DeleteImage request for Image {ImageId} in Service {ServiceId} by User {UserId}", imageId, serviceId, userId);

        if (userId == null) return Unauthorized();

        await this.serviceService.DeleteImageAsync(serviceId, userId, imageId, cancellationToken);
        return NoContent();
    }
}