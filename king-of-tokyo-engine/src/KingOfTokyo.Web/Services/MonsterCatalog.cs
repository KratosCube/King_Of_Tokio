namespace KingOfTokyo.Web.Services;

public sealed record MonsterOption(
    string MonsterId,
    string MonsterName,
    string AvatarId,
    string Emoji,
    string Accent,
    string Tagline);

public static class MonsterCatalog
{
    public static IReadOnlyList<MonsterOption> Monsters { get; } = new[]
    {
        new MonsterOption("gigasaur", "Gigasaur", "avatar-roar", "🦖", "#f97316", "Classic city-stomping kaiju."),
        new MonsterOption("cyber-kitty", "Cyber Kitty", "avatar-neon", "🐱", "#22d3ee", "Fast, shiny, and suspiciously cute."),
        new MonsterOption("the-king", "The King", "avatar-crown", "🦍", "#a78bfa", "Big fists. Bigger ego."),
        new MonsterOption("meka-dragon", "Meka Dragon", "avatar-steel", "🐉", "#94a3b8", "Metal wings over Tokyo."),
        new MonsterOption("alienoid", "Alienoid", "avatar-ufo", "👽", "#84cc16", "Not from around here."),
        new MonsterOption("space-penguin", "Space Penguin", "avatar-orbit", "🐧", "#38bdf8", "Cold stare. Cosmic rage.")
    };

    public static MonsterOption Default => Monsters[0];

    public static MonsterOption Find(string? monsterId)
    {
        return Monsters.FirstOrDefault(monster => monster.MonsterId == monsterId) ?? Default;
    }

    public static MonsterOption FindByName(string? monsterName)
    {
        return Monsters.FirstOrDefault(monster => string.Equals(monster.MonsterName, monsterName, StringComparison.OrdinalIgnoreCase))
            ?? Default;
    }
}
