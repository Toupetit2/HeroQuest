using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Point d'entrée sans Editor script : ouvre Assets/Scenes/HQ_Runtime.unity et appuie Play.
/// Crée le GameManager dans cette scène uniquement (ne touche pas aux scènes de l'équipe).
/// </summary>
public static class HQGameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Demarrer()
    {
        if (SceneManager.GetActiveScene().name != "HQ_Runtime") return;
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (gm == null)
            new GameObject("GameManager").AddComponent<GameManager>();
    }
}
