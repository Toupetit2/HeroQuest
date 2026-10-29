using UnityEngine;

// les boutons du Canvas appellent deja ces methodes (cablage de la scene MainMenu inchange)
// questPicker / questEditor sont poses dans la scene par HeroQuest > Installer le menu et le plateau
// les deux modes restent distincts et ne lancent pas directement de scene sans selecteur
public class MainMenu : MonoBehaviour
{
    public QuestPickerMenu questPicker;
    public QuestEditorMenu questEditor;

    void Awake()
    {
        QuestSelection.Clear(); // aucune session precedente ne survit au retour au menu
        if (questPicker == null) questPicker = FindFirstObjectByType<QuestPickerMenu>();
        if (questEditor == null) questEditor = FindFirstObjectByType<QuestEditorMenu>();
    }

    public void PlayCampaign()
    {
        if (questPicker != null) questPicker.OpenCampaign();
        else Debug.LogError("MainMenu : QuestPickerMenu manquant, campagne non lancee.");
    }

    public void ChooseQuest()
    {
        if (questPicker != null) questPicker.OpenPersonalQuests();
        else Debug.LogWarning("MainMenu : aucun QuestPickerMenu dans la scene (menu HeroQuest > Installer le menu et le plateau)");
    }

    public void QuestEditor()
    {
        if (questEditor != null) questEditor.Open();
        else Debug.LogWarning("MainMenu : aucun QuestEditorMenu dans la scene (menu HeroQuest > Installer le menu et le plateau)");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
