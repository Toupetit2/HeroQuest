using System.Collections.Generic;

// recompense de la quete, donnee par le gameplay si elle est reussie
public class Reward
{
    public int goldShared;  // a partager entre les heros
    public int goldEach;    // pour chaque heros
    public Dictionary<MonsterClass, int> bounties = new Dictionary<MonsterClass, int>(); // or par monstre tue (quete 11)
}
