using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Engine;
using KingOfTokyo.Core.Events;
using KingOfTokyo.Core.Rules.Dice;
using Xunit;

namespace KingOfTokyo.Core.Tests.Integration;

public sealed class MoreAttackRelatedKeepCardEffectsFlowTests
{
    [Fact]
    public void FinalizeDice_Should_GainNineVictoryPoints_WhenPlayerHasCompleteDestruction_AndRollsOneTwoThree()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetCurrentPlayer();

        player.AddKeepCard(new MarketCardState(
            KnownCardIds.CompleteDestruction,
            "Complete Destruction",
            "If you roll 1, 2, 3, gain 9 extra victory points.",
            3,
            MarketCardType.Keep));

        var engine = CreateEngine(
            DieFace.One, DieFace.Two, DieFace.Three,
            DieFace.Heart, DieFace.Heart, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(player.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(player.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(player.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(9, player.VictoryPoints);
        Assert.Contains(result.NewEvents, e => e is VictoryPointsGainedEvent gained &&
                                               gained.PlayerId == player.PlayerId &&
                                               gained.Amount == 9);
    }

    [Fact]
    public void FinalizeDice_Should_DealTwoCardEffectDamage_WhenPlayerHasPoisonQuills_AndScoresOnes()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetPlayerById(0);
        var defender = gameState.GetPlayerById(1);

        attacker.AddKeepCard(new MarketCardState(
            KnownCardIds.PoisonQuills,
            "Poison Quills",
            "When you score 1s, also deal 2 damage.",
            3,
            MarketCardType.Keep));

        defender.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(defender.PlayerId);

        var engine = CreateEngine(
            DieFace.One, DieFace.One, DieFace.One,
            DieFace.Heart, DieFace.Heart, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(1, attacker.VictoryPoints);
        Assert.Equal(8, defender.Health);
        Assert.Null(result.PendingDecision);
        Assert.Contains(result.NewEvents, e => e is DamageDealtEvent damage &&
                                               damage.SourcePlayerId == attacker.PlayerId &&
                                               damage.TargetPlayerId == defender.PlayerId &&
                                               damage.Amount == 2 &&
                                               damage.DamageKind == DamageKind.CardEffect);
    }

    [Fact]
    public void FinalizeDice_Should_AddPoisonTokenToAttackTarget_WhenPlayerHasPoisonSpit()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetPlayerById(0);
        var defender = gameState.GetPlayerById(1);

        attacker.AddKeepCard(new MarketCardState(
            KnownCardIds.PoisonSpit,
            "Poison Spit",
            "When you attack, give each damaged monster a poison token.",
            4,
            MarketCardType.Keep));

        defender.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(defender.PlayerId);

        var engine = CreateEngine(
            DieFace.Attack, DieFace.Heart, DieFace.One,
            DieFace.Two, DieFace.Three, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(1, defender.Status.PoisonTokens);
        Assert.Contains(result.NewEvents, e => e is StatusTokensAddedEvent added &&
                                               added.SourcePlayerId == attacker.PlayerId &&
                                               added.TargetPlayerId == defender.PlayerId &&
                                               added.PoisonTokensAdded == 1 &&
                                               added.ShrinkTokensAdded == 0);
    }

    [Fact]
    public void FinalizeDice_Should_AddShrinkTokenToAttackTarget_WhenPlayerHasShrinkRay()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetPlayerById(0);
        var defender = gameState.GetPlayerById(1);

        attacker.AddKeepCard(new MarketCardState(
            KnownCardIds.ShrinkRay,
            "Shrink Ray",
            "When you attack, give each damaged monster a shrink token.",
            6,
            MarketCardType.Keep));

        defender.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(defender.PlayerId);

        var engine = CreateEngine(
            DieFace.Attack, DieFace.Heart, DieFace.One,
            DieFace.Two, DieFace.Three, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(1, defender.Status.ShrinkTokens);
        Assert.Contains(result.NewEvents, e => e is StatusTokensAddedEvent added &&
                                               added.SourcePlayerId == attacker.PlayerId &&
                                               added.TargetPlayerId == defender.PlayerId &&
                                               added.PoisonTokensAdded == 0 &&
                                               added.ShrinkTokensAdded == 1);
    }

    [Fact]
    public void FinalizeDice_Should_DamageAllOtherMonsters_WhenPlayerHasNovaBreath()
    {
        var gameState = CreateGameState(4);
        var attacker = gameState.GetPlayerById(0);
        var tokyoDefender = gameState.GetPlayerById(1);
        var outsideDefender = gameState.GetPlayerById(2);
        var otherOutsideDefender = gameState.GetPlayerById(3);

        attacker.AddKeepCard(new MarketCardState(
            KnownCardIds.NovaBreath,
            "Nova Breath",
            "Your attacks damage all other monsters.",
            7,
            MarketCardType.Keep));

        tokyoDefender.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(tokyoDefender.PlayerId);

        var engine = CreateEngine(
            DieFace.Attack, DieFace.Heart, DieFace.One,
            DieFace.Two, DieFace.Three, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(9, tokyoDefender.Health);
        Assert.Equal(9, outsideDefender.Health);
        Assert.Equal(9, otherOutsideDefender.Health);
        Assert.Equal(3, result.NewEvents.OfType<DamageDealtEvent>().Count(e => e.DamageKind == DamageKind.Attack));
    }

    [Fact]
    public void FinalizeDice_Should_DealOneExtraAttackDamage_WhenPlayerHasAcidAttack()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetPlayerById(0);
        var defender = gameState.GetPlayerById(1);

        attacker.AddKeepCard(new MarketCardState(
            KnownCardIds.AcidAttack,
            "Acid Attack",
            "Your attacks deal 1 extra damage.",
            6,
            MarketCardType.Keep));

        defender.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(defender.PlayerId);

        var engine = CreateEngine(
            DieFace.Attack, DieFace.Heart, DieFace.One,
            DieFace.Two, DieFace.Three, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(8, defender.Health);
        Assert.Contains(result.NewEvents, e => e is DamageDealtEvent damage &&
                                               damage.SourcePlayerId == attacker.PlayerId &&
                                               damage.TargetPlayerId == defender.PlayerId &&
                                               damage.Amount == 2 &&
                                               damage.DamageKind == DamageKind.Attack);
    }

    [Fact]
    public void FinalizeDice_Should_GainEnergyToDamagedDefender_WhenDefenderHasStronger()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetPlayerById(0);
        var defender = gameState.GetPlayerById(1);

        defender.AddKeepCard(new MarketCardState(
            KnownCardIds.WereOnlyMakingItStronger,
            "We're Only Making It Stronger",
            "When damaged enough, gain 1 energy.",
            3,
            MarketCardType.Keep));

        defender.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(defender.PlayerId);

        var engine = CreateEngine(
            DieFace.Attack, DieFace.Attack, DieFace.One,
            DieFace.Two, DieFace.Three, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(8, defender.Health);
        Assert.Equal(1, defender.Energy);
        Assert.Equal(0, defender.VictoryPoints);
    }

    [Fact]
    public void FinalizeDice_Should_DealOneExtraAttackDamageIntoTokyo_WhenPlayerHasBurrowing()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetPlayerById(0);
        var defender = gameState.GetPlayerById(1);

        attacker.AddKeepCard(new MarketCardState(
            KnownCardIds.Burrowing,
            "Burrowing",
            "When attacking Tokyo, deal 1 extra damage. When leaving Tokyo, deal 1 damage to the monster taking your place.",
            5,
            MarketCardType.Keep));

        defender.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(defender.PlayerId);

        var engine = CreateEngine(
            DieFace.Attack, DieFace.Heart, DieFace.One,
            DieFace.Two, DieFace.Three, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(8, defender.Health);
        Assert.NotNull(result.PendingDecision);
    }

    [Fact]
    public void ChooseLeaveTokyo_Should_DealOneDamageToNewOccupant_WhenDefenderHasBurrowing()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetPlayerById(0);
        var defender = gameState.GetPlayerById(1);

        defender.AddKeepCard(new MarketCardState(
            KnownCardIds.Burrowing,
            "Burrowing",
            "When attacking Tokyo, deal 1 extra damage. When leaving Tokyo, deal 1 damage to the monster taking your place.",
            5,
            MarketCardType.Keep));

        defender.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(defender.PlayerId);

        var engine = CreateEngine(
            DieFace.Attack, DieFace.Heart, DieFace.One,
            DieFace.Two, DieFace.Three, DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));
        engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new ChooseLeaveTokyoCommand(true, defender.PlayerId));

        Assert.True(result.Success);
        Assert.Equal(TokyoSlot.City, attacker.TokyoSlot);
        Assert.Equal(9, attacker.Health);
        Assert.Contains(result.NewEvents, e => e is DamageDealtEvent damage &&
                                               damage.SourcePlayerId == defender.PlayerId &&
                                               damage.TargetPlayerId == attacker.PlayerId &&
                                               damage.Amount == 1 &&
                                               damage.DamageKind == DamageKind.CardEffect);
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
    }

    private static GameEngine CreateEngine(params DieFace[] sequence)
    {
        return new GameEngine(
            diceRollService: new DiceRollService(new SequenceRandomSource(sequence)));
    }

    private sealed class SequenceRandomSource : IRandomSource
    {
        private readonly Queue<DieFace> _faces;

        public SequenceRandomSource(IEnumerable<DieFace> faces)
        {
            _faces = new Queue<DieFace>(faces);
        }

        public DieFace RollDieFace()
        {
            if (_faces.Count == 0)
            {
                throw new InvalidOperationException("No more queued die faces in SequenceRandomSource.");
            }

            return _faces.Dequeue();
        }
    }
}
