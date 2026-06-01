using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Commands;

public sealed class ChooseLeaveTokyoCommand : CommandBase
{
    public bool LeaveTokyo { get; }

    public ChooseLeaveTokyoCommand(bool leaveTokyo, int actorPlayerId) : base(actorPlayerId)
    {
        LeaveTokyo = leaveTokyo;
    }
}