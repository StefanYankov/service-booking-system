using Microsoft.AspNetCore.Http;
using ServiceBookingSystem.Application.DTOs.Image;

namespace ServiceBookingSystem.Application.Interfaces.Infrastructure;

/// <summary>
/// Defines the contract for an external image storage service (e.g., Cloudinary, Azure Blob).
/// </summary>
public interface IImageService
{
    /// <summary>
    /// Uploads an image file to the storage provider.
    /// </summary>
    /// <param name="file">The image file to upload.</param>
    /// <returns>The result containing the URL and Public ID of the uploaded image.</returns>
    Task<ImageStorageResult> AddImageAsync(IFormFile file);

    /// <summary>
    /// Deletes an image from the storage provider.
    /// </summary>
    /// <param name="publicId">The public identifier of the image to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteImageAsync(string publicId);
}