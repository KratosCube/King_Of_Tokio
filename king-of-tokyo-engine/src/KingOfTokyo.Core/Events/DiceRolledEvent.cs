using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Domain.Enums;

namespace KingOfTokyo.Core.Events;

public sealed class DiceRolledEvent : GameEventBase
{
    public int PlayerId { get; }
    public int RollNumber { get; }
    public IReadOnlyList<DieFace> Faces { get; }

    public DiceRolledEvent(int playerId, int rollNumber, IReadOnlyList<DieFace> faces)
    {
        if (rollNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rollNumber));
        }

        PlayerId = playerId;
        RollNumber = rollNumber;
        Faces = faces ?? throw new ArgumentNullException(nameof(faces));
    }

    public override string EventName => nameof(DiceRolledEvent);
}