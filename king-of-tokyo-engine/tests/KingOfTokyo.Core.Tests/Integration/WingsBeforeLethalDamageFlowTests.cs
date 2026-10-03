using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Decisions;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Events;
using KingOfTokyo.Core.Rules.Dice;
using KingOfTokyo.Core.Services;
using Xunit;

namespace KingOfTokyo.Core.Tests.Integration;

public sealed class WingsBeforeLethalDamageFlowTests
{
    [Fact]
    public void Wings_CanCancelLethalDiceAttack_WithoutProtectingLaterPurchases()
    {
        var attacker = new PlayerState(0, "Attacker");
        var defender = CreateWingsOwner(1, health: 1);
        defender.SetTokyoSlot(TokyoSlot.City);
        var game = new GameState(new[] { attacker, defender }, new GameOptions(2));
        game.Tokyo.SetCityOccupant(defender.PlayerId);
        var market = new MarketSetupService(new[]
        {
            DamagingCard(damageToOthers: 2), Filler(1), Filler(2), Filler(3)
        });
        var engine = new GameEngine(
            diceRollService: new DiceRollService(new Faces(DieFace.Attack, DieFace.One, DieFace.Two,
                DieFace.Heart, DieFace.Three, DieFace.Energy)),
            marketSetupService: market);
        Assert.True(engine.Execute(game, new InitializeGameCommand()).Success);
        Assert.True(engine.Execute(game, new BeginTurnCommand(0)).Success);
        Assert.True(engine.Execute(game, new RollDiceCommand(0)).Success);

        var window = engine.Execute(game, new FinalizeDiceCommand(0));
        Assert.True(window.Success, window.Error);
        Assert.Equal(DecisionType.RapidHealingBeforeDamage, window.PendingDecision!.DecisionType);
        Assert.Equal(defender.PlayerId, window.PendingDecision.PlayerId);
        Assert.Equal(1, defender.Health);

        var activated = engine.Execute(game, new ActivateWingsCommand(defender.PlayerId));
        Assert.True(activated.Success, activated.Error);
        Assert.Equal(0, defender.Energy);
        Assert.False(engine.Execute(game, new ActivateWingsCommand(defender.PlayerId)).Success);

        var resolved = engine.Execute(game, new ContinueAfterRapidHealingCommand(defender.PlayerId));
        Assert.True(resolved.Success, resolved.Error);
        Assert.Equal(1, defender.Health);
        Assert.Equal(TokyoSlot.City, defender.TokyoSlot);
        Assert.Null(resolved.PendingDecision);
        Assert.Contains(resolved.NewEvents, e => e is DamagePreventedEvent prevented &&
            prevented.TargetPlayerId == defender.PlayerId && prevented.Amount == 1);
        Assert.DoesNotContain(resolved.NewEvents, e => e is PlayerEliminatedEvent);

        var bought = engine.Execute(game, new BuyFaceUpCardCommand(0, 0));
        Assert.True(bought.Success, bought.Error);
        Assert.False(defender.IsAlive);
        Assert.Contains(bought.NewEvents, e => e is PlayerEliminatedEvent eliminated &&
            eliminated.EliminatedPlayerId == defender.PlayerId);
    }

    [Fact]
    public void Wings_CanCancelLethalDamageFromOwnPurchase()
    {
        var buyer = CreateWingsOwner(0, health: 2);
        buyer.GainEnergy(1);
        var other = new PlayerState(1, "Other");
        var game = new GameState(new[] { buyer, other }, new GameOptions(2));
        var card = new MarketCardState("self-damage", "Self Damage", "Deal 3 damage to self.", 1,
            MarketCardType.Discard, new CardPurchaseEffect { DamageSelf = 3 });
        var engine = new GameEngine(marketSetupService: new MarketSetupService(new[]
        {
            card, Filler(1), Filler(2), Filler(3)
        }));
        BeginPurchase(engine, game);

        var window = engine.Execute(game, new BuyFaceUpCardCommand(0, 0));
        Assert.True(window.Success, window.Error);
        Assert.Equal(buyer.PlayerId, window.PendingDecision!.PlayerId);
        Assert.Same(card, game.Market.FaceUpCards[0]);

        Assert.True(engine.Execute(game, new ActivateWingsCommand(buyer.PlayerId)).Success);
        var purchase = engine.Execute(game, new ContinueAfterRapidHealingCommand(buyer.PlayerId));
        Assert.True(purchase.Success, purchase.Error);
        Assert.Equal(2, buyer.Health);
        Assert.True(buyer.IsAlive);
        Assert.Equal(0, buyer.Energy);
        Assert.Contains(purchase.NewEvents, e => e is CardBoughtEvent bought && bought.CardId == card.CardId);
        Assert.Contains(purchase.NewEvents, e => e is DamagePreventedEvent prevented &&
            prevented.TargetPlayerId == buyer.PlayerId && prevented.Amount == 3);
    }

    [Fact]
    public void Wings_CanCancelLethalPoisonAtEndOfTurn()
    {
        var player = CreateWingsOwner(0, health: 1);
        player.Status.AddPoisonTokens(1);
        var game = new GameState(new[] { player, new PlayerState(1, "Other") }, new GameOptions(2));
        var engine = new GameEngine();
        BeginPurchase(engine, game);

        var window = engine.Execute(game, new EndTurnCommand(0));
        Assert.True(window.Success, window.Error);
        Assert.Equal(player.PlayerId, window.PendingDecision!.PlayerId);

        Assert.True(engine.Execute(game, new ActivateWingsCommand(0)).Success);
        var ended = engine.Execute(game, new ContinueAfterRapidHealingCommand(0));
        Assert.True(ended.Success, ended.Error);
        Assert.Equal(1, player.Health);
        Assert.Equal(TurnPhase.Finished, game.CurrentTurn!.Phase);
        Assert.Contains(ended.NewEvents, e => e is DamagePreventedEvent prevented &&
            prevented.TargetPlayerId == player.PlayerId && prevented.DamageKind == DamageKind.StatusEffect);
    }

    [Fact]
    public void Buyer_CannotSpendEnergyReservedForSuspendedPurchase()
    {
        var buyer = CreateWingsOwner(0, health: 1);
        buyer.GainEnergy(2);
        buyer.AddKeepCard(new MarketCardState(KnownCardIds.RapidHealing, "Rapid Healing", "Heal.", 3, MarketCardType.Keep));
        var game = new GameState(new[] { buyer, new PlayerState(1, "Other") }, new GameOptions(2));
        var card = new MarketCardState("self-damage", "Self Damage", "Deal 2 damage to self.", 2,
            MarketCardType.Discard, new CardPurchaseEffect { DamageSelf = 2 });
        var engine = new GameEngine(marketSetupService: new MarketSetupService(new[]
        {
            card, Filler(1), Filler(2), Filler(3)
        }));
        BeginPurchase(engine, game);

        Assert.True(engine.Execute(game, new BuyFaceUpCardCommand(0, 0)).Success);
        Assert.True(engine.Execute(game, new ActivateWingsCommand(0)).Success);
        Assert.False(engine.Execute(game, new ActivateRapidHealingCommand(0)).Success);
        Assert.Equal(2, buyer.Energy);

        var purchased = engine.Execute(game, new ContinueAfterRapidHealingCommand(0));
        Assert.True(purchased.Success, purchased.Error);
        Assert.Equal(0, buyer.Energy);
        Assert.True(buyer.IsAlive);
    }

    [Fact]
    public void Wings_CanCancelLethalPoisonQuillsBeforeDiceResolution()
    {
        var attacker = new PlayerState(0, "Attacker");
        attacker.AddKeepCard(new MarketCardState(KnownCardIds.PoisonQuills, "Poison Quills", "Deal 2 damage.", 3, MarketCardType.Keep));
        var defender = CreateWingsOwner(1, health: 2);
        defender.SetTokyoSlot(TokyoSlot.City);
        var game = new GameState(new[] { attacker, defender }, new GameOptions(2));
        game.Tokyo.SetCityOccupant(defender.PlayerId);
        var engine = new GameEngine(diceRollService: new DiceRollService(new Faces(
            DieFace.One, DieFace.One, DieFace.One, DieFace.Heart, DieFace.Two, DieFace.Energy)));
        Assert.True(engine.Execute(game, new InitializeGameCommand()).Success);
        Assert.True(engine.Execute(game, new BeginTurnCommand(0)).Success);
        Assert.True(engine.Execute(game, new RollDiceCommand(0)).Success);

        var window = engine.Execute(game, new FinalizeDiceCommand(0));
        Assert.True(window.Success, window.Error);
        Assert.Equal(defender.PlayerId, window.PendingDecision!.PlayerId);
        Assert.True(engine.Execute(game, new ActivateWingsCommand(defender.PlayerId)).Success);
        var resolved = engine.Execute(game, new ContinueAfterRapidHealingCommand(defender.PlayerId));
        Assert.True(resolved.Success, resolved.Error);
        Assert.Equal(2, defender.Health);
        Assert.Contains(resolved.NewEvents, e => e is DamagePreventedEvent prevented &&
            prevented.TargetPlayerId == defender.PlayerId && prevented.Amount == 2 &&
            prevented.DamageKind == DamageKind.CardEffect);
    }

    [Fact]
    public void DecliningWings_StillEliminatesTheDefender()
    {
        var attacker = new PlayerState(0, "Attacker");
        var defender = CreateWingsOwner(1, health: 1);
        defender.SetTokyoSlot(TokyoSlot.City);
        var game = new GameState(new[] { attacker, defender }, new GameOptions(2));
        game.Tokyo.SetCityOccupant(defender.PlayerId);
        var engine = new GameEngine(diceRollService: new DiceRollService(new Faces(
            DieFace.Attack, DieFace.One, DieFace.Two, DieFace.Heart, DieFace.Three, DieFace.Energy)));
        Assert.True(engine.Execute(game, new InitializeGameCommand()).Success);
        Assert.True(engine.Execute(game, new BeginTurnCommand(0)).Success);
        Assert.True(engine.Execute(game, new RollDiceCommand(0)).Success);
        Assert.True(engine.Execute(game, new FinalizeDiceCommand(0)).Success);

        var resolved = engine.Execute(game, new ContinueAfterRapidHealingCommand(defender.PlayerId));
        Assert.True(resolved.Success, resolved.Error);
        Assert.False(defender.IsAlive);
        Assert.Equal(2, defender.Energy);
        Assert.Contains(resolved.NewEvents, e => e is PlayerEliminatedEvent eliminated &&
            eliminated.EliminatedPlayerId == defender.PlayerId);
    }

    private static PlayerState CreateWingsOwner(int playerId, int health)
    {
        var player = new PlayerState(playerId, $"Wings {playerId}");
        player.TakeDamage(player.MaxHealth - health);
        player.GainEnergy(2);
        player.AddKeepCard(new MarketCardState(KnownCardIds.Wings, "Wings", "Cancel damage.", 6, MarketCardType.Keep));
        return player;
    }

    private static MarketCardState DamagingCard(int damageToOthers) => new(
        "damage-card", "Damage Card", "Damage other players.", 0,
        MarketCardType.Discard, new CardPurchaseEffect { DamageAllOthers = damageToOthers });

    private static MarketCardState Filler(int index) => new(
        $"filler-{index}", $"Filler {index}", "No effect.", 0, MarketCardType.Keep);

    private static void BeginPurchase(GameEngine engine, GameState game)
    {
        Assert.True(engine.Execute(game, new InitializeGameCommand()).Success);
        Assert.True(engine.Execute(game, new BeginTurnCommand(0)).Success);
        game.CurrentTurn!.MarkDiceResolved();
        game.CurrentTurn.SetPhase(TurnPhase.Purchase);
    }

    private sealed class Faces : IRandomSource
    {
        private readonly Queue<DieFace> _faces;
        public Faces(params DieFace[] faces) => _faces = new Queue<DieFace>(faces);
        public DieFace RollDieFace() => _faces.Dequeue();
    }
}
