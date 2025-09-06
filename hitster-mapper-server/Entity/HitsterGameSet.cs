using System.ComponentModel.DataAnnotations;

namespace hitster_mapper_server.Entity
{
    public class HitsterGameSet
    {
        [Key]
        public int Id { get; set; }
        public string Sku { get; set; }
        public string Language { get; set; }
        public string SetName { get; set; }
        public List<HitsterCard> SetCards { get; set; }
    }
}
