using System;

// Les fichiers Resources/Campaign/quest_XX.txt sont du JSON lisible.
// Une quête n'est dite COMPLETE que lorsque tous ses objets, portes et notes
// ont été comparés au Quest Book. Pas de placement aléatoire ici.
[Serializable]
public class HQCampaignQuestData
{
    public int questNumber;
    public string title;
    public int questBookPdfPage;
    public string digitization;
    public string description;
    public string wanderingMonster;
    public string[] rockRows;
    public HQCampaignDoorData[] doors;
    public HQCampaignObjectData[] objects;
    public string[] specialNotes;
    // Metadata already present in Resources/Campaign/*.txt.
    // Keeping these fields ensures the editor does not discard them on Save.
    public string referenceMap;
    public string boardPreset;
    public string summaryBriefFR;
    public string source;
    public bool transcriptionVerified;
}

[Serializable]
public class HQCampaignDoorData
{
    public int x;
    public int y;                 // Coordonnees de la carte lue du haut vers le bas
    public string direction;      // E ou S : frontiere vers la cellule voisine
    public bool secret;
}

[Serializable]
public class HQCampaignObjectData
{
    public string group;          // stairs / monster / furniture / obstacle / trap
    public int id;
    public int x;
    public int y;                 // Coin SUPERIEUR gauche sur le Quest Book
    public int width = 1;
    public int height = 1;
    public int angle;
}
