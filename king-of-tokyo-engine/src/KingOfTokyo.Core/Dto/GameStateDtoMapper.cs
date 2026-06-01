using KingOfTokyo.Core.Decisions;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.State;

namespace KingOfTokyo.Core.Dto;

public static class GameStateDtoMapper
{
    public static GameStateDto ToDto(this GameState gameState)
    {
        ArgumentNullException.ThrowIfNull(gameState);

        return new GameStateDto(
            gameState.GameId,
            gameState.Version,
            gameState.Status,
            gameState.CurrentPlayerIndex,
            gameState.WinnerInfo?.WinnerPlayerId,
            gameState.WinnerInfo?.Reason,
            gameState.Players.Select(ToDto).ToArray(),
            ToDto(gameState.Tokyo),
            ToDto(gameState.Market),
            gameState.CurrentTurn is null ? null : ToDto(gameState.CurrentTurn),
            gameState.PendingDecision is null ? null : ToDto(gameState.PendingDecision));
    }

    private static PlayerDto ToDto(PlayerState player)
    {
        return new PlayerDto(
            player.PlayerId,
            player.MonsterName,
            player.Health,
            player.MaxHealth,
            player.VictoryPoints,
            player.Energy,
            player.TokyoSlot,
            player.IsAlive,
            ToDto(player.Status),
            player.KeepCards.Select(ToDto).ToArray());
    }

    private static PlayerStatusDto ToDto(PlayerStatusState status)
    {
        return new PlayerStatusDto(status.PoisonTokens, status.ShrinkTokens);
    }

    private static TokyoDto ToDto(TokyoState tokyo)
    {
        return new TokyoDto(tokyo.CityOccupantId, tokyo.BayOccupantId, tokyo.BayEnabled, tokyo.IsEmpty);
    }

    private static MarketDto ToDto(MarketState market)
    {
        return new MarketDto(
            market.FaceUpCards.Select(card => card is null ? null : ToDto(card)).ToArray(),
            market.DrawPileCount,
            market.DiscardPileCount);
    }

    private static CardDto ToDto(MarketCardState card)
    {
        return new CardDto(
            card.CardId,
            card.Name,
            card.Description,
            card.Cost,
            card.CardType,
            card.Counters,
            card.StoredEnergy);
    }

    private static TurnDto ToDto(TurnState turn)
    {
        return new TurnDto(
            turn.CurrentPlayerId,
            turn.Phase,
            turn.RollCountUsed,
            turn.MaxRolls,
            turn.DiceCount,
            turn.DiceResolved,
            turn.PurchasePhaseFinished,
            turn.DicePool.Dice.Select(ToDto).ToArray(),
            ToDto(turn.Flags));
    }

    private static DieDto ToDto(DieState die)
    {
        return new DieDto(die.Index, die.CurrentFace, die.IsLocked);
    }

    private static TurnFlagsDto ToDto(TurnFlags flags)
    {
        return new TurnFlagsDto(
            flags.AttackedWithDice,
            flags.DealtDamage,
            flags.EnteredTokyo,
            flags.StartedTurnInTokyo,
            flags.ScoredVictoryPoints,
            flags.EliminatedSomeone,
            flags.BoughtCard,
            flags.HerdCullerUsed);
    }

    private static PendingDecisionDto ToDto(PendingDecision pendingDecision)
    {
        return new PendingDecisionDto(pendingDecision.DecisionType, pendingDecision.PlayerId, pendingDecision.Payload);
    }
}