using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Initialise le menu PCG/Campagne en Play Mode dans la scène HQ_Runtime.
/// Laisse intact le GameManager de l'équipe.
/// </summary>
public static class HQGameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Demarrer()
    {
        if (SceneManager.GetActiveScene().name != "HQ_Runtime")
            return;

        GameManager manager = Object.FindFirstObjectByType<GameManager>();
        if (manager == null)
            manager = new GameObject("GameManager").AddComponent<GameManager>();

        HQGameModeRuntime runtime = manager.GetComponent<HQGameModeRuntime>();
        if (runtime == null)
            runtime = manager.gameObject.AddComponent<HQGameModeRuntime>();

        runtime.manager = manager;
        runtime.montrerMenu = true;
        runtime.enabled = true;

        Debug.Log("[HeroQuest] HQGameModeRuntime activé sur GameManager. Le menu doit apparaître dans l'onglet Game.", manager);
    }
}
