using UnityEngine;
using UnityEngine.SceneManagement;

// les boutons du Canvas appellent deja ces methodes (cablage de la scene MainMenu inchange)
// questPicker / questEditor sont poses dans la scene par HeroQuest > Installer le menu et le plateau
// s'ils manquent, PlayCampaign garde son comportement d'origine
public class MainMenu : MonoBehaviour
{
    public QuestPickerMenu questPicker;
    public QuestEditorMenu questEditor;

    void Awake()
    {
        if (questPicker == null) questPicker = FindFirstObjectByType<QuestPickerMenu>();
        if (questEditor == null) questEditor = FindFirstObjectByType<QuestEditorMenu>();
    }

    public void PlayCampaign()
    {
        if (questPicker != null) questPicker.Open(true);
        else SceneManager.LoadSceneAsync(1);
    }

    public void ChooseQuest()
    {
        if (questPicker != null) questPicker.Open(false);
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
