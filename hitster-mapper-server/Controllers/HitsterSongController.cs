using hitster_mapper_server.Model;
using hitster_mapper_server.Service.Navidrome;
using hitster_mapper_server.Service.Spotify;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace hitster_mapper_server.Controllers
{
    [ApiController]
    [Route("api/")]
    public class HitsterSongController : ControllerBase
    {
        private readonly ILogger<HitsterSongController> _logger;
        private readonly NavidromeService _navidromeService;
        private readonly SpotifyService _spotifyService;
        private readonly HitsterContext _hitsterContext;

        private static HitsterSongCollection _hitsterSongCollection;

        public HitsterSongController(ILogger<HitsterSongController> logger, NavidromeService service, SpotifyService spotifyService, HitsterContext hitsterContext)
        {
            _logger = logger;
            _navidromeService = service;
            _spotifyService = spotifyService;
            _hitsterContext = hitsterContext;

            if (_hitsterSongCollection == null)
            {
                var paredResult = JsonConvert.DeserializeObject<HitsterSongCollection>(System.IO.File.ReadAllText("Resources/hitstersongs.json"));

                if (paredResult == null)
                {
                    throw new Exception("Failed to load HitsterSongCollection from JSON file.");
                }
                _hitsterSongCollection = paredResult;
            }

            _hitsterContext = hitsterContext;
        }

        [HttpGet("GetSongYoutube")]
        public HitsterItem GetSongYoutube(string originalUri)
        {
           return new HitsterItem
           {
               // Assuming HitsterItem has properties to set based on the originalUri
               // Populate the properties as needed
           };
        }

        [HttpGet("GetSongNavidrome")]
        public HitsterItem GetSongNavidrome(string originalUri)
        {
            return new HitsterItem
            {
                // Assuming HitsterItem has properties to set based on the originalUri
                // Populate the properties as needed
            };
        }

        [HttpGet("Debug")]
        public HitsterCard[] Debug(string songNumber, string sku = "aaaa0001", string gameLanguage = "Netherlands")
        {
            var result = _hitsterSongCollection.gamesets.Where(gs => gs.gameset_data.gameset_language == gameLanguage && gs.sku == sku).SelectMany(gs => gs.gameset_data.cards.Where(card => card.CardNumber == songNumber));
            
            return result.ToArray();
        }


        [HttpGet("Debug2")]
        public async Task<IActionResult> Debug2(string name, string artist, int maxBitRate = 128)
        {
            var id = await _navidromeService.SearchSongAsync(name, artist);
            if (id == null)
            {
                _logger.LogError("Failed to find song in Navidrome.");
                return NotFound($"Song not found in Navidrome. {name} - {artist}");
            }
            var file = await _navidromeService.DownloadSongAsync(id, maxBitRate);
            if (file == null) return NotFound("Failed to download song from Navidrome.");
            return File(file, "audio/mp3", "song.mp3");
        }

        [HttpGet("Debug3")]
        public string? Debug3(string songNumber, string sku = "aaaa0001", string gameLanguage = "Netherlands")
        {
            var result = _hitsterSongCollection.gamesets.Where(gs => gs.gameset_data.gameset_language == gameLanguage && gs.sku == sku).SelectMany(gs => gs.gameset_data.cards.Where(card => card.CardNumber == songNumber));
            if (!result.Any())
            {
                _logger.LogWarning($"No song found for CardNumber: {songNumber}, SKU: {sku}, Language: {gameLanguage}");
                return "No song found";
            }
            return _spotifyService.GetSpotifySongInformationByID(result.First().Spotify).Item1;
        }

        [HttpGet("DownloadSong")]
        public async Task<IActionResult> DownloadSong(string songNumber, string sku = "aaaa0001", string gameLanguage = "Netherlands")
        {
            var path = $"./cache/{sku}";
            var fileName = $"{songNumber}.mp3";
            // Check if the file exists in the cache
            if (System.IO.File.Exists(Path.Combine(path, fileName)))
            {
                var localSong = await System.IO.File.ReadAllBytesAsync(Path.Combine(path, fileName));
                return File(localSong, "audio/mp3", "song.mp3");
            }

            // First check if the ID is already known in the database
            var set = _hitsterContext.HitsterGameSet.Include(set => set.SetCards).FirstOrDefault(set => set.Sku == sku && set.Language == gameLanguage);
            if(set != null)
            {
                var card = set.SetCards.FirstOrDefault(card => card.CardNumber == songNumber);
                if(card != null && !string.IsNullOrEmpty(card.NavidromeId))
                {
                    var file = await _navidromeService.DownloadSongAsync(card.NavidromeId);
                    if (file == null) return NotFound("Failed to download song from Navidrome.");
                    return File(file, "audio/mp3", "song.mp3");
                }
            }  

            var result = _hitsterSongCollection.gamesets.Where(gs => gs.gameset_data.gameset_language == gameLanguage && gs.sku == sku).SelectMany(gs => gs.gameset_data.cards.Where(card => card.CardNumber == songNumber));
            if (!result.Any())
            {
                _logger.LogWarning($"No song found for CardNumber: {songNumber}, SKU: {sku}, Language: {gameLanguage}");
                return NotFound("Song not found in Navidrome.");
            }

            var spotifyName = _spotifyService.GetSpotifySongInformationByID(result.First().Spotify);
            return await Debug2(spotifyName.Item1, spotifyName.Item2);
        }

        [HttpGet("FillNames")]
        public async Task<IActionResult> FillNames(string language)
        {
            foreach (var set in _hitsterContext.HitsterGameSet.Where(set => set.Language == language).Include(set => set.SetCards))
            {
               foreach (var card in set.SetCards)
               {
                    var name = _spotifyService.GetSpotifySongInformationByID(card.Spotify);
                    card.ArtistName = name.Item2;
                    card.SongName = name.Item1;
                }               
            }

            await _hitsterContext.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("PreloadSongs")]
        public async Task<IActionResult> PreloadSongs()
        {
            var sets = _hitsterContext.HitsterGameSet.Where(set => set.Language == "Netherlands").Include(set => set.SetCards);
            foreach (var set in sets) {
                var cards = set.SetCards.Where(card =>  card.NavidromeId != null);
                foreach (var card in cards)
                {
                    var song = await _navidromeService.DownloadSongAsync(card.NavidromeId);

                    var path = $"./cache/{set.Sku}";
                    var fileName = $"{card.CardNumber}.mp3";
                    Directory.CreateDirectory(path);

                    try
                    {
                        using (var fs = new FileStream(Path.Combine(path, fileName), FileMode.Create, FileAccess.Write))
                        {
                            fs.Write(song, 0, song.Length);
                            Console.WriteLine("Downloaded: {0} - {1} to {2}", set.Sku, card.CardNumber, Path.Combine(path, fileName));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception caught in process: {0}", ex);
                        return BadRequest();
                    }
                }
            }

            return Ok();
        }


        [HttpGet("Debug5")]
        public async Task<IActionResult> Debug5()
        {
            return null;
            foreach (var set in _hitsterSongCollection.gamesets)
            {
                var gameSet = new Entity.HitsterGameSet()
                {
                    SetName = set.gameset_data.gameset_name,
                    Language = set.gameset_data.gameset_language,
                    SetCards = new List<Entity.HitsterCard>(),
                    Sku = set.sku
                };

                foreach(var card in set.gameset_data.cards)
                {
                    var cardEntity = new Entity.HitsterCard()
                    {
                        CardNumber = card.CardNumber,
                        Spotify = card.Spotify
                    };

                    gameSet.SetCards.Add(cardEntity);

                    _hitsterContext.HitsterCard.Add(cardEntity);
                }
                _hitsterContext.HitsterGameSet.Add(gameSet);
            }

            await _hitsterContext.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("FillSpotifyPlaylist")]
        public async Task<IActionResult> FillSpotifyPlaylist(string playlistID, string language = "Netherlands")
        {
            foreach (var set in _hitsterSongCollection.gamesets.Where(set => set.gameset_data.gameset_language == language))
            {
                var songIds = set.gameset_data.cards.Select(card => card.Spotify);
                _spotifyService.AddSongsToPlaylist(songIds, playlistID);
            }

            return Ok();
        }

        [HttpGet("formatted-ids")]
        public IActionResult GetFormattedSongIds(string language = "Netherlands")
        {
            int batchSize = 100;
            var formattedLines = new List<string>();

            foreach (var set in _hitsterSongCollection.gamesets.Where(set => set.gameset_data.gameset_language == language))
            {
                var songIds = set.gameset_data.cards.Select(card => card.Spotify);

                for (int i = 0; i < songIds.Count(); i += batchSize)
                {
                    var batch = songIds
                        .Skip(i)
                        .Take(batchSize)
                        .Select(id => $"\"spotify:track:{id}\"");

                    string line = string.Join(",", batch);
                    formattedLines.Add(line);
                }
            }

            // Combine lines separated by newline character
            string result = string.Join("\n", formattedLines);

            return Ok(result);
        }
    }
}
