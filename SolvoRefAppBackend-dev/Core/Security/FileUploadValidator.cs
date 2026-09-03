using Microsoft.AspNetCore.Http;

namespace Core.Security
{
    public static class FileUploadValidator
    {
        private const long MaxImageBytes = 100 * 1024 * 1024;

        private static readonly Dictionary<string, string[]> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = ["image/png"],
            [".apng"] = ["image/apng", "image/png"],
            [".jpg"] = ["image/jpeg"],
            [".jpeg"] = ["image/jpeg"],
            [".jpe"] = ["image/jpeg"],
            [".jfif"] = ["image/jpeg"],
            [".webp"] = ["image/webp"],
            [".gif"] = ["image/gif"],
            [".avif"] = ["image/avif"],
            [".bmp"] = ["image/bmp", "image/x-ms-bmp"],
            [".ico"] = ["image/vnd.microsoft.icon", "image/x-icon"],
            [".tif"] = ["image/tiff"],
            [".tiff"] = ["image/tiff"],
            [".svg"] = ["image/svg+xml"],
            [".heic"] = ["image/heic", "image/heif", "application/octet-stream"],
            [".heif"] = ["image/heif", "image/heic", "application/octet-stream"]
        };

        public static List<string> ValidateImage(IFormFile? file)
        {
            var errors = new List<string>();
            if (file == null || file.Length == 0)
            {
                errors.Add("File is required.");
                return errors;
            }

            if (file.Length > MaxImageBytes)
            {
                errors.Add("Image size cannot exceed 100 MB.");
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedContentTypes.ContainsKey(extension))
            {
                errors.Add("Only PNG, APNG, JPG, JPEG, JPE, JFIF, WEBP, GIF, AVIF, BMP, ICO, TIFF, SVG, HEIC and HEIF images are allowed.");
                return errors;
            }

            var contentType = file.ContentType ?? string.Empty;
            var hasCompatibleImageMime = contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
            if (!AllowedContentTypes[extension].Contains(contentType, StringComparer.OrdinalIgnoreCase) && !hasCompatibleImageMime)
            {
                errors.Add("The uploaded file content type is not allowed.");
            }

            try
            {
                using var stream = file.OpenReadStream();
                if (!MatchesKnownSignature(extension, stream))
                {
                    errors.Add("The uploaded file signature is invalid.");
                }
            }
            catch
            {
                errors.Add("The uploaded file could not be inspected.");
            }

            return errors;
        }

        private static bool MatchesKnownSignature(string extension, Stream stream)
        {
            stream.Position = 0;
            Span<byte> header = stackalloc byte[64];
            var bytesRead = stream.Read(header);

            return extension.ToLowerInvariant() switch
            {
                ".png" or ".apng" => bytesRead >= 8 && header[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
                ".jpg" or ".jpeg" or ".jpe" or ".jfif" => bytesRead >= 3 && header[..3].SequenceEqual(new byte[] { 255, 216, 255 }),
                ".webp" => bytesRead >= 12
                    && header[..4].SequenceEqual("RIFF"u8)
                    && header[8..12].SequenceEqual("WEBP"u8),
                ".gif" => bytesRead >= 6
                    && (header[..6].SequenceEqual("GIF87a"u8) || header[..6].SequenceEqual("GIF89a"u8)),
                ".avif" => HasIsoBaseMediaBrand(header[..bytesRead], "avif") || HasIsoBaseMediaBrand(header[..bytesRead], "avis"),
                ".bmp" => bytesRead >= 2 && header[..2].SequenceEqual("BM"u8),
                ".ico" => bytesRead >= 4 && header[..4].SequenceEqual(new byte[] { 0, 0, 1, 0 }),
                ".tif" or ".tiff" => bytesRead >= 4
                    && (header[..4].SequenceEqual(new byte[] { 73, 73, 42, 0 }) || header[..4].SequenceEqual(new byte[] { 77, 77, 0, 42 })),
                ".svg" => LooksLikeSafeSvg(stream),
                ".heic" => HasIsoBaseMediaBrand(header[..bytesRead], "heic")
                    || HasIsoBaseMediaBrand(header[..bytesRead], "heix")
                    || HasIsoBaseMediaBrand(header[..bytesRead], "hevc")
                    || HasIsoBaseMediaBrand(header[..bytesRead], "hevx")
                    || HasIsoBaseMediaBrand(header[..bytesRead], "mif1"),
                ".heif" => HasIsoBaseMediaBrand(header[..bytesRead], "heif")
                    || HasIsoBaseMediaBrand(header[..bytesRead], "heic")
                    || HasIsoBaseMediaBrand(header[..bytesRead], "mif1")
                    || HasIsoBaseMediaBrand(header[..bytesRead], "msf1"),
                _ => false
            };
        }

        private static bool HasIsoBaseMediaBrand(ReadOnlySpan<byte> header, string brand)
        {
            if (header.Length < 12 || !header[4..8].SequenceEqual("ftyp"u8))
            {
                return false;
            }

            var brandBytes = System.Text.Encoding.ASCII.GetBytes(brand);
            for (var index = 8; index <= header.Length - brandBytes.Length; index++)
            {
                if (header.Slice(index, brandBytes.Length).SequenceEqual(brandBytes))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool LooksLikeSafeSvg(Stream stream)
        {
            stream.Position = 0;
            using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);
            var buffer = new char[4096];
            var read = reader.Read(buffer, 0, buffer.Length);
            var content = new string(buffer, 0, read).TrimStart('\uFEFF', ' ', '\t', '\r', '\n');

            return (content.StartsWith("<svg", StringComparison.OrdinalIgnoreCase)
                    || content.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) && content.Contains("<svg", StringComparison.OrdinalIgnoreCase))
                && !content.Contains("<script", StringComparison.OrdinalIgnoreCase)
                && !content.Contains("javascript:", StringComparison.OrdinalIgnoreCase)
                && !content.Contains("onload=", StringComparison.OrdinalIgnoreCase)
                && !content.Contains("onerror=", StringComparison.OrdinalIgnoreCase)
                && !content.Contains("<foreignObject", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsSupportedImageExtension(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            return !string.IsNullOrWhiteSpace(extension) && AllowedContentTypes.ContainsKey(extension);
        }

        public static string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedContentTypes.TryGetValue(extension, out var contentTypes))
            {
                throw new InvalidOperationException("Unsupported image type.");
            }

            return contentTypes[0];
        }
    }
}
