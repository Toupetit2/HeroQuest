using System.IO;
using UnityEngine;

// Choix de la session : progression lineaire, puis rejeu libre apres la fin.
// Les quetes personnelles sont chargees depuis QuestStorage, sans progression.
public static class QuestSelection
{
    public static string FileName = "";
    public static int Number;
    public static string Title = "";
    public static bool IsCampaign;

    public static bool HasSelection
    {
        get { return !string.IsNullOrEmpty(FileName); }
    }

    public static void Set(string fileName, int number, string title, bool campaign)
    {
        if (campaign && !CampaignProgress.CanPlay(fileName))
        {
            Debug.LogWarning("Campagne : impossible de lancer une quete hors progression : " + fileName);
            Clear();
            return;
        }
        FileName = Path.GetFileName(fileName);
        Number = number;
        Title = title;
        IsCampaign = campaign;
    }

    public static bool TrySelectNextCampaign()
    {
        CampaignProgress.Chapter chapter;
        if (!CampaignProgress.TryGetNext(out chapter))
        {
            Clear();
            return false;
        }
        Set(chapter.fileName, chapter.number, chapter.title, true);
        return HasSelection;
    }

    // Rejeu volontaire uniquement apres la campagne terminee.
    // Les noms de fichiers sont controles contre les quetes officielles.
    public static bool TrySelectReplayCampaign(string fileName)
    {
        if (!CampaignProgress.IsFinished || !CampaignProgress.IsOfficialChapter(fileName))
        {
            Clear();
            return false;
        }
        foreach (CampaignProgress.Chapter chapter in CampaignProgress.GetChapters())
        {
            if (!string.Equals(chapter.fileName, Path.GetFileName(fileName), System.StringComparison.Ordinal)) continue;
            Set(chapter.fileName, chapter.number, chapter.title, true);
            return HasSelection;
        }
        Clear();
        return false;
    }

    public static void Clear()
    {
        FileName = "";
        Number = 0;
        Title = "";
        IsCampaign = false;
    }

    public static string ReadJson()
    {
        if (!HasSelection) return null;

        if (IsCampaign)
        {
            // Ne pas permettre a une ancienne selection de contourner les verrous.
            if (!CampaignProgress.CanPlay(FileName)) return null;
            TextAsset asset = Resources.Load<TextAsset>("Quests/" + Path.GetFileNameWithoutExtension(FileName));
            return asset != null ? asset.text : null;
        }
        return QuestStorage.Read(FileName);
    }
}
