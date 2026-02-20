using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.FavoriteTags.Services;

/// <summary>
/// Service for interacting with Sonarr API.
/// </summary>
public class SonarrService
{
    private readonly string _baseUrl;
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SonarrService"/> class.
    /// </summary>
    /// <param name="baseUrl">Sonarr base URL.</param>
    /// <param name="apiKey">Sonarr API key.</param>
    /// <param name="httpClient">HTTP client.</param>
    /// <param name="logger">Logger.</param>
    public SonarrService(string baseUrl, string apiKey, HttpClient httpClient, ILogger logger)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _apiKey = apiKey;
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Test connection to Sonarr.
    /// </summary>
    /// <returns>True if connection successful.</returns>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var url = $"{_baseUrl}/api/v3/system/status";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Api-Key", _apiKey);
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Sonarr connection");
            return false;
        }
    }

    /// <summary>
    /// Get series by TVDB ID.
    /// </summary>
    /// <param name="tvdbId">TVDB ID.</param>
    /// <returns>Series info or null if not found.</returns>
    public async Task<SonarrSeries?> GetSeriesByTvdbIdAsync(int tvdbId)
    {
        try
        {
            // Get all series from Sonarr
            var url = $"{_baseUrl}/api/v3/series";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Api-Key", _apiKey);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var allSeries = JsonSerializer.Deserialize<List<SonarrSeries>>(content);

            // Find series with matching TVDB ID
            var series = allSeries?.FirstOrDefault(s => s.TvdbId == tvdbId);
            return series;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting series by TVDB ID {TvdbId}", tvdbId);
            return null;
        }
    }

    /// <summary>
    /// Get tag by name, or create if not exists.
    /// </summary>
    /// <param name="tagName">Tag name.</param>
    /// <returns>Tag ID.</returns>
    public async Task<int> GetOrCreateTagAsync(string tagName)
    {
        try
        {
            // Try to get existing tag
            var url = $"{_baseUrl}/api/v3/tag";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Api-Key", _apiKey);
            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var tags = JsonSerializer.Deserialize<List<SonarrTag>>(content);
                var existingTag = tags?.Find(t => t.Label == tagName);
                if (existingTag != null)
                {
                    return existingTag.Id;
                }
            }

            // Create new tag
            var createUrl = $"{_baseUrl}/api/v3/tag";
            var createBody = new { label = tagName };
            var json = JsonSerializer.Serialize(createBody);
            var content2 = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var createRequest = new HttpRequestMessage(HttpMethod.Post, createUrl)
            {
                Content = content2
            };
            createRequest.Headers.Add("X-Api-Key", _apiKey);
            var createResponse = await _httpClient.SendAsync(createRequest);

            if (createResponse.IsSuccessStatusCode)
            {
                var createContent = await createResponse.Content.ReadAsStringAsync();
                var newTag = JsonSerializer.Deserialize<SonarrTag>(createContent);
                return newTag?.Id ?? -1;
            }

            return -1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting or creating tag {TagName}", tagName);
            return -1;
        }
    }

    /// <summary>
    /// Get all series with a specific tag.
    /// </summary>
    /// <param name="tagId">Tag ID.</param>
    /// <returns>List of series with the tag.</returns>
    public async Task<List<SonarrSeries>> GetSeriesWithTagAsync(int tagId)
    {
        try
        {
            var url = $"{_baseUrl}/api/v3/series";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Api-Key", _apiKey);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return new List<SonarrSeries>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var allSeries = JsonSerializer.Deserialize<List<SonarrSeries>>(content);

            // Filter series that have the tag
            var seriesWithTag = allSeries?
                .Where(s => s.Tags != null && s.Tags.Contains(tagId))
                .ToList() ?? new List<SonarrSeries>();

            return seriesWithTag;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting series with tag {TagId}", tagId);
            return new List<SonarrSeries>();
        }
    }

    /// <summary>
    /// Add tag to series.
    /// </summary>
    /// <param name="seriesId">Series ID.</param>
    /// <param name="tagId">Tag ID.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> AddTagToSeriesAsync(int seriesId, int tagId)
    {
        try
        {
            var getUrl = $"{_baseUrl}/api/v3/series/{seriesId}";
            var getRequest = new HttpRequestMessage(HttpMethod.Get, getUrl);
            getRequest.Headers.Add("X-Api-Key", _apiKey);
            var getResponse = await _httpClient.SendAsync(getRequest);

            if (!getResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get series {SeriesId}: {StatusCode}", seriesId, getResponse.StatusCode);
                return false;
            }

            var getContent = await getResponse.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(getContent);
            var seriesElement = jsonDoc.RootElement;

            // Check if tag already exists
            var tagsArray = seriesElement.GetProperty("tags");
            var tagIds = new List<int>();
            bool alreadyHasTag = false;
            foreach (var tag in tagsArray.EnumerateArray())
            {
                int existingTagId = tag.GetInt32();
                tagIds.Add(existingTagId);
                if (existingTagId == tagId)
                {
                    alreadyHasTag = true;
                }
            }

            if (alreadyHasTag)
            {
                _logger.LogInformation("Series {SeriesId} already has tag {TagId}", seriesId, tagId);
                return true;
            }

            // Add the new tag
            tagIds.Add(tagId);

            // Send back the complete series object with modified tags
            var updateUrl = $"{_baseUrl}/api/v3/series";
            var seriesJson = seriesElement.GetRawText();

            // Modify tags in the JSON string directly
            var seriesDict = JsonSerializer.Deserialize<Dictionary<string, object>>(seriesJson);
            if (seriesDict == null)
            {
                _logger.LogError("Failed to deserialize series JSON");
                return false;
            }

            seriesDict["tags"] = tagIds;
            var updateBody = JsonSerializer.Serialize(seriesDict);

            var updateContent = new StringContent(updateBody, System.Text.Encoding.UTF8, "application/json");
            var updateRequest = new HttpRequestMessage(HttpMethod.Put, updateUrl)
            {
                Content = updateContent
            };
            updateRequest.Headers.Add("X-Api-Key", _apiKey);
            var updateResponse = await _httpClient.SendAsync(updateRequest);

            if (!updateResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to update series {SeriesId}: {StatusCode}", seriesId, updateResponse.StatusCode);
                return false;
            }

            _logger.LogInformation("Successfully added tag {TagId} to series {SeriesId}", tagId, seriesId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding tag {TagId} to series {SeriesId}", tagId, seriesId);
            return false;
        }
    }

    /// <summary>
    /// Remove tag from series.
    /// </summary>
    /// <param name="seriesId">Series ID.</param>
    /// <param name="tagId">Tag ID.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> RemoveTagFromSeriesAsync(int seriesId, int tagId)
    {
        try
        {
            var getUrl = $"{_baseUrl}/api/v3/series/{seriesId}";
            var getRequest = new HttpRequestMessage(HttpMethod.Get, getUrl);
            getRequest.Headers.Add("X-Api-Key", _apiKey);
            var getResponse = await _httpClient.SendAsync(getRequest);

            if (!getResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get series {SeriesId}: {StatusCode}", seriesId, getResponse.StatusCode);
                return false;
            }

            var getContent = await getResponse.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(getContent);
            var seriesElement = jsonDoc.RootElement;

            // Get all tags and remove the target tag
            var tagsArray = seriesElement.GetProperty("tags");
            var tagIds = new List<int>();
            bool hadTag = false;
            foreach (var tag in tagsArray.EnumerateArray())
            {
                int existingTagId = tag.GetInt32();
                if (existingTagId != tagId)
                {
                    tagIds.Add(existingTagId);
                }
                else
                {
                    hadTag = true;
                }
            }

            if (!hadTag)
            {
                _logger.LogInformation("Series {SeriesId} does not have tag {TagId}", seriesId, tagId);
                return true;
            }

            // Send back the complete series object with modified tags
            var updateUrl = $"{_baseUrl}/api/v3/series";
            var seriesJson = seriesElement.GetRawText();

            // Modify tags in the JSON
            var seriesDict = JsonSerializer.Deserialize<Dictionary<string, object>>(seriesJson);
            if (seriesDict == null)
            {
                _logger.LogError("Failed to deserialize series JSON");
                return false;
            }

            seriesDict["tags"] = tagIds;
            var updateBody = JsonSerializer.Serialize(seriesDict);

            var updateContent = new StringContent(updateBody, System.Text.Encoding.UTF8, "application/json");
            var updateRequest = new HttpRequestMessage(HttpMethod.Put, updateUrl)
            {
                Content = updateContent
            };
            updateRequest.Headers.Add("X-Api-Key", _apiKey);
            var updateResponse = await _httpClient.SendAsync(updateRequest);

            if (!updateResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to update series {SeriesId}: {StatusCode}", seriesId, updateResponse.StatusCode);
                return false;
            }

            _logger.LogInformation("Successfully removed tag {TagId} from series {SeriesId}", tagId, seriesId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing tag {TagId} from series {SeriesId}", tagId, seriesId);
            return false;
        }
    }
}

/// <summary>
/// Sonarr series model.
/// </summary>
public class SonarrSeries
{
    /// <summary>
    /// Gets or sets the series ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the series title.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the TVDB ID.
    /// </summary>
    [JsonPropertyName("tvdbId")]
    public int? TvdbId { get; set; }

    /// <summary>
    /// Gets or sets the tags.
    /// </summary>
    [JsonPropertyName("tags")]
    public List<int> Tags { get; set; } = new();
}

/// <summary>
/// Sonarr tag model.
/// </summary>
public class SonarrTag
{
    /// <summary>
    /// Gets or sets the tag ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the tag label.
    /// </summary>
    [JsonPropertyName("label")]
    public string? Label { get; set; }
}
