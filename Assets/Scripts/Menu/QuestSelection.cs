using System.IO;
using UnityEngine;

// quete choisie dans le menu, lue par le jeu apres le chargement de la scene de jeu :
//     string json = QuestSelection.ReadJson();   // null si aucune quete n'a ete choisie
//     if (json != null) questLoader.Load(json);
// campagne : Resources/Quests/quest_NN.json (fournies avec le jeu)
// quete perso : dossier de QuestStorage (creees avec l'editeur)
public static class QuestSelection
{
    public static string FileName = "";   // ex : "quest_01.json"
    public static int Number;
    public static string Title = "";
    public static bool IsCampaign;

    public static bool HasSelection
    {
        get { return !string.IsNullOrEmpty(FileName); }
    }

    public static void Set(string fileName, int number, string title, bool campaign)
    {
        FileName = fileName;
        Number = number;
        Title = title;
        IsCampaign = campaign;
    }

    public static void Clear()
    {
        Set("", 0, "", false);
    }

    public static string ReadJson()
    {
        if (!HasSelection) return null;

        if (IsCampaign)
        {
            TextAsset asset = Resources.Load<TextAsset>("Quests/" + Path.GetFileNameWithoutExtension(FileName));
            return asset != null ? asset.text : null;
        }
        return QuestStorage.Read(FileName);
    }
}
