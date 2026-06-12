using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace KingOfTokyo.Web.Pages;

public partial class GameTable
{
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;

    private string? _lastOpportunistPromptKey;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await PromptForOpportunistPurchaseDecisionAsync();
    }

    private async Task PromptForOpportunistPurchaseDecisionAsync()
    {
        if (_busy || _game?.PendingDecision is null)
        {
            return;
        }

        var decision = _game.PendingDecision;
        if (!string.Equals(decision.DecisionType, "OpportunistPurchase", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (Session.PlayerId is null || decision.PlayerId != Session.PlayerId.Value)
        {
            return;
        }

        var payload = decision.Payload;
        if (payload is null || payload.Value.ValueKind != System.Text.Json.JsonValueKind.Object)
        {
            return;
        }

        var json = payload.Value;
        var slotIndex = ReadInt(json, "slotIndex") ?? -1;
        var cardId = ReadString(json, "cardId") ?? string.Empty;
        var cardName = ReadString(json, "cardName") ?? "revealed card";
        var cost = ReadInt(json, "cost") ?? 0;
        var promptKey = $"{_game.GameId}:{_game.Version}:{decision.PlayerId}:{slotIndex}:{cardId}";

        if (string.Equals(_lastOpportunistPromptKey, promptKey, StringComparison.Ordinal))
        {
            return;
        }

        _lastOpportunistPromptKey = promptKey;

        var buyCard = await JsRuntime.InvokeAsync<bool>(
            "confirm",
            $"Opportunist: buy {cardName} for {cost} energy now?\n\nOK = Buy revealed card\nCancel = Skip");

        if (_game?.PendingDecision is null ||
            !string.Equals(_game.PendingDecision.DecisionType, "OpportunistPurchase", StringComparison.OrdinalIgnoreCase) ||
            _game.PendingDecision.PlayerId != Session.PlayerId)
        {
            return;
        }

        await ExecuteAsync(() => buyCard
            ? ApiClient.BuyOpportunistRevealedCardAsync(GameId, new(Session.PlayerId))
            : ApiClient.DeclineOpportunistRevealedCardAsync(GameId, new(Session.PlayerId)));

        await InvokeAsync(StateHasChanged);
    }
}
