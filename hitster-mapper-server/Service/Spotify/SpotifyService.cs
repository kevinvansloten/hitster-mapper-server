using hitster_mapper_server.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text;

namespace hitster_mapper_server.Service.Spotify
{
    public class SpotifyService
    {
        private readonly ILogger<SpotifyService> _logger;
        private readonly string _baseUrl;
        private readonly string _clientId;
        private readonly string _clientSecret;

        public SpotifyService(ILogger<SpotifyService> logger, IOptions<ConnectionConfiguration> config)
        {
            _logger = logger;

            _clientId = config.Value.SpotifyClientID;
            _clientSecret = config.Value.SpotifyClientSecret;

            _baseUrl = "https://accounts.spotify.com/api/token";
        }

        public string GetSpotifyAccessToken()
        {
            var client_id = _clientId;
            var client_secret = _clientSecret;

            var baseurl = "https://accounts.spotify.com/api/token";

            using var httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, baseurl);
            var requestBody = new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" }
            };
            var authHeader = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{client_id}:{client_secret}"));
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);
            request.Content = new FormUrlEncodedContent(requestBody);
            var response = httpClient.SendAsync(request).Result;
            if (response.IsSuccessStatusCode)
            {
                var content = response.Content.ReadAsStringAsync().Result;
                var json = Newtonsoft.Json.Linq.JObject.Parse(content);
                var accessToken = json["access_token"]?.ToString();
                _logger.LogInformation("Successfully retrieved Spotify access token.");
                return accessToken; // Return the access token
            }
            else
            {
                _logger.LogError($"Failed to retrieve Spotify access token. Status code: {response.StatusCode}");
                return null;
            }
        }

        public (string?, string?) GetSpotifySongInformationByID(string songId)
        {
            try
            {
                var token = GetSpotifyAccessToken();
                using var httpClient = new HttpClient();
                var requestUrl = $"https://api.spotify.com/v1/tracks/{songId}";
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var response = httpClient.GetAsync(requestUrl).Result;
                if (response.IsSuccessStatusCode)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    _logger.LogInformation("Successfully retrieved song information from Spotify.");
                    var json = Newtonsoft.Json.Linq.JObject.Parse(content);
                    var artistName = json.SelectToken("$.artists[0].name")?.ToString();
                    var songName = json.SelectToken("$.name")?.ToString();

                    return (songName, artistName); // Return the JSON response or process it as needed
                }
                else
                {
                    _logger.LogError($"Failed to retrieve song information from Spotify. Status code: {response.StatusCode}");
                    return (null, null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving song information from Spotify.");
                return (null, null);
            }
        }

        public bool AddSongsToPlaylist(IEnumerable<string> spotifySongIds, string playlistID)
        {
            try
            {
                var token = GetSpotifyAccessToken();
                using var httpClient = new HttpClient();

                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                const int batchSize = 75;
                var allSongIds = spotifySongIds.ToList();

                for (int i = 0; i < allSongIds.Count; i += batchSize)
                {
                    var batch = allSongIds.Skip(i).Take(batchSize)
                        .Select(id => $"spotify:track:{id}")
                        .ToArray();

                    var requestBody = new SpotifyPlaylistUpdateRequest
                    {
                        uris = batch,
                        position = 0
                    };

                    var json = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var requestUrl = $"https://api.spotify.com/v1/playlists/{playlistID}/tracks";

                    var response = httpClient.PostAsync(requestUrl, content).Result;
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError($"Failed to add batch starting at index {i}. Status code: {response.StatusCode}");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding songs to Spotify playlist.");
                return false;
            }
        }

    }

    public class SpotifyPlaylistUpdateRequest
    {
        public string[] uris { get; set; }
        public int position { get; set; }
    }
}
