using hitster_mapper_server.Entity;
using Microsoft.EntityFrameworkCore;

public class HitsterContext : DbContext
{
    public DbSet<HitsterGameSet> HitsterGameSet { get; set; }
    public DbSet<HitsterCard> HitsterCard { get; set; }

    public HitsterContext(DbContextOptions<HitsterContext> options) : base(options)
    {
        
    }

    public HitsterGameSet? GetHitsterGameSet(string sku, string language)
    {
        return HitsterGameSet
            .Include(set => set.SetCards)
            .FirstOrDefault(set => set.Sku == sku && set.Language == language);
    }

    public HitsterCard? GetHitsterGameCard(string sku, string language, string cardNumber)
    {
        return HitsterGameSet
            .Include(set => set.SetCards)
            .Where(set => set.Sku == sku && set.Language == language)
            .SelectMany(set => set.SetCards)
            .FirstOrDefault(card => card.CardNumber == cardNumber);
    }

    public async Task<bool> CreateHitsterGameCard(string cardNumber)
    {
        HitsterCard existingCard = new()
        {
            CardNumber = cardNumber,
            Spotify = "Test"
        };

        HitsterCard.Add(existingCard);
        await SaveChangesAsync();

        return true;
    }
}