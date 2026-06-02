using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;

namespace KingOfTokyo.Core.Services;

public sealed class MarketSetupService
{
    private readonly IReadOnlyList<MarketCardState> _starterDeck;

    public MarketSetupService(IReadOnlyList<MarketCardState>? starterDeck = null)
    {
        _starterDeck = starterDeck ?? BuildDefaultDeck();
    }

    public void InitializeMarket(GameState gameState)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        gameState.Market.Initialize(_starterDeck);
    }

    private static IReadOnlyList<MarketCardState> BuildDefaultDeck()
    {
        return new List<MarketCardState>
        {
            new(KnownCardIds.GiantBrain, "Giant Brain", "You have 1 extra reroll each turn.", 5, MarketCardType.Keep),
            new(KnownCardIds.Heal, "Heal", "Heal 2 damage.", 3, MarketCardType.Discard, new CardPurchaseEffect { Heal = 2 }),
            new(KnownCardIds.ApartmentBuilding, "Apartment Building", "+3 victory points.", 5, MarketCardType.Discard, new CardPurchaseEffect { GainVictoryPoints = 3 }),
            new(KnownCardIds.SpikedTail, "Spiked Tail", "Your attacks deal 1 extra damage.", 5, MarketCardType.Keep),
            new(KnownCardIds.CornerStore, "Corner Store", "+1 victory point.", 3, MarketCardType.Discard, new CardPurchaseEffect { GainVictoryPoints = 1 }),
            new(KnownCardIds.Energize, "Energize", "+9 energy.", 8, MarketCardType.Discard, new CardPurchaseEffect { GainEnergy = 9 }),
            new(KnownCardIds.AlienMetabolism, "Alien Metabolism", "Cards cost 1 less.", 3, MarketCardType.Keep),
            new(KnownCardIds.CommuterTrain, "Commuter Train", "+2 victory points.", 4, MarketCardType.Discard, new CardPurchaseEffect { GainVictoryPoints = 2 }),
            new(KnownCardIds.Gourmet, "Gourmet", "When you score, gain 2 extra victory points.", 4, MarketCardType.Keep),
            new(KnownCardIds.Herbivore, "Herbivore", "At the end of each turn in which you dealt no damage, gain 1 victory point.", 5, MarketCardType.Keep),
            new(KnownCardIds.Regeneration, "Regeneration", "When you heal, heal 1 extra damage.", 4, MarketCardType.Keep),
            new(KnownCardIds.FriendOfChildren, "Friend of Children", "If you gain any energy, gain 1 extra energy.", 3, MarketCardType.Keep),
            new(KnownCardIds.Urbavore, "Urbavore", "When you start your turn in Tokyo, gain 1 extra victory point. When you deal any damage from Tokyo, deal 1 extra damage.", 4, MarketCardType.Keep),
            new(KnownCardIds.RapidHealing, "Rapid Healing", "Any time during your turn, spend 2 energy to heal 1 damage.", 3, MarketCardType.Keep),
            new(KnownCardIds.SolarPowered, "Solar Powered", "At the end of your turn, gain 1 energy if you have none.", 2, MarketCardType.Keep),
            new(KnownCardIds.EnergyHoarder, "Energy Hoarder", "Gain 1 victory point for every 6 energy you have at the end of your turn.", 3, MarketCardType.Keep),
            new(KnownCardIds.RootingForTheUnderdog, "Rooting for the Underdog", "At the end of your turn, if you have the fewest victory points, gain 1 victory point.", 3, MarketCardType.Keep),
            new(KnownCardIds.AlphaMonster, "Alpha Monster", "Gain 1 victory point when you attack.", 5, MarketCardType.Keep),
            new(KnownCardIds.DedicatedNewsTeam, "Dedicated News Team", "Whenever you buy a card, gain 1 victory point.", 3, MarketCardType.Keep),
            new(KnownCardIds.EaterOfTheDead, "Eater of the Dead", "Gain 3 victory points whenever any monster reaches 0 health.", 4, MarketCardType.Keep),
            new(KnownCardIds.CompleteDestruction, "Complete Destruction", "If you roll 1, 2, 3, gain 9 extra victory points.", 3, MarketCardType.Keep),
            new(KnownCardIds.PoisonQuills, "Poison Quills", "When you score 1s, also deal 2 damage.", 3, MarketCardType.Keep),
            new(KnownCardIds.AcidAttack, "Acid Attack", "Your attacks deal 1 extra damage.", 6, MarketCardType.Keep),
            new(KnownCardIds.PoisonSpit, "Poison Spit", "When you attack, give each damaged monster a poison token.", 4, MarketCardType.Keep),
            new(KnownCardIds.ShrinkRay, "Shrink Ray", "When you attack, give each damaged monster a shrink token.", 6, MarketCardType.Keep),
            new(KnownCardIds.NovaBreath, "Nova Breath", "Your attacks damage all other monsters.", 7, MarketCardType.Keep),
            new(KnownCardIds.Burrowing, "Burrowing", "When attacking Tokyo, deal 1 extra damage. When leaving Tokyo, deal 1 damage to the monster taking your place.", 5, MarketCardType.Keep),
            new(KnownCardIds.Jets, "Jets", "When you leave Tokyo after taking damage, heal that damage.", 5, MarketCardType.Keep),
            new(KnownCardIds.Wings, "Wings", "Spend 2 energy to cancel damage you took this turn.", 6, MarketCardType.Keep),
            new(KnownCardIds.Camouflage, "Camouflage", "When you take damage, roll a die for each damage. Each heart prevents 1 damage.", 3, MarketCardType.Keep),
            new(KnownCardIds.ArmorPlating, "Armor Plating", "Ignore 1 damage.", 4, MarketCardType.Keep),
            new(KnownCardIds.EvenBigger, "Even Bigger", "Your maximum health is increased by 2. When you gain this card, heal 2. When you lose this card, lose 2 health.", 8, MarketCardType.Keep, new CardPurchaseEffect { IncreaseMaxHealth = 2, Heal = 2 }),
            new(KnownCardIds.WereOnlyMakingItStronger, "We're Only Making It Stronger", "When you lose 2 or more health, gain 1 victory point.", 3, MarketCardType.Keep),
            new(KnownCardIds.NuclearPowerPlant, "Nuclear Power Plant", "+2 victory points and heal 3 damage.", 6, MarketCardType.Discard, new CardPurchaseEffect { GainVictoryPoints = 2, Heal = 3 }),
            new(KnownCardIds.DropFromHighAltitude, "Drop from High Altitude", "+2 victory points and enter Tokyo if a slot is available.", 5, MarketCardType.Discard, new CardPurchaseEffect { GainVictoryPoints = 2, EnterTokyo = true }),
            new(KnownCardIds.FireBlast, "Fire Blast", "All other monsters lose 2 health.", 3, MarketCardType.Discard, new CardPurchaseEffect { DamageAllOthers = 2 }),
            new(KnownCardIds.HighAltitudeBombing, "High Altitude Bombing", "All monsters lose 3 health.", 4, MarketCardType.Discard, new CardPurchaseEffect { DamageAllIncludingSelf = 3 }),
            new(KnownCardIds.EvacuationOrders, "Evacuation Orders", "All other monsters lose 5 health.", 7, MarketCardType.Discard, new CardPurchaseEffect { DamageAllOthers = 5 }),
            new(KnownCardIds.NationalGuard, "National Guard", "+2 victory points and suffer 2 damage.", 3, MarketCardType.Discard, new CardPurchaseEffect { GainVictoryPoints = 2, DamageSelf = 2 }),
            new(KnownCardIds.Tanks, "Tanks", "+4 victory points and suffer 3 damage.", 4, MarketCardType.Discard, new CardPurchaseEffect { GainVictoryPoints = 4, DamageSelf = 3 }),
            new(KnownCardIds.JetFighters, "Jet Fighters", "+5 victory points and suffer 4 damage.", 5, MarketCardType.Discard, new CardPurchaseEffect { GainVictoryPoints = 5, DamageSelf = 4 }),
            new(KnownCardIds.GasRefinery, "Gas Refinery", "+2 victory points and deal 3 damage to all other monsters.", 6, MarketCardType.Discard, new CardPurchaseEffect { GainVictoryPoints = 2, DamageAllOthers = 3 }),
            new(KnownCardIds.Skyscraper, "Skyscraper", "+4 victory points.", 6, MarketCardType.Discard, new CardPurchaseEffect { GainVictoryPoints = 4 }),
            new(KnownCardIds.VastStorm, "Vast Storm", "+2 victory points. All other monsters lose 1 health for every 2 energy they have.", 6, MarketCardType.Discard, new CardPurchaseEffect { GainVictoryPoints = 2, DamageOthersPerTwoEnergy = 1 }),
            new(KnownCardIds.HerdCuller, "Herd Culler", "Once per turn, you may change one die to 1.", 3, MarketCardType.Keep),
            new(KnownCardIds.MadeInALab, "Made in a Lab", "During your purchase phase, you may look at the top card of the deck and buy it.", 2, MarketCardType.Keep),
            new(KnownCardIds.Metamorph, "Metamorph", "At the end of your turn, you may discard one of your keep cards to gain energy equal to its cost.", 3, MarketCardType.Keep),
            new(KnownCardIds.PlotTwist, "Plot Twist", "Change one of your dice to any result, then discard this card.", 3, MarketCardType.Keep),
            new(KnownCardIds.ExtraHead, "Extra Head", "You have 1 extra die.", 7, MarketCardType.Keep),
            new(KnownCardIds.Telepath, "Telepath", "Spend 1 energy to gain 1 extra reroll.", 4, MarketCardType.Keep),
            new(KnownCardIds.Stretchy, "Stretchy", "Spend 2 energy to change one of your dice to any result.", 3, MarketCardType.Keep),
            new(KnownCardIds.SmokeCloud, "Smoke Cloud", "Starts with 3 charges. Spend 1 charge to gain 1 extra reroll. Discard after all charges are used.", 4, MarketCardType.Keep, counters: 3),
            new(KnownCardIds.MonsterBatteries, "Monster Batteries", "Starts with 6 stored energy. At the end of each turn, lose 2 stored energy. Discard when empty.", 5, MarketCardType.Keep, storedEnergy: 6),
            new(KnownCardIds.FreezeTime, "Freeze Time", "After a turn in which you score three 1s, take one extra turn with one fewer die.", 5, MarketCardType.Keep),
            new(KnownCardIds.Frenzy, "Frenzy", "After buying this card, immediately take one extra turn.", 7, MarketCardType.Discard),
            new(KnownCardIds.PsychicProbe, "Psychic Probe", "Once during each other player's turn, reroll one of that player's dice. If the rerolled die is energy, discard this card.", 3, MarketCardType.Keep)
        };
    }
}
