using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Rules.Attack;
using Xunit;

namespace KingOfTokyo.Core.Tests.Rules;

public sealed class DamagePreventionServiceTests
{
    [Fact]
    public void Resolve_Should_NotPreventDamage_WhenPlayerHasNoPreventionCards()
    {
        var target = new PlayerState(0, "Monster");
        var service = new DamagePreventionService();

        var result = service.Resolve(target, CreateDamagePacket(amount: 3));

        Assert.Equal(3, result.OriginalAmount);
        Assert.Equal(0, result.PreventedDamage);
        Assert.Equal(3, result.FinalAmount);
    }

    [Fact]
    public void Resolve_Should_PreventOneDamage_WhenPlayerHasArmorPlating()
    {
        var target = new PlayerState(0, "Monster");
        target.AddKeepCard(new MarketCardState(
            KnownCardIds.ArmorPlating,
            "Armor Plating",
            "Ignore 1 damage.",
            4,
            MarketCardType.Keep));
        var service = new DamagePreventionService();

        var result = service.Resolve(target, CreateDamagePacket(amount: 3));

        Assert.Equal(3, result.OriginalAmount);
        Assert.Equal(1, result.PreventedDamage);
        Assert.Equal(1, result.StaticPreventedDamage);
        Assert.Equal(0, result.CamouflagePreventedDamage);
        Assert.Equal(2, result.FinalAmount);
    }

    [Fact]
    public void Resolve_Should_NotPreventBelowZero_WhenPreventionExceedsDamage()
    {
        var target = new PlayerState(0, "Monster");
        target.AddKeepCard(new MarketCardState(
            KnownCardIds.ArmorPlating,
            "Armor Plating",
            "Ignore 1 damage.",
            4,
            MarketCardType.Keep));
        var service = new DamagePreventionService();

        var result = service.Resolve(target, CreateDamagePacket(amount: 1));

        Assert.Equal(1, result.OriginalAmount);
        Assert.Equal(1, result.PreventedDamage);
        Assert.Equal(1, result.StaticPreventedDamage);
        Assert.Equal(0, result.CamouflagePreventedDamage);
        Assert.Equal(0, result.FinalAmount);
    }

    [Fact]
    public void Resolve_Should_TreatNegativeDamageAsZero()
    {
        var target = new PlayerState(0, "Monster");
        var service = new DamagePreventionService();

        var result = service.Resolve(target, CreateDamagePacket(amount: -1));

        Assert.Equal(0, result.OriginalAmount);
        Assert.Equal(0, result.PreventedDamage);
        Assert.Equal(0, result.FinalAmount);
    }

    [Fact]
    public void Resolve_Should_PreventOneDamagePerHeart_WhenPlayerHasCamouflage()
    {
        var target = new PlayerState(0, "Monster");
        target.AddKeepCard(CreateKeepCard(KnownCardIds.Camouflage, "Camouflage"));
        var service = new DamagePreventionService(
            randomSource: new SequenceRandomSource(DieFace.Heart, DieFace.Attack, DieFace.Heart));

        var result = service.Resolve(target, CreateDamagePacket(amount: 3));

        Assert.Equal(3, result.OriginalAmount);
        Assert.Equal(2, result.PreventedDamage);
        Assert.Equal(0, result.StaticPreventedDamage);
        Assert.Equal(2, result.CamouflagePreventedDamage);
        Assert.Equal(1, result.FinalAmount);
    }

    [Fact]
    public void Resolve_Should_RollCamouflageOnlyForDamageRemainingAfterStaticPrevention()
    {
        var target = new PlayerState(0, "Monster");
        target.AddKeepCard(CreateKeepCard(KnownCardIds.ArmorPlating, "Armor Plating"));
        target.AddKeepCard(CreateKeepCard(KnownCardIds.Camouflage, "Camouflage"));
        var service = new DamagePreventionService(
            randomSource: new SequenceRandomSource(DieFace.Heart, DieFace.Attack));

        var result = service.Resolve(target, CreateDamagePacket(amount: 3));

        Assert.Equal(3, result.OriginalAmount);
        Assert.Equal(2, result.PreventedDamage);
        Assert.Equal(1, result.StaticPreventedDamage);
        Assert.Equal(1, result.CamouflagePreventedDamage);
        Assert.Equal(1, result.FinalAmount);
    }

    [Fact]
    public void Resolve_Should_NotRollCamouflage_WhenStaticPreventionAlreadyCanceledDamage()
    {
        var target = new PlayerState(0, "Monster");
        target.AddKeepCard(CreateKeepCard(KnownCardIds.ArmorPlating, "Armor Plating"));
        target.AddKeepCard(CreateKeepCard(KnownCardIds.Camouflage, "Camouflage"));
        var randomSource = new SequenceRandomSource();
        var service = new DamagePreventionService(randomSource: randomSource);

        var result = service.Resolve(target, CreateDamagePacket(amount: 1));

        Assert.Equal(1, result.OriginalAmount);
        Assert.Equal(1, result.PreventedDamage);
        Assert.Equal(0, result.CamouflagePreventedDamage);
        Assert.Equal(0, result.FinalAmount);
        Assert.Equal(0, randomSource.RollCount);
    }

    private static DamagePacket CreateDamagePacket(int amount)
    {
        return new DamagePacket
        {
            SourcePlayerId = 1,
            TargetPlayerId = 0,
            Amount = amount,
            DamageKind = DamageKind.Attack,
            CountsAsAttack = true,
            AllowsTokyoLeave = true
        };
    }

    private static MarketCardState CreateKeepCard(string cardId, string name)
    {
        return new MarketCardState(
            cardId,
            name,
            $"Description for {name}.",
            3,
            MarketCardType.Keep);
    }

    private sealed class SequenceRandomSource : IRandomSource
    {
        private readonly Queue<DieFace> _faces;

        public int RollCount { get; private set; }

        public SequenceRandomSource(params DieFace[] faces)
        {
            _faces = new Queue<DieFace>(faces);
        }

        public DieFace RollDieFace()
        {
            RollCount++;

            if (_faces.Count == 0)
            {
                throw new InvalidOperationException("No more queued die faces in SequenceRandomSource.");
            }

            return _faces.Dequeue();
        }
    }
}