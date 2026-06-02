using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Commands;

public sealed class ActivatePsychicProbeCommand : CommandBase
{
    public ActivatePsychicProbeCommand(int actorPlayerId, int targetDieIndex) : base(actorPlayerId)
    {
        TargetDieIndex = targetDieIndex;
    }

    public int TargetDieIndex { get; }
}
