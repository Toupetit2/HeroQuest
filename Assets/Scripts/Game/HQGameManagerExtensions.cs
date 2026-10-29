using UnityEngine;

/// <summary>
/// Ajoute les commandes de quête au GameManager SANS modifier la classe de l'équipe.
/// Les méthodes d'extension sont appelables comme manager.LancerCampagne(1).
/// Requiert un GameManager d'origine qui expose GetGameState().
/// </summary>
public static class HQGameManagerExtensions
{
    private static HQGameModeRuntime GetRuntime(GameManager manager)
    {
        if (manager == null)
        {
            Debug.LogError("HQ: GameManager absent.");
            return null;
        }

        HQGameModeRuntime runtime = manager.GetComponent<HQGameModeRuntime>();
        if (runtime == null)
            runtime = manager.gameObject.AddComponent<HQGameModeRuntime>();

        runtime.manager = manager;
        return runtime;
    }

    private static bool SynchroniserCarte(GameManager manager, HQGameModeRuntime runtime)
    {
        GameState state = manager.GetGameState();
        if (state == null)
        {
            Debug.LogError("HQ: GetGameState() renvoie null : impossible d'initialiser la carte logique.");
            return false;
        }

        if (runtime.PlateauLogique == null)
        {
            Debug.LogError("HQ: board_prefab.txt n'a pas pu etre charge.");
            return false;
        }

        state.map = new Map(runtime.PlateauLogique.text);
        return true;
    }

    public static bool LancerCampagne(this GameManager manager, int numeroQuete)
    {
        HQGameModeRuntime runtime = GetRuntime(manager);
        if (runtime == null || !runtime.ChargerCampagne(numeroQuete)) return false;
        return SynchroniserCarte(manager, runtime);
    }

    public static bool LancerProcedural(
        this GameManager manager, HQGameModeRuntime.Options options)
    {
        HQGameModeRuntime runtime = GetRuntime(manager);
        if (runtime == null || !runtime.GenererProcedural(options)) return false;
        return SynchroniserCarte(manager, runtime);
    }

    public static void RevenirAuChoixDuMode(this GameManager manager)
    {
        HQGameModeRuntime runtime = GetRuntime(manager);
        if (runtime != null) runtime.EffacerQuete();
    }
}
