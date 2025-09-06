namespace hitster_mapper_server.Model
{
    public class HitsterSongCollection
    {
        public string updated_on { get; set; }

        public List<HitsterGameSet> gamesets { get; set; }
    }
}
