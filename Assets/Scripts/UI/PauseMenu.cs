using UnityEngine;
using UnityEngine.SceneManagement;

// menu pause de la partie : bouton "Menu" toujours visible en haut a droite, ou touche Echap
// Resume / Main menu / Quit game ; ajoute par le UIManager, rien a placer dans la scene
// PauseMenu.IsOpen permet au reste du jeu d'ignorer les clics sur le plateau pendant la pause
public class PauseMenu : MonoBehaviour
{
    public const string MainMenuScene = "MainMenu";

    public static bool IsOpen { get; private set; }

    GameObject menuPanel;

    void Start()
    {
        Canvas canvas = UIFactory.OverlayCanvas("PauseMenuCanvas", transform, 100);
        Transform root = canvas.transform;

        // bouton toujours visible, en haut a droite de l'ecran
        RectTransform openButton = (RectTransform)UIFactory.Button("MenuButton", root, "Menu (Esc)", 24, new Vector2(170f, 54f), Vector2.zero, Toggle).transform;
        openButton.anchorMin = openButton.anchorMax = new Vector2(1f, 1f);
        openButton.pivot = new Vector2(1f, 1f);
        openButton.anchoredPosition = new Vector2(-20f, -20f);

        // fond sombre qui couvre tout l'ecran et bloque les clics derriere
        menuPanel = UIFactory.Panel("PausePanel", root, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.6f), false).gameObject;
        UIFactory.Stretch((RectTransform)menuPanel.transform);

        Transform box = UIFactory.Panel("Box", menuPanel.transform, new Vector2(460f, 400f), Vector2.zero, UIFactory.DarkBrown, true).transform;
        UIFactory.Label("Title", box, "Paused", 44, UIFactory.Gold, TextAnchor.MiddleCenter, new Vector2(420f, 70f), new Vector2(0f, 135f));
        UIFactory.Button("Resume", box, "Resume", 28, new Vector2(320f, 64f), new Vector2(0f, 40f), Close);
        UIFactory.Button("MainMenu", box, "Main menu", 28, new Vector2(320f, 64f), new Vector2(0f, -40f), GoToMainMenu);
        UIFactory.Button("Quit", box, "Quit game", 28, new Vector2(320f, 64f), new Vector2(0f, -120f), QuitGame);

        menuPanel.SetActive(false);
        IsOpen = false;
    }

    void Update()
    {
        // Echap sert deja a fermer l'editeur de quetes quand il est ouvert
        if (Input.GetKeyDown(KeyCode.Escape) && !QuestEditorMenu.IsOpen) Toggle();
    }

    void OnDestroy()
    {
        IsOpen = false;
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (menuPanel == null) return;
        menuPanel.SetActive(true);
        IsOpen = true;
    }

    public void Close()
    {
        if (menuPanel == null) return;
        menuPanel.SetActive(false);
        IsOpen = false;
    }

    // MainMenu remet la selection de quete a zero dans son Awake
    public void GoToMainMenu()
    {
        IsOpen = false;
        SceneManager.LoadScene(MainMenuScene);
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
