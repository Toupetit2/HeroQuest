using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Parcours lineaire des quetes officielles uniquement.
// Les quetes de joueurs (QuestStorage) ne sont jamais prises en compte ici.
// La sauvegarde memorise le nombre de chapitres termines, pas un fichier librement choisi.
public static class CampaignProgress
{
    const string SaveKey = "HeroQuest.Campaign.CompletedChapters.v1";

    public sealed class Chapter
    {
        public string fileName;
        public int number;
        public string title;
    }

    private static List<Chapter> cachedChapters;

    private static List<Chapter> LoadChapters()
    {
        if (cachedChapters != null) return cachedChapters;
        List<Chapter> chapters = new List<Chapter>();
        foreach (TextAsset asset in Resources.LoadAll<TextAsset>("Quests"))
        {
            // le dossier contient aussi les quetes perso (custom_) : seules les official_ font la campagne
            if (asset == null || !QuestStorage.IsOfficial(asset.name)) continue;
            try
            {
                QuestDefinition definition = JsonUtility.FromJson<QuestDefinition>(asset.text);
                if (definition == null || definition.number <= 0) continue;
                chapters.Add(new Chapter
                {
                    fileName = asset.name + ".json",
                    number = definition.number,
                    title = definition.title ?? ""
                });
            }
            catch (Exception e)
            {
                Debug.LogWarning("Campagne : quete officielle ignoree (" + asset.name + ") : " + e.Message);
            }
        }
        chapters.Sort((a, b) =>
        {
            int byNumber = a.number.CompareTo(b.number);
            return byNumber != 0 ? byNumber : string.CompareOrdinal(a.fileName, b.fileName);
        });
        cachedChapters = chapters;
        return cachedChapters;
    }

    public static List<Chapter> GetChapters()
    {
        return new List<Chapter>(LoadChapters());
    }

    public static int CompletedCount
    {
        get { return Mathf.Clamp(PlayerPrefs.GetInt(SaveKey, 0), 0, LoadChapters().Count); }
    }

    // La selection libre n'est autorisee qu'apres la victoire du dernier chapitre.
    public static bool IsFinished
    {
        get { return LoadChapters().Count > 0 && CompletedCount == LoadChapters().Count; }
    }

    public static bool IsOfficialChapter(string fileName)
    {
        string name = Path.GetFileName(fileName);
        foreach (Chapter chapter in LoadChapters())
            if (string.Equals(chapter.fileName, name, StringComparison.Ordinal)) return true;
        return false;
    }

    // Avant la fin : seulement le chapitre suivant. Apres : les 14 chapitres.
    public static bool CanPlay(string fileName)
    {
        return IsNext(fileName) || (IsFinished && IsOfficialChapter(fileName));
    }

    public static bool TryGetNext(out Chapter chapter)
    {
        List<Chapter> chapters = LoadChapters();
        int completed = Mathf.Clamp(PlayerPrefs.GetInt(SaveKey, 0), 0, chapters.Count);
        chapter = completed < chapters.Count ? chapters[completed] : null;
        return chapter != null;
    }

    public static bool IsNext(string fileName)
    {
        Chapter chapter;
        return TryGetNext(out chapter) &&
               string.Equals(chapter.fileName, Path.GetFileName(fileName), StringComparison.Ordinal);
    }

    // Appele uniquement apres une victoire reelle dans GameManager.
    // Une victoire en mode rejeu conserve la campagne terminee (aucune reecriture).
    public static bool TryComplete(string fileName)
    {
        if (IsFinished) return IsOfficialChapter(fileName);
        if (!IsNext(fileName)) return false;
        PlayerPrefs.SetInt(SaveKey, CompletedCount + 1);
        PlayerPrefs.Save();
        return true;
    }

    public static void Reset()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }
}
