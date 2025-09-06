using System.ComponentModel.DataAnnotations;

namespace hitster_mapper_server.Entity
{
    public class HitsterCard
    {
        [Key]
        public int Id { get; set; }
        public string ArtistName { get; set; } = string.Empty;
        public string SongName { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public string Spotify { get; set; } = string.Empty;
        public string NavidromeId { get; set; } = string.Empty;
        public string YoutubeId { get; set; } = string.Empty;
    }
}
