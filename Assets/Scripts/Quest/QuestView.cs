using System.Collections.Generic;
using UnityEngine;

// affiche le contenu d'une quete a partir du GameState (aucune logique ici)
// a mettre sur le meme GameObject que le BoardBuilder
// laisser un prefab vide = cube de couleur
public class QuestView : MonoBehaviour
{
    public BoardBuilder board;

    public GameObject doorPrefab;
    public GameObject secretDoorPrefab;
    public GameObject blockedPrefab;
    public GameObject stairsPrefab;
    public Texture2D stairsTexture;   // optionnel : image de l'escalier (sinon Resources/HeroQuest/StairsTexture.png)
    public GameObject allyPrefab;
    public GameObject trapPrefab;
    public FurniturePrefab[] furniturePrefabs;
    public MonsterPrefab[] monsterPrefabs;

    public float doorHeight = 2f;
    public float doorThickness = 0.3f;
    public float blockedHeight = 2.5f;
    public float stairsHeight = 0.2f;
    public float furnitureHeight = 1f;
    public float monsterHeight = 1.5f;

    // ignore le brouillard pour verifier la quete complete
    public bool showAll;
    // montre les objets caches (portes secretes, monstres caches, pieges) pour verifier la quete
    public bool showHidden;

    public float trapHeight = 0.1f;

    GameState gameState;
    Transform root;

    // chaque objet pose et les cases qu'il couvre (pour le brouillard)
    Dictionary<GameObject, RectInt> placed = new Dictionary<GameObject, RectInt>();
    Dictionary<Monster, GameObject> monsterObjects = new Dictionary<Monster, GameObject>();
    Dictionary<Ally, GameObject> allyObjects = new Dictionary<Ally, GameObject>();
    Dictionary<Door, GameObject> doorObjects = new Dictionary<Door, GameObject>();
    Dictionary<Trap, GameObject> trapObjects = new Dictionary<Trap, GameObject>();
    Dictionary<Color, Material> colorMaterials = new Dictionary<Color, Material>();

    public void Show(GameState gameState)
    {
        Clear();
        this.gameState = gameState;

        root = new GameObject("Quest").transform;
        root.SetParent(transform, false);

        RectInt s = gameState.stairs;
        HQStairsVisual.SetTexture(stairsTexture);
        // l'image Resources/HeroQuest/StairsTexture.png est posee a plat sur l'escalier ; sans image, on garde le prefab
        if (!PlaceTexturedStairs(s, gameState.stairsRotation))
            Place("Stairs", stairsPrefab, s, stairsHeight, 0, Color.green);

        foreach (Door door in gameState.doorList) PlaceDoor(door);

        foreach (Tile tile in gameState.map.tiles)
        {
            if (tile.blocked)
                Place("Blocked", blockedPrefab, new RectInt(tile.x, tile.y, 1, 1), blockedHeight, 0, Color.gray);
        }

        foreach (Furniture f in gameState.furnitureList)
        {
            RectInt rect = new RectInt(f.x, f.y, f.width, f.height);
            Place(f.type.ToString(), FindFurniturePrefab(f.type), rect, furnitureHeight, f.rotation, new Color(0.9f, 0.7f, 0.1f));
        }

        foreach (Monster m in gameState.monsterList)
        {
            RectInt rect = new RectInt(m.position.x, m.position.y, 1, 1);
            GameObject go = Place(m.data.monsterClass.ToString(), FindMonsterPrefab(m.data.monsterClass), rect, monsterHeight, 0, Color.red);
            placed.Remove(go); // les monstres bougent : geres a part dans Refresh
            monsterObjects[m] = go;
        }

        foreach (Ally a in gameState.allyList)
        {
            RectInt rect = new RectInt(a.position.x, a.position.y, 1, 1);
            GameObject go = Place("Ally_" + a.id, allyPrefab, rect, monsterHeight, 0, Color.blue);
            placed.Remove(go); // les allies bougent aussi
            allyObjects[a] = go;
        }

        foreach (Trap t in gameState.trapList)
        {
            RectInt rect = new RectInt(t.x, t.y, 1, 1);
            GameObject go = Place("Trap_" + t.type, trapPrefab, rect, trapHeight, 0, new Color(1f, 0.5f, 0f));
            placed.Remove(go); // visibilite selon l'etat du piege : geree dans Refresh
            trapObjects[t] = go;
        }

        Refresh();
    }

    // met a jour la visibilite (brouillard), les portes et la position des monstres
    // a appeler quand une porte s'ouvre, qu'un heros regarde ou qu'un monstre bouge
    [ContextMenu("Refresh")]
    public void Refresh()
    {
        if (gameState == null) return;

        foreach (KeyValuePair<GameObject, RectInt> pair in placed)
            pair.Key.SetActive(showAll || IsRevealed(pair.Value));

        foreach (KeyValuePair<Monster, GameObject> pair in monsterObjects)
        {
            Vector2Int p = pair.Key.position;
            RectInt rect = new RectInt(p.x, p.y, 1, 1);
            pair.Value.transform.position = RectCenter(rect);
            pair.Value.SetActive(showAll || ((showHidden || !pair.Key.isHidden) && IsRevealed(rect)));
        }

        // un piege n'a une tuile sur le plateau que s'il s'est declenche
        foreach (KeyValuePair<Trap, GameObject> pair in trapObjects)
            pair.Value.SetActive(showHidden || (pair.Key.state == TrapState.Declenche
                && (showAll || IsRevealed(new RectInt(pair.Key.x, pair.Key.y, 1, 1)))));

        foreach (KeyValuePair<Ally, GameObject> pair in allyObjects)
        {
            Vector2Int p = pair.Key.position;
            RectInt rect = new RectInt(p.x, p.y, 1, 1);
            pair.Value.transform.position = RectCenter(rect);
            pair.Value.SetActive(showAll || IsRevealed(rect));
        }

        // une porte visible remplace son mur ; une porte cachee laisse le mur en place
        foreach (KeyValuePair<Door, GameObject> pair in doorObjects)
        {
            Door door = pair.Key;
            bool visible = (!door.isSecret || door.isFound || showHidden) && (showAll || IsDoorSeen(door));
            pair.Value.SetActive(visible);

            GameObject wall = board.GetWall(door.x, door.y, door.side);
            if (wall != null) wall.SetActive(!visible);
        }
    }

    // un monstre mort quitte le plateau
    public void RemoveMonster(Monster monster)
    {
        GameObject go;
        if (!monsterObjects.TryGetValue(monster, out go)) return;

        monsterObjects.Remove(monster);
        if (go != null) Destroy(go);
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        // on remet les murs caches par les portes
        foreach (Door door in doorObjects.Keys)
        {
            GameObject wall = board.GetWall(door.x, door.y, door.side);
            if (wall != null) wall.SetActive(true);
        }

        if (root != null)
        {
            if (Application.isPlaying) Destroy(root.gameObject);
            else DestroyImmediate(root.gameObject);
        }

        placed.Clear();
        monsterObjects.Clear();
        allyObjects.Clear();
        doorObjects.Clear();
        trapObjects.Clear();
        gameState = null;
    }

    void PlaceDoor(Door door)
    {
        GameObject wall = board.GetWall(door.x, door.y, door.side);
        if (wall == null)
        {
            Debug.LogWarning("Pas de mur pour la porte (" + door.x + ", " + door.y + ", " + door.side + ")");
            return;
        }

        GameObject prefab = door.isSecret ? secretDoorPrefab : doorPrefab;
        Color color = door.isSecret ? new Color(0.5f, 0.2f, 0.6f) : new Color(0.55f, 0.3f, 0.1f);
        GameObject go = CreateFitted("Door_" + door.x + "_" + door.y + "_" + door.side, prefab,
            new Vector3(board.cellSize, doorHeight, doorThickness), color);

        // la porte prend la place et l'orientation du mur
        go.transform.position = wall.transform.position;
        go.transform.rotation = wall.transform.rotation;
        doorObjects[door] = go;
    }

    // pose un objet sur un rectangle de cases ; rotation 90 ou 270 = objet tourne d'un quart de tour
    GameObject Place(string name, GameObject prefab, RectInt rect, float height, int rotation, Color color)
    {
        // le modele est mesure avant rotation : un objet tourne a ses cotes inverses
        bool turned = rotation == 90 || rotation == 270;
        float sizeX = (turned ? rect.height : rect.width) * board.cellSize;
        float sizeZ = (turned ? rect.width : rect.height) * board.cellSize;

        GameObject go = CreateFitted(name + "_" + rect.x + "_" + rect.y, prefab, new Vector3(sizeX, height, sizeZ), color);
        go.transform.position = RectCenter(rect);
        go.transform.rotation = Quaternion.Euler(0f, rotation, 0f);

        placed[go] = rect;
        return go;
    }

    // escalier = image a plat qui couvre toutes ses cases ; renvoie false si l'image est introuvable
    bool PlaceTexturedStairs(RectInt rect, int rotation)
    {
        GameObject go = HQStairsVisual.Create("Stairs_" + rect.x + "_" + rect.y,
            rect.width * board.cellSize, rect.height * board.cellSize, rotation);
        if (go == null) return false;

        go.transform.position = RectCenter(rect) + new Vector3(0f, HQStairsVisual.HeightAboveFloor, 0f);
        go.transform.SetParent(root, true);
        placed[go] = rect;
        return true;
    }

    // conteneur + modele ajuste a la taille "size", range sous la racine de la quete
    GameObject CreateFitted(string name, GameObject prefab, Vector3 size, Color color)
    {
        GameObject container = new GameObject(name);

        GameObject model;
        if (prefab != null) model = Instantiate(prefab);
        else
        {
            model = GameObject.CreatePrimitive(PrimitiveType.Cube);
            model.GetComponent<Renderer>().sharedMaterial = ColorMaterial(color);
        }
        model.transform.SetParent(container.transform, false);

        board.FitInBox(container, model, size);
        container.transform.SetParent(root, true);
        return container;
    }

    // centre d'un rectangle de cases, au niveau du sol
    Vector3 RectCenter(RectInt rect)
    {
        Vector3 topLeft = board.CellToWorld(rect.x, rect.y);
        return topLeft + new Vector3((rect.width - 1) * board.cellSize * 0.5f, 0f, -(rect.height - 1) * board.cellSize * 0.5f);
    }

    // vrai si au moins une case du rectangle est revelee
    bool IsRevealed(RectInt rect)
    {
        for (int y = rect.y; y < rect.y + rect.height; y++)
        {
            for (int x = rect.x; x < rect.x + rect.width; x++)
            {
                Tile t = gameState.map.Get(x, y);
                if (t != null && t.revealed) return true;
            }
        }
        return false;
    }

    // une porte est vue si une des deux cases qu'elle separe est revelee
    bool IsDoorSeen(Door door)
    {
        int nx = door.x;
        int ny = door.y;
        if (door.side == Side.North) ny--;
        if (door.side == Side.South) ny++;
        if (door.side == Side.East) nx++;
        if (door.side == Side.West) nx--;

        return IsRevealed(new RectInt(door.x, door.y, 1, 1)) || IsRevealed(new RectInt(nx, ny, 1, 1));
    }

    GameObject FindFurniturePrefab(FurnitureType type)
    {
        if (furniturePrefabs == null) return null;
        foreach (FurniturePrefab p in furniturePrefabs)
        {
            if (p.type == type) return p.prefab;
        }
        return null;
    }

    GameObject FindMonsterPrefab(MonsterClass type)
    {
        if (monsterPrefabs == null) return null;
        foreach (MonsterPrefab p in monsterPrefabs)
        {
            if (p.type == type) return p.prefab;
        }
        return null;
    }

    // un materiau par couleur pour les cubes de secours
    Material ColorMaterial(Color color)
    {
        Material m;
        if (colorMaterials.TryGetValue(color, out m) && m != null) return m;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        m = new Material(shader);
        m.color = color;
        colorMaterials[color] = m;
        return m;
    }
}
