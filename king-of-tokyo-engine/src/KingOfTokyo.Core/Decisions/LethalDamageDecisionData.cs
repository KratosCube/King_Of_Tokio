namespace KingOfTokyo.Core.Decisions;

public sealed record LethalDamageDecisionData(
    bool WingsActivated = false,
    int? PurchaseBuyerId = null,
    int PurchaseCost = 0);
