using hitster_mapper_server.Configuration;
using Microsoft.Extensions.Options;

namespace hitster_mapper_server.Service.Navidrome
{
    public class NavidromeService
    {
        private readonly ILogger<NavidromeService> _logger;
        private readonly string _baseUrl;
        private readonly string NAVIDROME_USERNAME = "";
        private readonly string NAVIDROME_PASSWORD = "";

        public NavidromeService(ILogger<NavidromeService> logger, IOptions<ConnectionConfiguration> config)
        {
            _logger = logger;
            _baseUrl = config.Value.NavidromeBaseUrl;
            NAVIDROME_USERNAME = config.Value.NavidromeUsername;
            NAVIDROME_PASSWORD = config.Value.NavidromePassword;
            // Initialize the base URL for Navidrome API
        }

        public async Task<string> GetSongByUriAsync(string originalUri)
        {
            try
            {
                using var httpClient = new HttpClient();
                var requestUrl = $"{_baseUrl}/search?query={Uri.EscapeDataString(originalUri)}";
                var response = await httpClient.GetAsync(requestUrl);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Successfully retrieved song from Navidrome.");
                    return content; // Return the JSON response or process it as needed
                }
                else
                {
                    _logger.LogError($"Failed to retrieve song from Navidrome. Status code: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving song from Navidrome.");
                return null;
            }
        }

        // Method to search for a song by title and artist name
        public async Task<string> SearchSongAsync(string title, string artist)
        {
            try
            {
                var query = Uri.EscapeDataString(title + ' ' + artist);

                using var httpClient = new HttpClient();
                var requestUrl = $"{_baseUrl}/rest/search3?u={NAVIDROME_USERNAME}&p={NAVIDROME_PASSWORD}&query={query}&v=1.16.1&c=MyApp&f=json";
                var response = await httpClient.GetAsync(requestUrl);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Successfully searched for song in Navidrome.");

                    // Get the value of this path in the json $."subsonic-response"."searchResult3"."song"[0]."id" with newtonsoft.json
                    var json = Newtonsoft.Json.Linq.JObject.Parse(content);
                    var songId = json.SelectToken("$.subsonic-response.searchResult3.song[0].id")?.ToString();

                    return songId; 
                }
                else
                {
                    _logger.LogError($"Failed to search for song in Navidrome. Status code: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while searching for song in Navidrome.");
                return null;
            }
        }

        // Method to download the song by its ID in mp3 format at 64 kbps
        public async Task<byte[]> DownloadSongAsync(string songId, int maxBitRate = 128)
        {
            try
            {
                using var httpClient = new HttpClient();
                var requestUrl = $"{_baseUrl}/rest/stream?u={NAVIDROME_USERNAME}&p={NAVIDROME_PASSWORD}&id={songId}&maxBitRate={maxBitRate}&format=mp3&v=1.16.1&c=MyApp&f=json";
                var response = await httpClient.GetAsync(requestUrl);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsByteArrayAsync();
                    _logger.LogInformation("Successfully downloaded song from Navidrome.");
                    return content; // Return the byte array of the song
                }
                else
                {
                    _logger.LogError($"Failed to download song from Navidrome. Status code: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while downloading song from Navidrome.");
                return null;
            }
        }
    }
}
