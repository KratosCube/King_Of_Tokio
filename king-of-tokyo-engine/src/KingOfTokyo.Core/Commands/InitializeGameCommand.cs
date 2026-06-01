using KingOfTokyo.Core.Abstractions;

namespace KingOfTokyo.Core.Commands;

public sealed class InitializeGameCommand : CommandBase
{
    public InitializeGameCommand() : base(null)
    {
    }
}