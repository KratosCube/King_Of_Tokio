using KingOfTokyo.Core.Commands;
using KingOfTokyo.Core.Decisions;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;
using KingOfTokyo.Core.Domain.ValueObjects;
using KingOfTokyo.Core.Dto;
using KingOfTokyo.Core.Engine;
using Xunit;

namespace KingOfTokyo.Core.Tests.Dto;

public sealed class GameStateDtoMapperTests
{
    [Fact]
    public void ToDto_Should_MapSetupGameState()
    {
        var gameId = Guid.NewGuid();
        var gameState = CreateGameState(4, gameId);

        var dto = gameState.ToDto();

        Assert.Equal(gameId, dto.GameId);
        Assert.Equal(0, dto.Version);
        Assert.Equal(GameStatus.Setup, dto.Status);
        Assert.Equal(0, dto.CurrentPlayerIndex);
        Assert.Null(dto.WinnerPlayerId);
        Assert.Null(dto.WinnerReason);
        Assert.Equal(4, dto.Players.Count);
        Assert.False(dto.Tokyo.BayEnabled);
        Assert.True(dto.Tokyo.IsEmpty);
        Assert.Equal(3, dto.Market.FaceUpCards.Count);
        Assert.Null(dto.CurrentTurn);
        Assert.Null(dto.PendingDecision);
    }

    [Fact]
    public void ToDto_Should_MapRunningGameStateAfterBeginTurn()
    {
        var gameState = CreateGameState(4);
        var gameEngine = new GameEngine();

        gameEngine.Execute(gameState, new InitializeGameCommand());
        var result = gameEngine.Execute(gameState, new BeginTurnCommand(0));

        var dto = gameState.ToDto();

        Assert.True(result.Success);
        Assert.Equal(2, dto.Version);
        Assert.Equal(GameStatus.Running, dto.Status);
        Assert.NotNull(dto.CurrentTurn);
        Assert.Equal(0, dto.CurrentTurn!.CurrentPlayerId);
        Assert.Equal(TurnPhase.Rolling, dto.CurrentTurn.Phase);
        Assert.Equal(6, dto.CurrentTurn.DiceCount);
        Assert.Equal(6, dto.CurrentTurn.Dice.Count);
        Assert.Single(gameState.EventLog);
    }

    [Fact]
    public void ToDto_Should_MapPendingDecision()
    {
        var gameState = CreateGameState(4);
        var gameEngine = new GameEngine();

        gameEngine.Execute(gameState, new InitializeGameCommand());
        gameEngine.Execute(gameState, new BeginTurnCommand(0));
        var result = gameEngine.Execute(gameState, new RollDiceCommand(0));

        var dto = gameState.ToDto();

        Assert.True(result.Success);
        Assert.NotNull(dto.PendingDecision);
        Assert.Equal(DecisionType.SelectDiceToReroll, dto.PendingDecision!.DecisionType);
        Assert.Equal(0, dto.PendingDecision.PlayerId);

        var payload = Assert.IsType<RerollDecisionData>(dto.PendingDecision.Payload);
        Assert.Equal(1, payload.RollCountUsed);
        Assert.Equal(3, payload.MaxRolls);
        Assert.Equal(6, payload.CurrentFaces.Count);
    }

    [Fact]
    public void ToDto_Should_MapOpportunistPurchasePendingDecision()
    {
        var gameState = CreateGameState(4);
        gameState.SetPendingDecision(new PendingDecision
        {
            DecisionType = DecisionType.OpportunistPurchase,
            PlayerId = 2,
            Payload = new MarketCardRevealDecisionData
            {
                SlotIndex = 1,
                CardId = "card-revealed",
                CardName = "Revealed Card",
                Cost = 5,
                EligiblePlayerIds = new[] { 2, 3 }
            }
        });

        var dto = gameState.ToDto();

        Assert.NotNull(dto.PendingDecision);
        Assert.Equal(DecisionType.OpportunistPurchase, dto.PendingDecision!.DecisionType);
        Assert.Equal(2, dto.PendingDecision.PlayerId);

        var payload = Assert.IsType<MarketCardRevealDecisionData>(dto.PendingDecision.Payload);
        Assert.Equal(1, payload.SlotIndex);
        Assert.Equal("card-revealed", payload.CardId);
        Assert.Equal("Revealed Card", payload.CardName);
        Assert.Equal(5, payload.Cost);
        Assert.Equal(new[] { 2, 3 }, payload.EligiblePlayerIds);
    }

    [Fact]
    public void ToDto_Should_MapPlayerKeepCards()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetPlayerById(0);
        player.AddKeepCard(new MarketCardState(
            KnownCardIds.ExtraHead,
            "Extra Head",
            "You have 1 extra die.",
            7,
            MarketCardType.Keep));

        var dto = gameState.ToDto();

        Assert.Single(dto.Players[0].KeepCards);
        Assert.Equal(KnownCardIds.ExtraHead, dto.Players[0].KeepCards[0].CardId);
        Assert.Equal(0, dto.Players[0].KeepCards[0].Counters);
        Assert.Equal(0, dto.Players[0].KeepCards[0].StoredEnergy);
    }

    [Fact]
    public void ToDto_Should_MapPlayerKeepCardLocalState()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetPlayerById(0);
        player.AddKeepCard(new MarketCardState(
            "card-stateful",
            "Stateful Card",
            "Uses counters and stored energy.",
            4,
            MarketCardType.Keep,
            counters: 2,
            storedEnergy: 5));

        var dto = gameState.ToDto();

        var cardDto = Assert.Single(dto.Players[0].KeepCards);
        Assert.Equal("card-stateful", cardDto.CardId);
        Assert.Equal(2, cardDto.Counters);
        Assert.Equal(5, cardDto.StoredEnergy);
    }

    [Fact]
    public void ToDto_Should_MapPlayerStatusTokens()
    {
        var gameState = CreateGameState(4);
        var player = gameState.GetPlayerById(0);
        player.Status.AddPoisonTokens(2);
        player.Status.AddShrinkTokens(1);

        var dto = gameState.ToDto();

        Assert.Equal(2, dto.Players[0].Status.PoisonTokens);
        Assert.Equal(1, dto.Players[0].Status.ShrinkTokens);
    }

    [Fact]
    public void ToDto_Should_MapTokyoSlotsAndPlayerStats()
    {
        var gameState = CreateGameState(5);
        var cityPlayer = gameState.GetPlayerById(0);
        var bayPlayer = gameState.GetPlayerById(1);

        cityPlayer.IncreaseMaxHealth(2);
        cityPlayer.TakeDamage(3);
        cityPlayer.GainVictoryPoints(5);
        cityPlayer.GainEnergy(4);
        cityPlayer.SetTokyoSlot(TokyoSlot.City);
        bayPlayer.SetTokyoSlot(TokyoSlot.Bay);
        gameState.Tokyo.SetCityOccupant(cityPlayer.PlayerId);
        gameState.Tokyo.SetBayOccupant(bayPlayer.PlayerId);

        var dto = gameState.ToDto();

        Assert.True(dto.Tokyo.BayEnabled);
        Assert.False(dto.Tokyo.IsEmpty);
        Assert.Equal(cityPlayer.PlayerId, dto.Tokyo.CityOccupantId);
        Assert.Equal(bayPlayer.PlayerId, dto.Tokyo.BayOccupantId);

        var cityPlayerDto = dto.Players.Single(player => player.PlayerId == cityPlayer.PlayerId);
        Assert.Equal("Monster 1", cityPlayerDto.MonsterName);
        Assert.Equal(7, cityPlayerDto.Health);
        Assert.Equal(12, cityPlayerDto.MaxHealth);
        Assert.Equal(5, cityPlayerDto.VictoryPoints);
        Assert.Equal(4, cityPlayerDto.Energy);
        Assert.Equal(TokyoSlot.City, cityPlayerDto.TokyoSlot);
        Assert.True(cityPlayerDto.IsAlive);

        var bayPlayerDto = dto.Players.Single(player => player.PlayerId == bayPlayer.PlayerId);
        Assert.Equal(TokyoSlot.Bay, bayPlayerDto.TokyoSlot);
    }

    [Fact]
    public void ToDto_Should_MapMarketCardsAndPileCounts()
    {
        var gameState = CreateGameState(4);
        gameState.Market.Initialize(new[]
        {
            CreateCard("test-card-1", "Test Card 1", 3, MarketCardType.Keep),
            CreateCard("test-card-2", "Test Card 2", 4, MarketCardType.Discard)
        });
        gameState.Market.Discard(CreateCard("discarded-card", "Discarded Card", 5, MarketCardType.Discard));

        var dto = gameState.ToDto();

        Assert.Equal(3, dto.Market.FaceUpCards.Count);
        Assert.Equal(0, dto.Market.DrawPileCount);
        Assert.Equal(1, dto.Market.DiscardPileCount);

        Assert.Equal(new CardDto("test-card-1", "Test Card 1", "Description for Test Card 1.", 3, MarketCardType.Keep), dto.Market.FaceUpCards[0]);
        Assert.Equal(new CardDto("test-card-2", "Test Card 2", "Description for Test Card 2.", 4, MarketCardType.Discard), dto.Market.FaceUpCards[1]);
        Assert.Null(dto.Market.FaceUpCards[2]);
    }

    [Fact]
    public void ToDto_Should_MapMarketCardLocalState()
    {
        var gameState = CreateGameState(4);
        gameState.Market.Initialize(new[]
        {
            new MarketCardState(
                "card-stateful-market",
                "Stateful Market Card",
                "Uses counters and stored energy.",
                4,
                MarketCardType.Keep,
                counters: 3,
                storedEnergy: 6)
        });

        var dto = gameState.ToDto();

        var cardDto = Assert.Single(dto.Market.FaceUpCards, card => card is not null);
        Assert.NotNull(cardDto);
        Assert.Equal("card-stateful-market", cardDto!.CardId);
        Assert.Equal(3, cardDto.Counters);
        Assert.Equal(6, cardDto.StoredEnergy);
    }

    [Fact]
    public void ToDto_Should_MapTurnDiceAndFlags()
    {
        var gameState = CreateGameState(4);
        gameState.StartGame();
        gameState.StartTurnForCurrentPlayer(diceCount: 7, maxRolls: 4);
        var turn = gameState.CurrentTurn!;
        turn.SetPhase(TurnPhase.Rolling);
        turn.IncrementRollCount();
        turn.IncrementRollCount();
        turn.DicePool.SetFace(0, DieFace.Attack);
        turn.DicePool.SetFace(1, DieFace.Energy);
        turn.DicePool.Lock(1);
        turn.MarkDiceResolved();
        turn.SetPhase(TurnPhase.Purchase);
        turn.MarkPurchasePhaseFinished();
        turn.Flags.AttackedWithDice = true;
        turn.Flags.DealtDamage = true;
        turn.Flags.EnteredTokyo = true;
        turn.Flags.StartedTurnInTokyo = true;
        turn.Flags.ScoredVictoryPoints = true;
        turn.Flags.EliminatedSomeone = true;
        turn.Flags.BoughtCard = true;
        turn.Flags.HerdCullerUsed = true;

        var dto = gameState.ToDto();

        Assert.NotNull(dto.CurrentTurn);
        Assert.Equal(0, dto.CurrentTurn!.CurrentPlayerId);
        Assert.Equal(TurnPhase.Purchase, dto.CurrentTurn.Phase);
        Assert.Equal(2, dto.CurrentTurn.RollCountUsed);
        Assert.Equal(4, dto.CurrentTurn.MaxRolls);
        Assert.Equal(7, dto.CurrentTurn.DiceCount);
        Assert.True(dto.CurrentTurn.DiceResolved);
        Assert.True(dto.CurrentTurn.PurchasePhaseFinished);
        Assert.Equal(7, dto.CurrentTurn.Dice.Count);
        Assert.Equal(new DieDto(0, DieFace.Attack, false), dto.CurrentTurn.Dice[0]);
        Assert.Equal(new DieDto(1, DieFace.Energy, true), dto.CurrentTurn.Dice[1]);
        Assert.Equal(new TurnFlagsDto(
            AttackedWithDice: true,
            DealtDamage: true,
            EnteredTokyo: true,
            StartedTurnInTokyo: true,
            ScoredVictoryPoints: true,
            EliminatedSomeone: true,
            BoughtCard: true,
            HerdCullerUsed: true), dto.CurrentTurn.Flags);
    }

    private static GameState CreateGameState(int playerCount, Guid? gameId = null)
    {
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new PlayerState(i, $"Monster {i + 1}"))
            .ToArray();
        var options = new GameOptions(playerCount);
        return new GameState(players, options, gameId);
    }

    private static MarketCardState CreateCard(string cardId, string name, int cost, MarketCardType cardType)
    {
        return new MarketCardState(
            cardId,
            name,
            $"Description for {name}.",
            cost,
            cardType);
    }
}
