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

public sealed class AcidAttackPoisonQuillsFlowTests
{
    [Fact]
    public void FinalizeDice_Should_AddAcidAttackDamageToPoisonQuills_WhenPlayerScoresOnesWithoutAttacking()
    {
        var gameState = CreateGameState(3);
        var attacker = gameState.GetCurrentPlayer();
        var defender = gameState.GetPlayerById(1);
        defender.SetTokyoSlot(TokyoSlot.City);
        gameState.Tokyo.SetCityOccupant(defender.PlayerId);
        attacker.AddKeepCard(CreateKeepCard(KnownCardIds.AcidAttack, "Acid Attack", 6));
        attacker.AddKeepCard(CreateKeepCard(KnownCardIds.PoisonQuills, "Poison Quills", 3));

        var engine = CreateEngine(
            DieFace.One,
            DieFace.One,
            DieFace.One,
            DieFace.Two,
            DieFace.Heart,
            DieFace.Energy);

        engine.Execute(gameState, new InitializeGameCommand());
        engine.Execute(gameState, new BeginTurnCommand(attacker.PlayerId));
        engine.Execute(gameState, new RollDiceCommand(attacker.PlayerId));

        var result = engine.Execute(gameState, new FinalizeDiceCommand(attacker.PlayerId));

        Assert.True(result.Success, result.Error);
        Assert.Equal(7, defender.Health);
        Assert.False(gameState.CurrentTurn!.Flags.AttackedWithDice);
        Assert.True(gameState.CurrentTurn.Flags.DealtDamage);
        Assert.Contains(result.NewEvents, e => e is DamageDealtEvent damage &&
                                             damage.SourcePlayerId == attacker.PlayerId &&
                                             damage.TargetPlayerId == defender.PlayerId &&
                                             damage.Amount == 3 &&
                                             damage.DamageKind == DamageKind.CardEffect);
    }

    private static GameState CreateGameState(int playerCount)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();

        return new GameState(players, new GameOptions(playerCount));
    }

    private static GameEngine CreateEngine(params DieFace[] faces)
    {
        return new GameEngine(diceRollService: new DiceRollService(new SequenceRandomSource(faces)));
    }

    private static MarketCardState CreateKeepCard(string cardId, string name, int cost)
    {
        return new MarketCardState(
            cardId,
            name,
            "Test keep card.",
            cost,
            MarketCardType.Keep);
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
