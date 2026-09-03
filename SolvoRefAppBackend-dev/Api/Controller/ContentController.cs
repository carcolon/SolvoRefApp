using System.Net;
using Api.Models;
using Core.Contracts.Azure;
using Core.DBContext;
using Core.Models.Content;
using Core.Models.Global;
using Core.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.Extensions.Logging;

namespace Api.Controller
{
    [ApiController]
    [Route("api/content")]
    public class ContentController : ControllerBase
    {
        private static readonly HashSet<string> AllowedSections = new(StringComparer.OrdinalIgnoreCase)
        {
            "spotlight",
            "program_news",
        };

        private static readonly HashSet<string> AllowedActionTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "none",
            "modal",
            "detail",
            "route",
            "url",
        };

        private static readonly HashSet<string> AllowedBadgeVariants = new(StringComparer.OrdinalIgnoreCase)
        {
            "default",
            "mint",
            "teal",
            "orange",
            "update",
            "testimony",
            "campaign",
        };

        private readonly SolvoRefAppContext _context;
        private readonly IConfiguration _configuration;
        private readonly IAzureBlobStorageService _blobStorageService;
        private readonly ILogger<ContentController> _logger;

        public ContentController(
            SolvoRefAppContext context,
            IConfiguration configuration,
            IAzureBlobStorageService blobStorageService,
            ILogger<ContentController> logger)
        {
            _context = context;
            _configuration = configuration;
            _blobStorageService = blobStorageService;
            _logger = logger;
        }

        [HttpGet("home-cards")]
        [Produces<Response<List<HomeContentCardDto>>>]
        public async Task<ActionResult<Response<List<HomeContentCardDto>>>> GetHomeCards([FromQuery] string? section)
        {
            try
            {
                var now = DateTime.UtcNow;
                var query = _context.HomeContentCards
                    .AsNoTracking()
                    .Where(x => x.IsPublished)
                    .Where(x => !x.PublishStartUtc.HasValue || x.PublishStartUtc <= now)
                    .Where(x => !x.PublishEndUtc.HasValue || x.PublishEndUtc >= now);

                if (!string.IsNullOrWhiteSpace(section))
                {
                    query = query.Where(x => x.Section == section);
                }

                var items = await query
                    .OrderBy(x => x.DisplayOrder)
                    .ThenBy(x => x.CreatedAtUtc)
                    .Select(x => MapDto(x))
                    .ToListAsync();

                return Response<List<HomeContentCardDto>>.SuccessResponse(items, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load published Home content cards for section {Section}.", section ?? "all");
                return Response<List<HomeContentCardDto>>.ErrorResponse(["Could not load Home content cards."], HttpStatusCode.InternalServerError);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin/home-cards")]
        [Produces<Response<List<HomeContentCardDto>>>]
        public async Task<ActionResult<Response<List<HomeContentCardDto>>>> GetAdminHomeCards([FromQuery] string? section)
        {
            try
            {
                var query = _context.HomeContentCards.AsNoTracking();
                if (!string.IsNullOrWhiteSpace(section))
                {
                    query = query.Where(x => x.Section == section);
                }

                var items = await query
                    .OrderBy(x => x.Section)
                    .ThenBy(x => x.DisplayOrder)
                    .ThenBy(x => x.CreatedAtUtc)
                    .Select(x => MapDto(x))
                    .ToListAsync();

                return Response<List<HomeContentCardDto>>.SuccessResponse(items, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load admin Home content cards for section {Section}.", section ?? "all");
                return Response<List<HomeContentCardDto>>.ErrorResponse(["Could not load admin cards."], HttpStatusCode.InternalServerError);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin/home-cards/{id:guid}")]
        [Produces<Response<HomeContentCardDto>>]
        public async Task<ActionResult<Response<HomeContentCardDto>>> GetAdminHomeCard(Guid id)
        {
            var item = await _context.HomeContentCards.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (item == null)
            {
                return Response<HomeContentCardDto>.ErrorResponse(["Card not found."], HttpStatusCode.NotFound);
            }

            return Response<HomeContentCardDto>.SuccessResponse(MapDto(item), HttpStatusCode.OK);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("admin/home-cards")]
        [EnableRateLimiting("admin-content-write")]
        [Produces<Response<HomeContentCardDto>>]
        public async Task<ActionResult<Response<HomeContentCardDto>>> CreateHomeCard([FromBody] UpsertHomeContentCardRequest request)
        {
            var validationErrors = ValidateRequest(request);
            if (validationErrors.Count != 0)
            {
                return Response<HomeContentCardDto>.ErrorResponse(validationErrors, HttpStatusCode.BadRequest);
            }

            try
            {
                var entity = new HomeContentCard
                {
                    Id = Guid.NewGuid(),
                    CreatedAtUtc = DateTime.UtcNow,
                };
                ApplyRequest(entity, request);

                _context.HomeContentCards.Add(entity);
                await _context.SaveChangesAsync();

                return Response<HomeContentCardDto>.SuccessResponse(MapDto(entity), HttpStatusCode.Created);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update failed while creating Home content card.");
                return Response<HomeContentCardDto>.ErrorResponse(["Could not save the card. Review title, button text, URL/image length and publishing dates."], HttpStatusCode.BadRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Home content card.");
                return Response<HomeContentCardDto>.ErrorResponse(["Could not save card."], HttpStatusCode.InternalServerError);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("admin/home-cards/{id:guid}")]
        [EnableRateLimiting("admin-content-write")]
        [Produces<Response<HomeContentCardDto>>]
        public async Task<ActionResult<Response<HomeContentCardDto>>> UpdateHomeCard(Guid id, [FromBody] UpsertHomeContentCardRequest request)
        {
            var validationErrors = ValidateRequest(request);
            if (validationErrors.Count != 0)
            {
                return Response<HomeContentCardDto>.ErrorResponse(validationErrors, HttpStatusCode.BadRequest);
            }

            try
            {
                var entity = await _context.HomeContentCards.FirstOrDefaultAsync(x => x.Id == id);
                if (entity == null)
                {
                    return Response<HomeContentCardDto>.ErrorResponse(["Card not found."], HttpStatusCode.NotFound);
                }

                ApplyRequest(entity, request);
                await _context.SaveChangesAsync();

                return Response<HomeContentCardDto>.SuccessResponse(MapDto(entity), HttpStatusCode.OK);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update failed while updating Home content card {CardId}.", id);
                return Response<HomeContentCardDto>.ErrorResponse(["Could not save the card. Review title, button text, URL/image length and publishing dates."], HttpStatusCode.BadRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update Home content card {CardId}.", id);
                return Response<HomeContentCardDto>.ErrorResponse(["Could not update card."], HttpStatusCode.InternalServerError);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("admin/home-cards/{id:guid}")]
        [EnableRateLimiting("admin-content-write")]
        [Produces<Response<bool>>]
        public async Task<ActionResult<Response<bool>>> DeleteHomeCard(Guid id)
        {
            var entity = await _context.HomeContentCards.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
            {
                return Response<bool>.ErrorResponse(["Card not found."], HttpStatusCode.NotFound);
            }

            _context.HomeContentCards.Remove(entity);
            await _context.SaveChangesAsync();

            return Response<bool>.SuccessResponse(true, HttpStatusCode.OK);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("admin/upload-image")]
        [EnableRateLimiting("admin-content-write")]
        [Produces<Response<ContentUploadResponseDto>>]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(110_000_000)]
        public async Task<ActionResult<Response<ContentUploadResponseDto>>> UploadImage(IFormFile file)
        {
            var validationErrors = FileUploadValidator.ValidateImage(file);
            if (validationErrors.Count != 0)
            {
                return Response<ContentUploadResponseDto>.ErrorResponse(validationErrors, HttpStatusCode.BadRequest);
            }

            try
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var safeName = $"{Guid.NewGuid():N}{extension}";
                var containerName = _configuration["AzureStorageContainerName"] ?? "content-assets";
                var connectionString = _configuration.GetConnectionString("AzureStorageConnectionString");
                string url;

                if (!string.IsNullOrWhiteSpace(connectionString) && !connectionString.Contains("#{"))
                {
                    url = await _blobStorageService.UploadAsync(file, containerName, $"content/{safeName}");
                    url = NormalizeContentUrl(url, containerName);
                }
                else
                {
                    var uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "content");
                    Directory.CreateDirectory(uploadsRoot);
                    var filePath = Path.Combine(uploadsRoot, safeName);
                    await using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);
                    url = $"/uploads/content/{safeName}";
                }

                return Response<ContentUploadResponseDto>.SuccessResponse(
                    new ContentUploadResponseDto
                    {
                        FileName = safeName,
                        Url = url
                    },
                    HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload Home content image.");
                return Response<ContentUploadResponseDto>.ErrorResponse(["Could not upload image."], HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet("assets/{*blobName}")]
        public async Task<IActionResult> GetContentAsset(string blobName)
        {
            var normalizedBlobName = NormalizeAssetBlobName(blobName);
            if (string.IsNullOrWhiteSpace(normalizedBlobName))
            {
                return NotFound();
            }

            var containerName = _configuration["AzureStorageContainerName"] ?? "content-assets";
            var connectionString = _configuration.GetConnectionString("AzureStorageConnectionString");

            try
            {
                Response.Headers.CacheControl = "public,max-age=86400";

                if (!string.IsNullOrWhiteSpace(connectionString) && !connectionString.Contains("#{"))
                {
                    var (stream, contentType) = await _blobStorageService.DownloadAsync(normalizedBlobName, containerName);
                    return File(stream, contentType);
                }

                var fileName = Path.GetFileName(normalizedBlobName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "content", fileName);
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound();
                }

                return PhysicalFile(filePath, FileUploadValidator.GetContentType(filePath));
            }
            catch (FileNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load Home content asset {BlobName}.", normalizedBlobName);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        private static void ApplyRequest(HomeContentCard entity, UpsertHomeContentCardRequest request)
        {
            entity.Section = NormalizeSection(request.Section);
            entity.BadgeText = Limit(InputSanitizer.SanitizePlainText(request.BadgeText), 80);
            entity.BadgeVariant = NormalizeBadgeVariant(request.BadgeVariant);
            entity.Title = Limit(InputSanitizer.SanitizePlainText(request.Title), 200);
            entity.DescriptionHtml = InputSanitizer.SanitizeHtmlFragment(request.DescriptionHtml);
            entity.DateText = Limit(InputSanitizer.SanitizePlainText(request.DateText), 100);
            entity.ButtonText = Limit(InputSanitizer.SanitizePlainText(request.ButtonText), 80);
            entity.ActionType = NormalizeActionType(request.ActionType);
            entity.ActionValue = Limit(InputSanitizer.SanitizePlainText(request.ActionValue), 2000);
            entity.IconKey = Limit(InputSanitizer.SanitizePlainText(request.IconKey), 120);
            entity.ImageUrl = Limit(NormalizeContentUrl(InputSanitizer.SanitizePlainText(request.ImageUrl), null), 2000);
            entity.LayoutJson = string.IsNullOrWhiteSpace(request.LayoutJson) ? null : request.LayoutJson.Trim();
            entity.DetailTitle = Limit(InputSanitizer.SanitizePlainText(request.DetailTitle), 200);
            entity.DetailContentHtml = InputSanitizer.SanitizeHtmlFragment(request.DetailContentHtml);
            entity.DisplayOrder = request.DisplayOrder <= 0 ? 1 : request.DisplayOrder;
            entity.IsPublished = request.IsPublished;
            entity.PublishStartUtc = request.PublishStartUtc;
            entity.PublishEndUtc = request.PublishEndUtc;
            entity.UpdatedAtUtc = DateTime.UtcNow;
        }

        private static List<string> ValidateRequest(UpsertHomeContentCardRequest? request)
        {
            var errors = new List<string>();
            if (request == null)
            {
                errors.Add("Card payload is required.");
                return errors;
            }

            var section = NormalizeSection(request.Section);
            if (!AllowedSections.Contains(section))
            {
                errors.Add("Choose a valid card section.");
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                errors.Add("Add a title before saving the card.");
            }

            if (!string.IsNullOrWhiteSpace(request.BadgeVariant) && !AllowedBadgeVariants.Contains(request.BadgeVariant.Trim()))
            {
                errors.Add("Choose a valid badge style.");
            }

            var actionType = NormalizeActionType(request.ActionType);
            if (!AllowedActionTypes.Contains(actionType))
            {
                errors.Add("Choose a valid button behavior.");
            }

            if ((actionType == "modal" || actionType == "detail" || actionType == "route" || actionType == "url")
                && string.IsNullOrWhiteSpace(request.ButtonText))
            {
                errors.Add("Add button text for the selected CTA behavior.");
            }

            if ((actionType == "modal" || actionType == "route" || actionType == "url")
                && string.IsNullOrWhiteSpace(request.ActionValue))
            {
                errors.Add("Add a CTA destination or action value.");
            }

            if (request.PublishStartUtc.HasValue && request.PublishEndUtc.HasValue && request.PublishStartUtc > request.PublishEndUtc)
            {
                errors.Add("Publish start date must be before publish end date.");
            }

            return errors;
        }

        private static string NormalizeSection(string? value)
        {
            var section = (value ?? string.Empty).Trim();
            return AllowedSections.Contains(section) ? section : "spotlight";
        }

        private static string NormalizeActionType(string? value)
        {
            var actionType = (value ?? string.Empty).Trim();
            return AllowedActionTypes.Contains(actionType) ? actionType : "none";
        }

        private static string NormalizeBadgeVariant(string? value)
        {
            var variant = (value ?? string.Empty).Trim();
            return AllowedBadgeVariants.Contains(variant) ? variant : "default";
        }

        private static string Limit(string? value, int maxLength)
        {
            var normalized = (value ?? string.Empty).Trim();
            return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
        }

        private static HomeContentCardDto MapDto(HomeContentCard item)
        {
            return new HomeContentCardDto
            {
                Id = item.Id,
                Section = item.Section,
                BadgeText = item.BadgeText,
                BadgeVariant = item.BadgeVariant,
                Title = item.Title,
                DescriptionHtml = item.DescriptionHtml,
                DateText = item.DateText,
                ButtonText = item.ButtonText,
                ActionType = item.ActionType,
                ActionValue = item.ActionValue,
                IconKey = item.IconKey,
                ImageUrl = NormalizeContentUrl(item.ImageUrl, null),
                LayoutJson = item.LayoutJson ?? string.Empty,
                DetailTitle = item.DetailTitle,
                DetailContentHtml = item.DetailContentHtml,
                DisplayOrder = item.DisplayOrder,
                IsPublished = item.IsPublished,
                PublishStartUtc = item.PublishStartUtc,
                PublishEndUtc = item.PublishEndUtc,
                CreatedAtUtc = item.CreatedAtUtc,
                UpdatedAtUtc = item.UpdatedAtUtc
            };
        }

        private static string NormalizeContentUrl(string url, string? containerName)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return string.Empty;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return url;
            }

            if (!uri.Host.Contains(".blob.core.windows.net", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            var rawSegments = uri.AbsolutePath
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            var segments = new List<string>(rawSegments.Length);
            foreach (var segment in rawSegments)
            {
                if (segments.Count > 0 && string.Equals(segments[^1], segment, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                segments.Add(segment);
            }

            if (!string.IsNullOrWhiteSpace(containerName)
                && segments.Count >= 2
                && string.Equals(segments[0], containerName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(segments[1], containerName, StringComparison.OrdinalIgnoreCase))
            {
                segments.RemoveAt(1);
            }

            var contentIndex = segments.FindIndex(x => string.Equals(x, "content", StringComparison.OrdinalIgnoreCase));
            if (contentIndex >= 0 && contentIndex < segments.Count - 1)
            {
                return $"/api/content/assets/{string.Join('/', segments.Skip(contentIndex))}";
            }

            var normalizedPath = string.Join('/', segments);
            return $"{uri.Scheme}://{uri.Host}/{normalizedPath}{uri.Query}{uri.Fragment}";
        }

        private static string NormalizeAssetBlobName(string? blobName)
        {
            var normalized = (blobName ?? string.Empty)
                .Replace('\\', '/')
                .Trim('/')
                .Trim();

            if (string.IsNullOrWhiteSpace(normalized)
                || normalized.Contains("..", StringComparison.Ordinal)
                || !normalized.StartsWith("content/", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            var fileName = Path.GetFileName(normalized);
            if (string.IsNullOrWhiteSpace(fileName) || !FileUploadValidator.IsSupportedImageExtension(fileName))
            {
                return string.Empty;
            }

            return normalized;
        }
    }
}
