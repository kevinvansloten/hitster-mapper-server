namespace hitster_mapper_server.Model
{
    public class HitsterGamesetData
    {
        public string gameset_language { get; set; }
        public string gameset_name { get; set; }
        public List<HitsterCard> cards { get; set; }
    }
}
