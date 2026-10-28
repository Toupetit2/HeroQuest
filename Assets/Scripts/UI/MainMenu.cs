using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayCampaign()
    {
        SceneManager.LoadSceneAsync(1);
    }

    public void ChooseQuest()
    {
        // Choose Quest
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