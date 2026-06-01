using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Commands;

public sealed class ActivateHerdCullerCommand : CommandBase
{
    public int DieIndex { get; }

    public ActivateHerdCullerCommand(int dieIndex, int? actorPlayerId = null)
        : base(actorPlayerId)
    {
        if (dieIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dieIndex));
        }

        DieIndex = dieIndex;
    }
}