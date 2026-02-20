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
/// Service for interacting with Radarr API.
/// </summary>
public class RadarrService
{
    private readonly string _baseUrl;
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RadarrService"/> class.
    /// </summary>
    /// <param name="baseUrl">Radarr base URL.</param>
    /// <param name="apiKey">Radarr API key.</param>
    /// <param name="httpClient">HTTP client.</param>
    /// <param name="logger">Logger.</param>
    public RadarrService(string baseUrl, string apiKey, HttpClient httpClient, ILogger logger)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _apiKey = apiKey;
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Test connection to Radarr.
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
            _logger.LogError(ex, "Error testing Radarr connection");
            return false;
        }
    }

    /// <summary>
    /// Get movie by IMDB ID.
    /// </summary>
    /// <param name="imdbId">IMDB ID.</param>
    /// <returns>Movie info or null if not found.</returns>
    public async Task<RadarrMovie?> GetMovieByImdbIdAsync(string imdbId)
    {
        try
        {
            // Get all movies from Radarr
            var url = $"{_baseUrl}/api/v3/movie";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Api-Key", _apiKey);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var allMovies = JsonSerializer.Deserialize<List<RadarrMovie>>(content);

            // Find movie with matching IMDB ID
            var movie = allMovies?.FirstOrDefault(m => m.ImdbId == imdbId);
            return movie;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting movie by IMDB ID {ImdbId}", imdbId);
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
                var tags = JsonSerializer.Deserialize<List<RadarrTag>>(content);
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
                var newTag = JsonSerializer.Deserialize<RadarrTag>(createContent);
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
    /// Get all movies with a specific tag.
    /// </summary>
    /// <param name="tagId">Tag ID.</param>
    /// <returns>List of movies with the tag.</returns>
    public async Task<List<RadarrMovie>> GetMoviesWithTagAsync(int tagId)
    {
        try
        {
            var url = $"{_baseUrl}/api/v3/movie";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Api-Key", _apiKey);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return new List<RadarrMovie>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var allMovies = JsonSerializer.Deserialize<List<RadarrMovie>>(content);

            // Filter movies that have the tag
            var moviesWithTag = allMovies?
                .Where(m => m.Tags != null && m.Tags.Contains(tagId))
                .ToList() ?? new List<RadarrMovie>();

            return moviesWithTag;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting movies with tag {TagId}", tagId);
            return new List<RadarrMovie>();
        }
    }

    /// <summary>
    /// Add tag to movie.
    /// </summary>
    /// <param name="movieId">Movie ID.</param>
    /// <param name="tagId">Tag ID.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> AddTagToMovieAsync(int movieId, int tagId)
    {
        try
        {
            var getUrl = $"{_baseUrl}/api/v3/movie/{movieId}";
            var getRequest = new HttpRequestMessage(HttpMethod.Get, getUrl);
            getRequest.Headers.Add("X-Api-Key", _apiKey);
            var getResponse = await _httpClient.SendAsync(getRequest);

            if (!getResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get movie {MovieId}: {StatusCode}", movieId, getResponse.StatusCode);
                return false;
            }

            var getContent = await getResponse.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(getContent);
            var movieElement = jsonDoc.RootElement;

            // Check if tag already exists
            var tagsArray = movieElement.GetProperty("tags");
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
                _logger.LogInformation("Movie {MovieId} already has tag {TagId}", movieId, tagId);
                return true;
            }

            // Add the new tag
            tagIds.Add(tagId);

            // Send back the complete movie object with modified tags
            var updateUrl = $"{_baseUrl}/api/v3/movie";
            var movieJson = movieElement.GetRawText();

            // Modify tags in the JSON
            var movieDict = JsonSerializer.Deserialize<Dictionary<string, object>>(movieJson);
            if (movieDict == null)
            {
                _logger.LogError("Failed to deserialize movie JSON");
                return false;
            }

            movieDict["tags"] = tagIds;
            var updateBody = JsonSerializer.Serialize(movieDict);
            var updateContent = new StringContent(updateBody, System.Text.Encoding.UTF8, "application/json");
            var updateRequest = new HttpRequestMessage(HttpMethod.Put, updateUrl)
            {
                Content = updateContent
            };
            updateRequest.Headers.Add("X-Api-Key", _apiKey);
            var updateResponse = await _httpClient.SendAsync(updateRequest);

            if (!updateResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to update movie {MovieId}: {StatusCode}", movieId, updateResponse.StatusCode);
                return false;
            }

            _logger.LogInformation("Successfully added tag {TagId} to movie {MovieId}", tagId, movieId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding tag {TagId} to movie {MovieId}", tagId, movieId);
            return false;
        }
    }

    /// <summary>
    /// Remove tag from movie.
    /// </summary>
    /// <param name="movieId">Movie ID.</param>
    /// <param name="tagId">Tag ID.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> RemoveTagFromMovieAsync(int movieId, int tagId)
    {
        try
        {
            var getUrl = $"{_baseUrl}/api/v3/movie/{movieId}";
            var getRequest = new HttpRequestMessage(HttpMethod.Get, getUrl);
            getRequest.Headers.Add("X-Api-Key", _apiKey);
            var getResponse = await _httpClient.SendAsync(getRequest);

            if (!getResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get movie {MovieId}: {StatusCode}", movieId, getResponse.StatusCode);
                return false;
            }

            var getContent = await getResponse.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(getContent);
            var movieElement = jsonDoc.RootElement;

            // Get all tags and remove the target tag
            var tagsArray = movieElement.GetProperty("tags");
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
                _logger.LogInformation("Movie {MovieId} does not have tag {TagId}", movieId, tagId);
                return true;
            }

            // Send back the complete movie object with modified tags
            var updateUrl = $"{_baseUrl}/api/v3/movie";
            var movieJson = movieElement.GetRawText();

            // Modify tags in the JSON
            var movieDict = JsonSerializer.Deserialize<Dictionary<string, object>>(movieJson);
            if (movieDict == null)
            {
                _logger.LogError("Failed to deserialize movie JSON");
                return false;
            }

            movieDict["tags"] = tagIds;
            var updateBody = JsonSerializer.Serialize(movieDict);
            var updateContent = new StringContent(updateBody, System.Text.Encoding.UTF8, "application/json");
            var updateRequest = new HttpRequestMessage(HttpMethod.Put, updateUrl)
            {
                Content = updateContent
            };
            updateRequest.Headers.Add("X-Api-Key", _apiKey);
            var updateResponse = await _httpClient.SendAsync(updateRequest);

            if (!updateResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to update movie {MovieId}: {StatusCode}", movieId, updateResponse.StatusCode);
                return false;
            }

            _logger.LogInformation("Successfully removed tag {TagId} from movie {MovieId}", tagId, movieId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing tag {TagId} from movie {MovieId}", tagId, movieId);
            return false;
        }
    }
}

/// <summary>
/// Radarr movie model.
/// </summary>
public class RadarrMovie
{
    /// <summary>
    /// Gets or sets the movie ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the movie title.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the IMDB ID.
    /// </summary>
    [JsonPropertyName("imdbId")]
    public string? ImdbId { get; set; }

    /// <summary>
    /// Gets or sets the tags.
    /// </summary>
    [JsonPropertyName("tags")]
    public List<int> Tags { get; set; } = new();
}

/// <summary>
/// Radarr tag model.
/// </summary>
public class RadarrTag
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
