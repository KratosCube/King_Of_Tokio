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

public sealed class RapidHealingBeforeDamageFlowTests
{
    [Fact]
    public void Defender_CanHealBeforeLethalAttackAndOnlyThatDefenderCanResumeDamage()
    {
        var attacker = new PlayerState(0, "Attacker");
        var defender = CreateWoundedHealer(1, health: 1);
        defender.SetTokyoSlot(TokyoSlot.City);
        var game = new GameState(new[] { attacker, defender }, new GameOptions(2));
        game.Tokyo.SetCityOccupant(defender.PlayerId);
        var engine = CreateEngine(DieFace.Attack, DieFace.One, DieFace.Two,
            DieFace.Heart, DieFace.Three, DieFace.Energy);
        StartAndRoll(engine, game);

        var invalid = engine.Execute(game, new FinalizeDiceCommand(0, heartsReservedForHealingRay: -1));
        Assert.False(invalid.Success);
        Assert.Equal(1, defender.Health);

        var window = engine.Execute(game, new FinalizeDiceCommand(0));
        Assert.True(window.Success, window.Error);
        Assert.Empty(window.NewEvents);
        Assert.Equal(DecisionType.RapidHealingBeforeDamage, window.PendingDecision!.DecisionType);
        Assert.Equal(defender.PlayerId, window.PendingDecision.PlayerId);
        Assert.Equal(1, defender.Health);

        var wrongActor = engine.Execute(game, new ContinueAfterRapidHealingCommand(attacker.PlayerId));
        Assert.False(wrongActor.Success);
        Assert.Equal(DecisionType.RapidHealingBeforeDamage, game.PendingDecision!.DecisionType);

        var healed = engine.Execute(game, new ActivateRapidHealingCommand(defender.PlayerId));
        Assert.True(healed.Success, healed.Error);
        Assert.Equal(2, defender.Health);
        Assert.Equal(DecisionType.RapidHealingBeforeDamage, game.PendingDecision!.DecisionType);

        var resumed = engine.Execute(game, new ContinueAfterRapidHealingCommand(defender.PlayerId));
        Assert.True(resumed.Success, resumed.Error);
        Assert.Equal(1, defender.Health);
        Assert.True(defender.IsAlive);
        Assert.Equal(DecisionType.LeaveTokyo, resumed.PendingDecision!.DecisionType);
        Assert.Contains(resumed.NewEvents, gameEvent => gameEvent is DamageDealtEvent damage &&
            damage.TargetPlayerId == defender.PlayerId && damage.Amount == 1);
    }

    [Fact]
    public void MultipleDefenders_RespondInTurnBeforeAnyAttackDamageIsApplied()
    {
        var attacker = new PlayerState(0, "Attacker");
        attacker.SetTokyoSlot(TokyoSlot.City);
        var first = CreateWoundedHealer(1, health: 1);
        var second = CreateWoundedHealer(2, health: 1);
        var game = new GameState(new[] { attacker, first, second }, new GameOptions(3));
        game.Tokyo.SetCityOccupant(attacker.PlayerId);
        var engine = CreateEngine(DieFace.Attack, DieFace.One, DieFace.Two,
            DieFace.Heart, DieFace.Three, DieFace.Energy);
        StartAndRoll(engine, game);

        var window = engine.Execute(game, new FinalizeDiceCommand(0));
        Assert.Equal(first.PlayerId, window.PendingDecision!.PlayerId);
        Assert.True(engine.Execute(game, new ActivateRapidHealingCommand(first.PlayerId)).Success);

        var next = engine.Execute(game, new ContinueAfterRapidHealingCommand(first.PlayerId));
        Assert.True(next.Success, next.Error);
        Assert.Equal(second.PlayerId, next.PendingDecision!.PlayerId);
        Assert.Equal(2, first.Health);
        Assert.Equal(1, second.Health);
        Assert.Empty(next.NewEvents);

        var resolved = engine.Execute(game, new ContinueAfterRapidHealingCommand(second.PlayerId));
        Assert.True(resolved.Success, resolved.Error);
        Assert.Equal(1, first.Health);
        Assert.False(second.IsAlive);
        Assert.Contains(resolved.NewEvents, gameEvent => gameEvent is PlayerEliminatedEvent eliminated &&
            eliminated.EliminatedPlayerId == second.PlayerId);
    }

    [Fact]
    public void Defender_CanHealBeforeLethalPoisonQuillsDamage()
    {
        var attacker = new PlayerState(0, "Attacker");
        attacker.AddKeepCard(new MarketCardState(KnownCardIds.PoisonQuills, "Poison Quills", "Damage when scoring ones.", 3, MarketCardType.Keep));
        var defender = CreateWoundedHealer(1, health: 2);
        defender.SetTokyoSlot(TokyoSlot.City);
        var game = new GameState(new[] { attacker, defender }, new GameOptions(2));
        game.Tokyo.SetCityOccupant(defender.PlayerId);
        var engine = CreateEngine(DieFace.One, DieFace.One, DieFace.One,
            DieFace.Heart, DieFace.Two, DieFace.Energy);
        StartAndRoll(engine, game);

        var window = engine.Execute(game, new FinalizeDiceCommand(0));
        Assert.Equal(DecisionType.RapidHealingBeforeDamage, window.PendingDecision!.DecisionType);
        Assert.True(engine.Execute(game, new ActivateRapidHealingCommand(defender.PlayerId)).Success);

        var resolved = engine.Execute(game, new ContinueAfterRapidHealingCommand(defender.PlayerId));
        Assert.True(resolved.Success, resolved.Error);
        Assert.Equal(1, defender.Health);
        Assert.True(defender.IsAlive);
        Assert.Null(resolved.PendingDecision);
        Assert.Contains(resolved.NewEvents, gameEvent => gameEvent is DamageDealtEvent damage &&
            damage.TargetPlayerId == defender.PlayerId && damage.Amount == 2);
    }

    [Fact]
    public void Attacker_CannotAlterTheRollWhileDefenderIsDeciding()
    {
        var attacker = new PlayerState(0, "Attacker");
        attacker.GainEnergy(3);
        attacker.AddKeepCard(new MarketCardState(KnownCardIds.Telepath, "Telepath", "Extra roll.", 5, MarketCardType.Keep));
        attacker.AddKeepCard(new MarketCardState(KnownCardIds.Stretchy, "Stretchy", "Change a die.", 4, MarketCardType.Keep));
        attacker.AddKeepCard(new MarketCardState(KnownCardIds.HerdCuller, "Herd Culler", "Change a die.", 3, MarketCardType.Keep));
        var defender = CreateWoundedHealer(1, health: 1);
        defender.SetTokyoSlot(TokyoSlot.City);
        var game = new GameState(new[] { attacker, defender }, new GameOptions(2));
        game.Tokyo.SetCityOccupant(defender.PlayerId);
        var engine = CreateEngine(DieFace.Attack, DieFace.One, DieFace.Two,
            DieFace.Heart, DieFace.Three, DieFace.Energy);
        StartAndRoll(engine, game);
        Assert.True(engine.Execute(game, new FinalizeDiceCommand(0)).Success);

        var facesBefore = game.CurrentTurn!.DicePool.Dice.Select(die => die.CurrentFace).ToArray();
        Assert.False(engine.Execute(game, new ActivateTelepathCommand(0)).Success);
        Assert.False(engine.Execute(game, new ActivateStretchyCommand(0, DieFace.Three, 0)).Success);
        Assert.False(engine.Execute(game, new ActivateHerdCullerCommand(0, 0)).Success);
        Assert.Equal(facesBefore, game.CurrentTurn.DicePool.Dice.Select(die => die.CurrentFace));
        Assert.Equal(3, attacker.Energy);
        Assert.Equal(DecisionType.RapidHealingBeforeDamage, game.PendingDecision!.DecisionType);
    }

    [Fact]
    public void CurrentPlayer_CanHealBeforeLethalPoisonAtEndOfTurn()
    {
        var player = CreateWoundedHealer(0, health: 1);
        player.Status.AddPoisonTokens(1);
        var other = new PlayerState(1, "Other");
        var game = new GameState(new[] { player, other }, new GameOptions(2));
        var engine = CreateEngine(DieFace.One, DieFace.Two, DieFace.Three,
            DieFace.Energy, DieFace.Energy, DieFace.Energy);
        StartAndRoll(engine, game);
        Assert.True(engine.Execute(game, new FinalizeDiceCommand(0)).Success);

        var window = engine.Execute(game, new EndTurnCommand(0));
        Assert.True(window.Success, window.Error);
        Assert.Equal(DecisionType.RapidHealingBeforeDamage, window.PendingDecision!.DecisionType);
        Assert.Equal(1, player.Health);

        Assert.True(engine.Execute(game, new ActivateRapidHealingCommand(0)).Success);
        var resolved = engine.Execute(game, new ContinueAfterRapidHealingCommand(0));
        Assert.True(resolved.Success, resolved.Error);
        Assert.Equal(1, player.Health);
        Assert.True(player.IsAlive);
        Assert.Equal(TurnPhase.Finished, game.CurrentTurn!.Phase);
        Assert.Contains(resolved.NewEvents, gameEvent => gameEvent is DamageDealtEvent damage &&
            damage.TargetPlayerId == player.PlayerId && damage.Amount == 1);
    }

    [Fact]
    public void Defender_CanHealBeforeFaceUpCardDamageAndPurchaseCompletesAfterDecision()
    {
        var (game, engine, card, defender) = CreateDamagingMarketGame();

        var window = engine.Execute(game, new BuyFaceUpCardCommand(0, 0));
        Assert.True(window.Success, window.Error);
        Assert.Equal(DecisionType.RapidHealingBeforeDamage, window.PendingDecision!.DecisionType);
        Assert.Same(card, game.Market.FaceUpCards[0]);
        Assert.Empty(window.NewEvents);

        Assert.True(engine.Execute(game, new ActivateRapidHealingCommand(defender.PlayerId)).Success);
        var purchase = engine.Execute(game, new ContinueAfterRapidHealingCommand(defender.PlayerId));
        Assert.True(purchase.Success, purchase.Error);
        Assert.Equal(1, defender.Health);
        Assert.Contains(purchase.NewEvents, gameEvent => gameEvent is CardBoughtEvent bought && bought.CardId == card.CardId);
        Assert.DoesNotContain(game.Market.FaceUpCards, faceUp => faceUp?.CardId == card.CardId);
    }

    [Fact]
    public void Defender_CanHealBeforePeekedTopCardDamage()
    {
        var buyer = new PlayerState(0, "Buyer");
        buyer.AddKeepCard(new MarketCardState(KnownCardIds.MadeInALab, "Made in a Lab", "Peek.", 2, MarketCardType.Keep));
        var defender = CreateWoundedHealer(1, health: 2);
        var card = DamagingCard();
        var game = new GameState(new[] { buyer, defender }, new GameOptions(2));
        var engine = new GameEngine(marketSetupService: new MarketSetupService(new[] { Filler(0), Filler(1), Filler(2), card }));
        BeginPurchase(engine, game);

        var peek = engine.Execute(game, new PeekTopDeckCardCommand(0));
        Assert.True(peek.Success, peek.Error);
        var window = engine.Execute(game, new BuyPeekedTopDeckCardCommand(0));
        Assert.True(window.Success, window.Error);
        Assert.Equal(DecisionType.RapidHealingBeforeDamage, window.PendingDecision!.DecisionType);

        Assert.True(engine.Execute(game, new ActivateRapidHealingCommand(defender.PlayerId)).Success);
        var purchase = engine.Execute(game, new ContinueAfterRapidHealingCommand(defender.PlayerId));
        Assert.True(purchase.Success, purchase.Error);
        Assert.Equal(1, defender.Health);
        Assert.Null(game.PendingDecision);
        Assert.Contains(purchase.NewEvents, gameEvent => gameEvent is CardBoughtEvent bought && bought.CardId == card.CardId);
    }

    [Fact]
    public void Defender_CanHealBeforeOpportunistCardDamage()
    {
        var buyer = new PlayerState(0, "Buyer");
        var opportunist = new PlayerState(1, "Opportunist");
        opportunist.AddKeepCard(new MarketCardState(KnownCardIds.Opportunist, "Opportunist", "Buy a reveal.", 3, MarketCardType.Keep));
        var defender = CreateWoundedHealer(2, health: 2);
        var card = DamagingCard();
        var game = new GameState(new[] { buyer, opportunist, defender }, new GameOptions(3));
        var engine = new GameEngine(marketSetupService: new MarketSetupService(new[] { Filler(0), Filler(1), Filler(2), card }));
        BeginPurchase(engine, game);
        Assert.True(engine.Execute(game, new BuyFaceUpCardCommand(0, 0)).Success);
        Assert.Equal(DecisionType.OpportunistPurchase, game.PendingDecision!.DecisionType);

        var window = engine.Execute(game, new BuyOpportunistRevealedCardCommand(opportunist.PlayerId));
        Assert.True(window.Success, window.Error);
        Assert.Equal(defender.PlayerId, window.PendingDecision!.PlayerId);
        Assert.Same(card, game.Market.FaceUpCards[0]);

        Assert.True(engine.Execute(game, new ActivateRapidHealingCommand(defender.PlayerId)).Success);
        var purchase = engine.Execute(game, new ContinueAfterRapidHealingCommand(defender.PlayerId));
        Assert.True(purchase.Success, purchase.Error);
        Assert.Equal(1, defender.Health);
        Assert.Null(game.PendingDecision);
        Assert.Contains(purchase.NewEvents, gameEvent => gameEvent is CardBoughtEvent bought &&
            bought.PlayerId == opportunist.PlayerId && bought.CardId == card.CardId);
    }

    private static (GameState Game, GameEngine Engine, MarketCardState Card, PlayerState Defender) CreateDamagingMarketGame()
    {
        var buyer = new PlayerState(0, "Buyer");
        var defender = CreateWoundedHealer(1, health: 2);
        var other = new PlayerState(2, "Other");
        var card = DamagingCard();
        var game = new GameState(new[] { buyer, defender, other }, new GameOptions(3));
        var engine = new GameEngine(marketSetupService: new MarketSetupService(new[] { card, Filler(0), Filler(1), Filler(2) }));
        BeginPurchase(engine, game);
        return (game, engine, card, defender);
    }

    private static MarketCardState DamagingCard() => new("test-damage-card", "Damage Card", "Deal 2 damage to others.", 0,
        MarketCardType.Discard, new CardPurchaseEffect { DamageAllOthers = 2 });

    private static MarketCardState Filler(int index) => new($"test-filler-{index}", $"Filler {index}", "No effect.", 0, MarketCardType.Keep);

    private static void BeginPurchase(GameEngine engine, GameState game)
    {
        Assert.True(engine.Execute(game, new InitializeGameCommand()).Success);
        Assert.True(engine.Execute(game, new BeginTurnCommand(0)).Success);
        game.CurrentTurn!.MarkDiceResolved();
        game.CurrentTurn.SetPhase(TurnPhase.Purchase);
    }

    private static PlayerState CreateWoundedHealer(int playerId, int health)
    {
        var player = new PlayerState(playerId, $"Defender {playerId}");
        player.TakeDamage(player.MaxHealth - health);
        player.GainEnergy(2);
        player.AddKeepCard(new MarketCardState(KnownCardIds.RapidHealing, "Rapid Healing", "Heal anytime.", 3, MarketCardType.Keep));
        return player;
    }

    private static GameEngine CreateEngine(params DieFace[] faces) =>
        new(diceRollService: new DiceRollService(new Faces(faces)));

    private static void StartAndRoll(GameEngine engine, GameState game)
    {
        Assert.True(engine.Execute(game, new InitializeGameCommand()).Success);
        Assert.True(engine.Execute(game, new BeginTurnCommand(0)).Success);
        Assert.True(engine.Execute(game, new RollDiceCommand(0)).Success);
    }

    private sealed class Faces : IRandomSource
    {
        private readonly Queue<DieFace> _faces;
        public Faces(IEnumerable<DieFace> faces) => _faces = new Queue<DieFace>(faces);
        public DieFace RollDieFace() => _faces.Dequeue();
    }
}
