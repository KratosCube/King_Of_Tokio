namespace KingOfTokyo.Core.Domain.Entities;

public sealed class TokyoState
{
    public int? CityOccupantId { get; private set; }
    public int? BayOccupantId { get; private set; }
    public bool BayEnabled { get; private set; }

    public TokyoState(bool bayEnabled)
    {
        BayEnabled = bayEnabled;
    }

    public bool IsEmpty => CityOccupantId is null && BayOccupantId is null;

    public void SetCityOccupant(int? playerId)
    {
        CityOccupantId = playerId;
    }

    public void SetBayOccupant(int? playerId)
    {
        if (!BayEnabled && playerId is not null)
        {
            throw new InvalidOperationException("Cannot assign bay occupant when bay is disabled.");
        }

        BayOccupantId = playerId;
    }

    public void DisableBay()
    {
        BayEnabled = false;
        BayOccupantId = null;
    }

    public void EnableBay()
    {
        BayEnabled = true;
    }
}