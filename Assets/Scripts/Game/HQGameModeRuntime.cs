using System;
using UnityEngine;

/// <summary>
/// Intégration runtime entre GameManager et les générateurs existants.
/// N'utilise jamais EditorSceneManager ni AssetDatabase.
/// Le plateau est instancié depuis Resources une seule fois.
/// </summary>
public class HQGameModeRuntime : MonoBehaviour
{
    [Serializable]
    public class Options
    {
        public int seed = 12345;
        [Range(1, 22)] public int salles = 6;
        [Range(0, 31)] public int monstres = 8;
        [Range(0, 30)] public int meubles = 5;
        [Range(0, 25)] public int pieges = 2;
        [Range(0, 12)] public int blocs = 0;
        [Range(0, 1)] public float portesSupplementaires = 0.14f;
        [Range(0, 1)] public float portesSecretes = 0.08f;
    }

    public GameManager manager;
    public Options options = new Options();
    public int queteSelectionnee = 1;
    public bool montrerMenu = true;
    public bool afficherCarteReference = false;
    public bool montrerRoche = true;

    public TextAsset PlateauLogique { get; private set; }
    public HeroQuestSimplePCG Generateur { get; private set; }
    public HQCampaignQuestLoader Campagne { get; private set; }

    private GameObject plateau;
    private bool modeCampagne;
    private string message = "Choisissez un mode, puis lancez une quête.";
    private string seedChamp = "12345";
    private Vector2 scroll;

    private static readonly string[] Titres = {
        "01 - The Trial", "02 - Rescue of Sir Ragnar", "03 - Lair of the Orc Warlord",
        "04 - Prince Magnus' Gold", "05 - Melar's Maze", "06 - Legacy of the Orc Warlord",
        "07 - The Lost Wizard", "08 - The Fire Mage", "09 - Race Against Time",
        "10 - Castle of Mystery", "11 - Bastion of Dread", "12 - Barak Tor",
        "13 - Quest for the Spirit Blade", "14 - Return to Barak Tor"
    };

    private void Start()
    {
        if (manager == null) manager = GetComponent<GameManager>();
        seedChamp = options.seed.ToString();
        PreparerPlateau();
    }

    private bool PreparerPlateau()
    {
        if (plateau != null) return true;

        GameObject prefab = Resources.Load<GameObject>("HeroQuest/Plateau");
        PlateauLogique = Resources.Load<TextAsset>("PCG/board_prefab");
        if (prefab == null || PlateauLogique == null)
        {
            message = "Ressource absente : Resources/HeroQuest/Plateau.prefab ou Resources/PCG/board_prefab.txt";
            Debug.LogError(message, this);
            return false;
        }

        plateau = Instantiate(prefab, Vector3.zero, Quaternion.Euler(0f, 180f, 0f));
        plateau.name = "Plateau_Runtime";
        plateau.transform.localScale = Vector3.one;
        ReparerMateriauxManquants(plateau);

        Generateur = plateau.GetComponent<HeroQuestSimplePCG>();
        if (Generateur == null) Generateur = plateau.AddComponent<HeroQuestSimplePCG>();
        Generateur.boardFile = PlateauLogique;
        Generateur.wallsRoot = plateau.transform.Find("Walls");
        Generateur.firstCellLocalCenter = new Vector3(-25f, 0f, -18f);
        Generateur.cellSize = 2f;
        Generateur.generateOnPlay = false;
        Generateur.hideUnexploredRoomContents = true;
        if (Generateur.wallsRoot == null)
        {
            message = "Le prefab Plateau ne contient pas de groupe Walls.";
            Debug.LogError(message, this);
            Destroy(plateau);
            plateau = null;
            return false;
        }

        Campagne = plateau.GetComponent<HQCampaignQuestLoader>();
        if (Campagne == null) Campagne = plateau.AddComponent<HQCampaignQuestLoader>();
        Campagne.board = Generateur;
        Campagne.hideUnexploredRoomContents = true;
        Campagne.previewIncompleteQuestsWithoutDoors = false;
        Campagne.showReferenceMap = afficherCarteReference;
        Campagne.showRockZones = montrerRoche;

        // Empêche le vieux menu de s'afficher en double : seul le GameManager pilote le mode.
        foreach (HeroQuestPCGMenu oldMenu in plateau.GetComponents<HeroQuestPCGMenu>())
            oldMenu.enabled = false;

        // La caméra de jeu est configurée pendant l'exécution, sans outil Editor.
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            camera = go.AddComponent<Camera>();
        }
        camera.orthographic = true;
        camera.orthographicSize = 32f;
        camera.transform.SetPositionAndRotation(new Vector3(0, 68f, 0), Quaternion.Euler(90f, 0f, 0f));
        message = "Plateau original chargé. Sélectionnez votre mode.";
        return true;
    }

    private static void ReparerMateriauxManquants(GameObject root)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return;
        Material fallback = new Material(shader);
        fallback.color = new Color(0.55f, 0.53f, 0.49f);
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            bool modified = false;
            for (int i = 0; i < materials.Length; i++)
                if (materials[i] == null) { materials[i] = fallback; modified = true; }
            if (modified) renderer.sharedMaterials = materials;
        }
    }

    // Appelée depuis GameManager.LancerCampagne().
    public bool ChargerCampagne(int numero)
    {
        if (!PreparerPlateau()) return false;
        if (numero < 1 || numero > 14) return false;
        Campagne.previewIncompleteQuestsWithoutDoors = false;
        Campagne.hideUnexploredRoomContents = true;
        Campagne.selectedQuest = numero;
        Campagne.showRockZones = montrerRoche;
        Campagne.LoadQuest(numero);
        bool ok = Campagne.loadedQuest != null && Campagne.loadedQuest.questNumber == numero;
        message = Campagne.loadReport;
        return ok;
    }

    // Appelée depuis GameManager.LancerProcedural().
    public bool GenererProcedural(Options o)
    {
        if (!PreparerPlateau() || o == null) return false;
        Campagne.ClearCampaign();
        Generateur.seed = o.seed;
        Generateur.availableRooms = o.salles;
        Generateur.monsterCount = o.monstres;
        Generateur.furnitureCount = o.meubles;
        Generateur.trapCount = o.pieges;
        Generateur.blockedSquareCount = o.blocs;
        Generateur.extraDoorChance = o.portesSupplementaires;
        Generateur.secretDoorChance = o.portesSecretes;
        Generateur.generateOnPlay = false;
        Generateur.hideUnexploredRoomContents = true;
        Generateur.Generate();
        message = Generateur.lastSummary;
        return plateau.transform.Find("PCG_Generated") != null;
    }

    public void EffacerQuete()
    {
        if (Campagne != null) Campagne.ClearCampaign();
        if (Generateur != null) Generateur.ClearGenerated();
        montrerMenu = true;
        message = "Quête effacée.";
    }

    private void OnGUI()
    {
        if (!montrerMenu)
        {
            if (GUI.Button(new Rect(10, 10, 170, 34), "Choix de quête")) montrerMenu = true;
            return;
        }
        float h = Mathf.Min(Screen.height - 20, 640f);
        GUILayout.BeginArea(new Rect(10, 10, 385, Mathf.Max(150, h)), GUI.skin.box);
        scroll = GUILayout.BeginScrollView(scroll);
        GUILayout.Label("HeroQuest - GameManager");
        modeCampagne = GUILayout.Toolbar(modeCampagne ? 1 : 0,
            new[] { "Procedural", "Campagne" }) == 1;

        if (modeCampagne)
        {
            GUILayout.Label("Quêtes de base (données TXT non toutes vérifiées)");
            int newIndex = GUILayout.SelectionGrid(queteSelectionnee - 1, Titres, 2);
            if (newIndex >= 0) queteSelectionnee = newIndex + 1;
            afficherCarteReference = GUILayout.Toggle(afficherCarteReference, "Afficher la carte de référence");
            montrerRoche = GUILayout.Toggle(montrerRoche, "Afficher les cases rocheuses");
            if (GUILayout.Button("Lancer la quête de campagne", GUILayout.Height(35)))
            {
                if (manager != null) manager.LancerCampagne(queteSelectionnee);
            }
            if (afficherCarteReference && Campagne != null)
            {
                Campagne.selectedQuest = queteSelectionnee;
                Texture2D map = Campagne.ReferenceMap;
                if (map != null) GUILayout.Label(map, GUILayout.Width(340), GUILayout.Height(256));
            }
        }
        else
        {
            GUILayout.Label("Salles disponibles : " + options.salles);
            options.salles = Slider(options.salles, 1, 21);
            GUILayout.Label("Monstres : " + options.monstres);
            options.monstres = Slider(options.monstres, 0, 31);
            GUILayout.Label("Meubles : " + options.meubles);
            options.meubles = Slider(options.meubles, 0, 20);
            GUILayout.Label("Pièges : " + options.pieges);
            options.pieges = Slider(options.pieges, 0, 25);
            GUILayout.Label("Cases bloquées : " + options.blocs);
            options.blocs = Slider(options.blocs, 0, 12);
            GUILayout.Label("Portes supplémentaires : " + Mathf.RoundToInt(options.portesSupplementaires * 100) + "%");
            options.portesSupplementaires = GUILayout.HorizontalSlider(options.portesSupplementaires, 0f, 1f);
            GUILayout.Label("Portes secrètes : " + Mathf.RoundToInt(options.portesSecretes * 100) + "%");
            options.portesSecretes = GUILayout.HorizontalSlider(options.portesSecretes, 0f, 1f);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Seed", GUILayout.Width(43));
            seedChamp = GUILayout.TextField(seedChamp, GUILayout.Width(140));
            if (GUILayout.Button("Aléatoire")) seedChamp = UnityEngine.Random.Range(1, int.MaxValue).ToString();
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Générer une quête", GUILayout.Height(35)))
            {
                int seed;
                if (int.TryParse(seedChamp, out seed) && manager != null)
                {
                    options.seed = seed;
                    manager.LancerProcedural(options);
                }
                else message = "Seed invalide.";
            }
        }
        GUILayout.Space(7f);
        if (GUILayout.Button("Effacer / revenir au choix"))
        {
            if (manager != null) manager.RevenirAuChoixDuMode();
        }
        if (GUILayout.Button("Masquer ce menu")) montrerMenu = false;
        if (Campagne != null && GUILayout.Button("Révéler toutes les salles (test)"))
            Campagne.RevealAllRoomsForTesting();
        if (Generateur != null && GUILayout.Button("Révéler la PCG (test)"))
            Generateur.RevealAllRoomsForTesting();
        if (Campagne != null && GUILayout.Button("Découvrir portes secrètes (test)"))
            Campagne.DiscoverAllSecretDoorsForTesting();
        if (Campagne != null && GUILayout.Button("Révéler pièges (test)"))
            Campagne.RevealAllTrapsForTesting();
        GUILayout.Space(5f);
        GUILayout.Label(message);
        GUILayout.Label("Les quêtes de campagne sans portes retranscrites n'ont pas encore d'exploration complète.");
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private static int Slider(int value, int low, int high)
    {
        return Mathf.RoundToInt(GUILayout.HorizontalSlider(value, low, high));
    }
}
