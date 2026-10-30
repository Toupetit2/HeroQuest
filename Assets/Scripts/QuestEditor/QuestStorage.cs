using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// lecture / ecriture des fichiers de quete JSON
// toutes les quetes sont rangees au meme endroit : Assets/Data/Resources/Quests
//  - official_NN.json : les 14 quetes du livret, en lecture seule (jamais ecrasees par l'editeur)
//  - custom_NN.json   : les quetes creees avec l'editeur
// dans un jeu compile, Resources est en lecture seule : les quetes creees par le joueur
// sont ecrites dans <persistentDataPath>/Quests, avec le meme prefixe custom_
public static class QuestStorage
{
    public const string OfficialPrefix = "official_";
    public const string CustomPrefix = "custom_";
    const string ResourcesFolder = "Quests";

    // dossier ou l'editeur ecrit les quetes perso
    public static string QuestDir
    {
        get
        {
#if UNITY_EDITOR
            return Path.Combine(Application.dataPath, "Data/Resources/Quests");
#else
            return Path.Combine(Application.persistentDataPath, "Quests");
#endif
        }
    }

    public static bool IsOfficial(string fileName)
    {
        return Path.GetFileName(fileName).StartsWith(OfficialPrefix, StringComparison.Ordinal);
    }

    public static string OfficialFileName(int number)
    {
        return OfficialPrefix + number.ToString("00") + ".json";
    }

    public static string CustomFileName(int number)
    {
        return CustomPrefix + number.ToString("00") + ".json";
    }

    // quetes perso (custom_*.json), triees ; includeOfficial = ajoute aussi les quetes du livret en tete
    public static List<string> ListFiles(bool includeOfficial = false)
    {
        List<string> names = new List<string>();
        if (includeOfficial) AddResources(names, OfficialPrefix);

        List<string> custom = new List<string>();
        AddResources(custom, CustomPrefix);   // fournies avec le jeu
        if (Directory.Exists(QuestDir))
        {
            foreach (string f in Directory.GetFiles(QuestDir, CustomPrefix + "*.json"))
            {
                string name = Path.GetFileName(f);
                if (!custom.Contains(name)) custom.Add(name);
            }
        }
        custom.Sort(StringComparer.Ordinal);
        names.AddRange(custom);
        return names;
    }

    static void AddResources(List<string> names, string prefix)
    {
        List<string> found = new List<string>();
        foreach (TextAsset asset in Resources.LoadAll<TextAsset>(ResourcesFolder))
        {
            if (asset != null && asset.name.StartsWith(prefix, StringComparison.Ordinal))
                found.Add(asset.name + ".json");
        }
        found.Sort(StringComparer.Ordinal);
        names.AddRange(found);
    }

    public static bool Exists(string fileName)
    {
        if (File.Exists(Path.Combine(QuestDir, fileName))) return true;
        return Resources.Load<TextAsset>(ResourcesFolder + "/" + Path.GetFileNameWithoutExtension(fileName)) != null;
    }

    // le fichier du dossier perso en priorite, sinon celui fourni avec le jeu (Resources)
    public static string Read(string fileName)
    {
        string path = Path.Combine(QuestDir, fileName);
        if (File.Exists(path)) return File.ReadAllText(path);

        TextAsset asset = Resources.Load<TextAsset>(ResourcesFolder + "/" + Path.GetFileNameWithoutExtension(fileName));
        if (asset == null) throw new FileNotFoundException("Quest not found: " + fileName);
        return asset.text;
    }

    // n'ecrit que des quetes perso : une quete du livret ne peut pas etre remplacee
    public static void Write(string fileName, string json)
    {
        if (!Path.GetFileName(fileName).StartsWith(CustomPrefix, StringComparison.Ordinal))
            throw new InvalidOperationException("Only custom quests (" + CustomPrefix + "NN.json) can be saved.");

        Directory.CreateDirectory(QuestDir);
        File.WriteAllText(Path.Combine(QuestDir, fileName), json, new UTF8Encoding(false));
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
}
