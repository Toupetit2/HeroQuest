using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// lecture / ecriture des fichiers de quete JSON, dans l'editeur ET dans un jeu compile
//  - dans l'editeur : Assets/Data/Quests (les quetes du projet)
//  - dans un jeu compile : <persistentDataPath>/Quests (le dossier Assets n'existe plus)
// le menu du jeu relit une quete avec QuestStorage.Read("quest_02.json") et la donne au QuestLoader
public static class QuestStorage
{
    public static string QuestDir
    {
        get
        {
#if UNITY_EDITOR
            return Path.Combine(Application.dataPath, "Data/Quests");
#else
            return Path.Combine(Application.persistentDataPath, "Quests");
#endif
        }
    }

    // noms des fichiers quest_*.json, tries
    public static List<string> ListFiles()
    {
        List<string> names = new List<string>();
        if (!Directory.Exists(QuestDir)) return names;

        string[] files = Directory.GetFiles(QuestDir, "quest_*.json");
        Array.Sort(files, StringComparer.Ordinal);
        foreach (string f in files) names.Add(Path.GetFileName(f));
        return names;
    }

    public static bool Exists(string fileName)
    {
        return File.Exists(Path.Combine(QuestDir, fileName));
    }

    public static string Read(string fileName)
    {
        return File.ReadAllText(Path.Combine(QuestDir, fileName));
    }

    public static void Write(string fileName, string json)
    {
        Directory.CreateDirectory(QuestDir);
        File.WriteAllText(Path.Combine(QuestDir, fileName), json, new UTF8Encoding(false));
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
}
