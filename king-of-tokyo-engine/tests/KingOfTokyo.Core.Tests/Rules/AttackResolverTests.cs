using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Rules.Attack;
using Xunit;

namespace KingOfTokyo.Core.Tests.Rules;

public sealed class AttackResolverTests
{
    [Fact]
    public void ResolveAttack_Should_TargetTokyoOccupant_WhenAttackerIsOutsideTokyo()
    {
        var attacker = new PlayerState(0, "Attacker");
        var defender = new PlayerState(1, "Defender");
        defender.SetTokyoSlot(TokyoSlot.City);

        var gameState = new GameState(
            new[] { attacker, defender, new PlayerState(2, "Other") },
            new GameOptions(3));

        var summary = new DiceResolutionSummary
        {
            AttackCount = 2
        };

        var resolver = new AttackResolver();

        var packets = resolver.ResolveAttack(gameState, attacker, summary);

        var packet = Assert.Single(packets);
        Assert.Equal(0, packet.SourcePlayerId);
        Assert.Equal(1, packet.TargetPlayerId);
        Assert.Equal(2, packet.Amount);
        Assert.True(packet.AllowsTokyoLeave);
    }

    [Fact]
    public void ResolveAttack_Should_TargetAllOutsideTokyo_WhenAttackerIsInTokyo()
    {
        var attacker = new PlayerState(0, "Attacker");
        attacker.SetTokyoSlot(TokyoSlot.City);

        var defenderA = new PlayerState(1, "Defender A");
        var defenderB = new PlayerState(2, "Defender B");

        var gameState = new GameState(
            new[] { attacker, defenderA, defenderB },
            new GameOptions(3));

        var summary = new DiceResolutionSummary
        {
            AttackCount = 1
        };

        var resolver = new AttackResolver();

        var packets = resolver.ResolveAttack(gameState, attacker, summary);

        Assert.Equal(2, packets.Count);
        Assert.All(packets, packet => Assert.False(packet.AllowsTokyoLeave));
        Assert.Contains(packets, packet => packet.TargetPlayerId == 1);
        Assert.Contains(packets, packet => packet.TargetPlayerId == 2);
    }

    [Fact]
    public void ResolveAttack_Should_ReturnNoPackets_WhenAttackCountIsZero()
    {
        var attacker = new PlayerState(0, "Attacker");
        var defender = new PlayerState(1, "Defender");

        var gameState = new GameState(
            new[] { attacker, defender, new PlayerState(2, "Other") },
            new GameOptions(3));

        var summary = new DiceResolutionSummary
        {
            AttackCount = 0
        };

        var resolver = new AttackResolver();

        var packets = resolver.ResolveAttack(gameState, attacker, summary);

        Assert.Empty(packets);
    }

    [Fact]
    public void ResolveAttack_Should_AddFireBreathingDamage_ToAttackedNeighborOnly()
    {
        var attacker = new PlayerState(1, "Attacker");
        attacker.SetTokyoSlot(TokyoSlot.City);
        attacker.AddKeepCard(new MarketCardState(
            KnownCardIds.FireBreathing,
            "Fire Breathing",
            "Neighboring attacked monsters take 1 extra damage.",
            4,
            MarketCardType.Keep));

        var neighborA = new PlayerState(0, "Neighbor A");
        var neighborB = new PlayerState(2, "Neighbor B");
        var nonNeighbor = new PlayerState(3, "Non-neighbor");

        var gameState = new GameState(
            new[] { neighborA, attacker, neighborB, nonNeighbor },
            new GameOptions(4));

        var summary = new DiceResolutionSummary
        {
            AttackCount = 2
        };

        var resolver = new AttackResolver();

        var packets = resolver.ResolveAttack(gameState, attacker, summary);

        Assert.Equal(3, packets.Count);
        Assert.Equal(3, packets.Single(packet => packet.TargetPlayerId == neighborA.PlayerId).Amount);
        Assert.Equal(3, packets.Single(packet => packet.TargetPlayerId == neighborB.PlayerId).Amount);
        Assert.Equal(2, packets.Single(packet => packet.TargetPlayerId == nonNeighbor.PlayerId).Amount);
    }

    [Fact]
    public void ResolveAttack_Should_NotCreateFireBreathingDamage_ForUnattackedNeighbor()
    {
        var attacker = new PlayerState(1, "Attacker");
        attacker.AddKeepCard(new MarketCardState(
            KnownCardIds.FireBreathing,
            "Fire Breathing",
            "Neighboring attacked monsters take 1 extra damage.",
            4,
            MarketCardType.Keep));

        var neighborA = new PlayerState(0, "Unattacked Neighbor A");
        var neighborB = new PlayerState(2, "Unattacked Neighbor B");
        var tokyoDefender = new PlayerState(3, "Tokyo Defender");
        tokyoDefender.SetTokyoSlot(TokyoSlot.City);

        var gameState = new GameState(
            new[] { neighborA, attacker, neighborB, tokyoDefender },
            new GameOptions(4));

        var summary = new DiceResolutionSummary
        {
            AttackCount = 2
        };

        var resolver = new AttackResolver();

        var packets = resolver.ResolveAttack(gameState, attacker, summary);

        var packet = Assert.Single(packets);
        Assert.Equal(tokyoDefender.PlayerId, packet.TargetPlayerId);
        Assert.Equal(2, packet.Amount);
    }
}
