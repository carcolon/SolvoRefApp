using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Core.Contracts.Azure;
using Core.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Core.Service.Azure
{
    public class AzureBlobStorageService : IAzureBlobStorageService
    {
        private readonly string _azureStorageConnectionString;
        public AzureBlobStorageService(IConfiguration configuration)
        {
            _azureStorageConnectionString = configuration.GetConnectionString("AzureStorageConnectionString")
                ?? throw new ArgumentNullException("AzureStorageConnectionString is missing or null in configuration.");
        }

        public async Task<string> UploadAsync(IFormFile file, string containerName, string blobName)
        {
            var validationErrors = FileUploadValidator.ValidateImage(file);
            if (validationErrors.Count > 0)
            {
                throw new InvalidOperationException(string.Join(" ", validationErrors));
            }

            var blobContainerClient = new BlobContainerClient(_azureStorageConnectionString, containerName.ToLower());

            if (string.IsNullOrEmpty(blobName))
            {
                blobName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
            }

            var blobClient = blobContainerClient.GetBlobClient(blobName);
            var blobHttpHeader = new BlobHttpHeaders
            {
                ContentType = FileUploadValidator.GetContentType(file.FileName)
            };
            await using Stream stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = blobHttpHeader });
            return blobClient.Uri.ToString();
        }

        public async Task<(Stream Stream, string ContentType)> DownloadAsync(string blobName, string containerName)
        {
            var blobContainerClient = new BlobContainerClient(_azureStorageConnectionString, containerName.ToLower());
            var blobClient = blobContainerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync())
            {
                throw new FileNotFoundException("Blob was not found.", blobName);
            }

            var response = await blobClient.DownloadStreamingAsync();
            var contentType = response.Value.Details.ContentType;
            if (string.IsNullOrWhiteSpace(contentType))
            {
                contentType = FileUploadValidator.GetContentType(blobName);
            }

            return (response.Value.Content, contentType);
        }
        
        public async Task DeleteAsync(string blobName, string containerName)
        {
            var blobContainerClient = new BlobContainerClient(_azureStorageConnectionString, containerName.ToLower());
            var blobClient = blobContainerClient.GetBlobClient(blobName);

            await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
        }
    }
}
