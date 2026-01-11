using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ServiceBookingSystem.Application.DTOs.Image;
using ServiceBookingSystem.Application.Interfaces.Infrastructure;
using ServiceBookingSystem.Infrastructure.Settings;

namespace ServiceBookingSystem.Infrastructure.FileStorage;

public class CloudinaryImageService : IImageService
{
    private readonly Cloudinary cloudinary;

    public CloudinaryImageService(IOptions<CloudinarySettings> config)
    {
        var account = new Account(
            config.Value.CloudName,
            config.Value.ApiKey,
            config.Value.ApiSecret
        );

        this.cloudinary = new Cloudinary(account);
    }

    public async Task<ImageStorageResult> AddImageAsync(IFormFile file)
    {
        var uploadResult = new ImageStorageResult();

        if (file.Length > 0)
        {
            await using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                // Standardize image size for consistency. 
                // "fill" maintains aspect ratio while filling the dimensions.
                Transformation = new Transformation().Height(500).Width(500).Crop("fill").Gravity("face")
            };

            var result = await this.cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
            {
                throw new Exception(result.Error.Message);
            }

            uploadResult = new ImageStorageResult
            {
                Url = result.SecureUrl.ToString(),
                PublicId = result.PublicId
            };
        }

        return uploadResult;
    }

    public async Task DeleteImageAsync(string publicId)
    {
        var deleteParams = new DeletionParams(publicId);
        await this.cloudinary.DestroyAsync(deleteParams);
    }
}