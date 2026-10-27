using System.Collections.Generic;

public class Victory
{
    public VictoryMode mode;
    public List<Objective> objectives = new List<Objective>();

    // vrai si la condition de victoire est remplie
    public bool IsWon()
    {
        if (objectives.Count == 0) return false;

        foreach (Objective o in objectives)
        {
            if (mode == VictoryMode.AuMoinsUn && o.isDone) return true;
            if (mode == VictoryMode.Tous && !o.isDone) return false;
        }
        return mode == VictoryMode.Tous;
    }
}
