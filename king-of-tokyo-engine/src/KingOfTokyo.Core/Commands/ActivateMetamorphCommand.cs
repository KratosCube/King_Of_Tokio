using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Commands;

public sealed class ActivateMetamorphCommand : CommandBase
{
    public string CardIdToDiscard { get; }

    public ActivateMetamorphCommand(string cardIdToDiscard, int? actorPlayerId = null)
        : base(actorPlayerId)
    {
        if (string.IsNullOrWhiteSpace(cardIdToDiscard))
        {
            throw new ArgumentException("Card id must not be empty.", nameof(cardIdToDiscard));
        }

        CardIdToDiscard = cardIdToDiscard;
    }
}