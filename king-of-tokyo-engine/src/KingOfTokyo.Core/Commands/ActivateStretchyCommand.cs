using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Domain.Enums;

namespace KingOfTokyo.Core.Commands;

public sealed class ActivateStretchyCommand : CommandBase
{
    public int DieIndex { get; }
    public DieFace TargetFace { get; }

    public ActivateStretchyCommand(int dieIndex, DieFace targetFace, int? actorPlayerId = null)
        : base(actorPlayerId)
    {
        if (dieIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dieIndex));
        }

        DieIndex = dieIndex;
        TargetFace = targetFace;
    }
}