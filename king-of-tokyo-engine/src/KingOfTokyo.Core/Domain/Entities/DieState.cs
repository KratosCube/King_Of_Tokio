using KingOfTokyo.Core.Domain.Enums;

namespace KingOfTokyo.Core.Domain.Entities;

public sealed class DieState
{
    public int Index { get; }
    public DieFace CurrentFace { get; private set; }
    public bool IsLocked { get; private set; }

    public DieState(int index, DieFace currentFace = DieFace.One)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Die index must be non-negative.");
        }

        Index = index;
        CurrentFace = currentFace;
        IsLocked = false;
    }

    public void SetFace(DieFace face)
    {
        CurrentFace = face;
        IsLocked = false;
    }

    public void Lock()
    {
        IsLocked = true;
    }

    public void Unlock()
    {
        IsLocked = false;
    }


}