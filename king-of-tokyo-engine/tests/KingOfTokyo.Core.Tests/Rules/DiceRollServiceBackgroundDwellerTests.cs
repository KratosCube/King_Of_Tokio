using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Rules.Dice;
using Xunit;

namespace KingOfTokyo.Core.Tests.Rules;

public sealed class DiceRollServiceBackgroundDwellerTests
{
    [Fact]
    public void RollAll_Should_LeaveThreesForPlayerToChoose_WhenPlayerHasBackgroundDweller()
    {
        var player = new PlayerState(0, "Monster");
        player.AddKeepCard(CreateBackgroundDweller());
        var dicePool = new DicePoolState(6);
        var service = new DiceRollService(new SequenceRandomSource(
            DieFace.Three, DieFace.Attack, DieFace.Three,
            DieFace.Heart, DieFace.Three, DieFace.Energy,
            DieFace.Three, DieFace.One, DieFace.Two, DieFace.Attack));

        service.RollAll(dicePool, player);

        Assert.Contains(dicePool.Dice, die => die.CurrentFace == DieFace.Three);
        Assert.Equal(new[]
        {
            DieFace.Three,
            DieFace.Attack,
            DieFace.Three,
            DieFace.Heart,
            DieFace.Three,
            DieFace.Energy
        }, dicePool.Dice.Select(die => die.CurrentFace));
    }

    [Fact]
    public void RollAll_Should_KeepThrees_WhenPlayerDoesNotHaveBackgroundDweller()
    {
        var player = new PlayerState(0, "Monster");
        var dicePool = new DicePoolState(6);
        var service = new DiceRollService(new SequenceRandomSource(
            DieFace.Three, DieFace.Attack, DieFace.Three,
            DieFace.Heart, DieFace.Three, DieFace.Energy));

        service.RollAll(dicePool, player);

        Assert.Contains(dicePool.Dice, die => die.CurrentFace == DieFace.Three);
    }

    [Fact]
    public void RerollBackgroundDwellerThrees_Should_RerollOnlySelectedThrees()
    {
        var player = new PlayerState(0, "Monster");
        player.AddKeepCard(CreateBackgroundDweller());
        var dicePool = new DicePoolState(6);
        var service = new DiceRollService(new SequenceRandomSource(
            DieFace.One, DieFace.Two, DieFace.Attack,
            DieFace.Heart, DieFace.Energy, DieFace.Attack,
            DieFace.Three, DieFace.Three,
            DieFace.Three, DieFace.Heart, DieFace.Attack));
        service.RollAll(dicePool, player: null);

        service.RerollSelected(dicePool, new[] { 0, 1 }, player);
        service.RerollBackgroundDwellerThrees(dicePool, new[] { 0 });

        Assert.Equal(DieFace.Three, dicePool.Dice[0].CurrentFace);
        Assert.Equal(DieFace.Three, dicePool.Dice[1].CurrentFace);
        Assert.Throws<InvalidOperationException>(() => service.RerollBackgroundDwellerThrees(dicePool, new[] { 2 }));
    }

    private static MarketCardState CreateBackgroundDweller()
    {
        return new MarketCardState(
            KnownCardIds.BackgroundDweller,
            "Background Dweller",
            "Whenever you roll any 3s, reroll them until none remain.",
            4,
            MarketCardType.Keep);
    }

    private sealed class SequenceRandomSource : IRandomSource
    {
        private readonly Queue<DieFace> _faces;

        public SequenceRandomSource(params DieFace[] faces)
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
