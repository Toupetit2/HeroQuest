using UnityEngine;

// A minimal in-game settings/debugging menu. Replace with a Canvas after gameplay works.
public class HeroQuestPCGMenu : MonoBehaviour
{
    public HeroQuestSimplePCG generator;
    public HQCampaignQuestLoader campaign;
    private int selectedTab = 0;
    public bool menuVisible = true;
    private string seedText = "12345";
    private string message = "";
    private Vector2 scroll;

    private void Awake()
    {
        if (generator == null) generator = GetComponent<HeroQuestSimplePCG>();
        if (campaign == null) campaign = GetComponent<HQCampaignQuestLoader>();
        if (campaign == null) campaign = gameObject.AddComponent<HQCampaignQuestLoader>();
        if (campaign != null) campaign.board = generator;
        if (generator != null) seedText = generator.seed.ToString();
    }

    private void OnGUI()
    {
        if (generator == null) return;
        if (!menuVisible)
        {
            if (GUI.Button(new Rect(12, 12, 165, 34), "Dungeon options")) menuVisible = true;
            return;
        }

        float height = Mathf.Min(675, Mathf.Max(120, Screen.height - 24));
        GUILayout.BeginArea(new Rect(12, 12, 365, height), GUI.skin.box);
        scroll = GUILayout.BeginScrollView(scroll);
        GUILayout.Label("HeroQuest - Choix du mode");
        selectedTab = GUILayout.Toolbar(selectedTab, new string[] { "Procedural", "Campagne de base" });
        GUILayout.Space(7);
        if (selectedTab == 1)
        {
            DrawCampaign();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            return;
        }
        GUILayout.Label("Classic-style random quest (not an official map).");

        generator.availableRooms = NumberSlider("Accessible rooms", generator.availableRooms, 1,
                                               generator.respectPhysicalPieceLimits ? 21 : 22);
        generator.furnitureCount = NumberSlider("Extra furniture", generator.furnitureCount, 0, 20);
        generator.monsterCount = NumberSlider("Monsters", generator.monsterCount, 0, 31);
        generator.trapCount = NumberSlider("Hidden traps", generator.trapCount, 0, 25);
        generator.blockedSquareCount = NumberSlider("Blocked tiles", generator.blockedSquareCount, 0, 12);
        GUILayout.Space(4);
        GUILayout.Label("Extra doors: " + Mathf.RoundToInt(generator.extraDoorChance * 100) + "%");
        generator.extraDoorChance = GUILayout.HorizontalSlider(generator.extraDoorChance, 0f, 1f);
        GUILayout.Label("Secret doors among extra doors: " + Mathf.RoundToInt(generator.secretDoorChance * 100) + "%");
        generator.secretDoorChance = GUILayout.HorizontalSlider(generator.secretDoorChance, 0f, 1f);

        generator.placeStairs = GUILayout.Toggle(generator.placeStairs, "2 x 2 starting stairs");
        generator.placeObjectiveChest = GUILayout.Toggle(generator.placeObjectiveChest, "Place quest objective chest (extra)");
        generator.respectPhysicalPieceLimits = GUILayout.Toggle(generator.respectPhysicalPieceLimits, "Respect classic piece counts");
        generator.includeQuestBosses = GUILayout.Toggle(generator.includeQuestBosses, "Allow random bosses (not classic default)");
        generator.hideUnexploredRoomContents = GUILayout.Toggle(generator.hideUnexploredRoomContents, "Hide room contents until opened");
        generator.revealTrapsForTesting = GUILayout.Toggle(generator.revealTrapsForTesting, "Show hidden traps (testing only)");

        GUILayout.BeginHorizontal();
        GUILayout.Label("Seed", GUILayout.Width(42));
        seedText = GUILayout.TextField(seedText, GUILayout.Width(165));
        if (GUILayout.Button("Random", GUILayout.Width(85)))
            seedText = Random.Range(1, int.MaxValue).ToString();
        GUILayout.EndHorizontal();
        GUILayout.Space(6);

        if (GUILayout.Button("Generate quest", GUILayout.Height(36)))
        {
            int newSeed;
            if (int.TryParse(seedText, out newSeed))
            {
                generator.seed = newSeed;
                if (campaign != null) campaign.ClearCampaign();
                generator.Generate();
                message = generator.lastSummary;
            }
            else message = "Seed must be a whole number.";
        }
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear")) { generator.ClearGenerated(); message = "Quest cleared."; }
        if (GUILayout.Button("Hide")) menuVisible = false;
        GUILayout.EndHorizontal();
        GUILayout.Space(7);
        GUILayout.Label("DEBUG VIEW (not hero rules)");
        if (GUILayout.Button("Reveal all rooms for testing")) generator.RevealAllRoomsForTesting();
        if (GUILayout.Button("Discover secret doors for testing")) generator.DiscoverAllSecretDoorsForTesting();
        if (GUILayout.Button("Reveal traps for testing")) generator.RevealAllTrapsForTesting();
        GUILayout.Space(5);
        GUILayout.Label(string.IsNullOrEmpty(message) ? generator.lastSummary : message);
        GUILayout.Label("Ordinary doors are visible and closed; secret doors look like walls.");
        GUILayout.Label("Room contents and undiscovered traps stay hidden in play.");
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawCampaign()
    {
        if (campaign == null)
        {
            GUILayout.Label("HQCampaignQuestLoader absent sur Plateau.");
            return;
        }
        GUILayout.Label("14 quetes du Quest Book - fichiers TXT fixes (pas de PCG)");
        string[] titles = {
            "01 - The Trial", "02 - Rescue of Sir Ragnar", "03 - Orc Warlord",
            "04 - Prince Magnus' Gold", "05 - Melar's Maze", "06 - Orc Warlord Legacy",
            "07 - The Lost Wizard", "08 - The Fire Mage", "09 - Race Against Time",
            "10 - Castle of Mystery", "11 - Bastion of Dread", "12 - Barak Tor",
            "13 - Spirit Blade", "14 - Return to Barak Tor"
        };
        int choice = GUILayout.SelectionGrid(campaign.selectedQuest - 1, titles, 2);
        if (choice >= 0) campaign.selectedQuest = choice + 1;
        campaign.showReferenceMap = GUILayout.Toggle(campaign.showReferenceMap,
            "Afficher le plan original de la quete");
        if (GUILayout.Button("Charger la quete choisie", GUILayout.Height(36)))
        {
            campaign.LoadSelectedQuest();
            message = campaign.loadReport;
        }
        if (GUILayout.Button("Effacer la campagne")) campaign.ClearCampaign();
        GUILayout.Space(4);
        campaign.hideUnexploredRoomContents = GUILayout.Toggle(campaign.hideUnexploredRoomContents,
            "Cacher le contenu des salles fermees (au prochain chargement)");
        GUILayout.Label("Objets des salles non decouvertes caches (y compris quetes sans portes).");
        bool rock = GUILayout.Toggle(campaign.showRockZones, "Afficher la roche pleine (blocs sombres)");
        if (rock != campaign.showRockZones) campaign.SetRockZones(rock);
        GUILayout.Label("DEBUG VIEW (pas les regles du jeu)");
        if (GUILayout.Button("Reveler toutes les salles (test)")) campaign.RevealAllRoomsForTesting();
        if (GUILayout.Button("Decouvrir portes secretes (test)")) campaign.DiscoverAllSecretDoorsForTesting();
        if (GUILayout.Button("Reveler les pieges (test)")) campaign.RevealAllTrapsForTesting();
        GUILayout.Space(4);
        GUILayout.Label(campaign.loadReport);
        GUILayout.Label("IMPORTANT : les quetes marquees PARTIAL ou REFERENCE ONLY " +
                        "ne sont PAS integralement retranscrites.");
        if (campaign.showReferenceMap)
        {
            Texture2D map = campaign.ReferenceMap;
            if (map != null)
                GUILayout.Label(map, GUILayout.Width(335), GUILayout.Height(252));
            else GUILayout.Label("Image de reference introuvable.");
        }
    }

    private int NumberSlider(string title, int current, int min, int max)
    {
        GUILayout.Label(title + ": " + current);
        return Mathf.RoundToInt(GUILayout.HorizontalSlider(current, min, max));
    }
}
