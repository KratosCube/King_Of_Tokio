namespace KingOfTokyo.Core.Domain.State;

public sealed class TurnFlags
{
    public bool AttackedWithDice { get; set; }
    public bool DealtDamage { get; set; }
    public bool EnteredTokyo { get; set; }
    public bool StartedTurnInTokyo { get; set; }
    public bool ScoredVictoryPoints { get; set; }
    public bool EliminatedSomeone { get; set; }
    public bool BoughtCard { get; set; }
    public bool HerdCullerUsed { get; set; }
}