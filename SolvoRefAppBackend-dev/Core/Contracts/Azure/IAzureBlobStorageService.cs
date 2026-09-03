using Microsoft.AspNetCore.Http;

namespace Core.Contracts.Azure
{
    public interface IAzureBlobStorageService
    {
        Task<string> UploadAsync(IFormFile file, string containerName, string blobName);
        Task<(Stream Stream, string ContentType)> DownloadAsync(string blobName, string containerName);
        Task DeleteAsync(string blobName, string containerName);
    }
}
