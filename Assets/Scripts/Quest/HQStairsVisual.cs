using UnityEngine;

// image posee a plat sur l'escalier : Resources/HeroQuest/StairsTexture.png
// si l'image est absente, les scripts gardent l'ancien prefab d'escalier (et un avertissement apparait dans la Console)
public static class HQStairsVisual
{
    public const string TexturePath = "HeroQuest/StairsTexture";

    // l'image est posee un peu au-dessus du sol (dessus du sol = y 0) pour eviter le scintillement
    public const float HeightAboveFloor = 0.05f;

    static Texture2D texture;
    static Material material;
    static Mesh mesh;
    static bool warned;
    static bool logged;

    public static bool Available
    {
        get { return GetTexture() != null; }
    }

    // permet d'imposer une image depuis l'Inspector (champ "Stairs Texture" de QuestView)
    public static void SetTexture(Texture2D custom)
    {
        if (custom == null || custom == texture) return;
        texture = custom;
        material = null;
    }

    static Texture2D GetTexture()
    {
        if (texture != null) return texture;

        // 1. emplacement prevu
        texture = Resources.Load<Texture2D>(TexturePath);

        // 2. image importee en Sprite
        if (texture == null)
        {
            Sprite sprite = Resources.Load<Sprite>(TexturePath);
            if (sprite != null) texture = sprite.texture;
        }

        // 3. n'importe quelle image nommee StairsTexture ou Stairs dans un dossier Resources
        if (texture == null)
        {
            foreach (Texture2D t in Resources.LoadAll<Texture2D>(""))
            {
                if (t != null && (t.name == "StairsTexture" || t.name == "Stairs")) { texture = t; break; }
            }
        }

        if (texture != null) texture.wrapMode = TextureWrapMode.Clamp;
        else if (!warned)
        {
            warned = true;
            Debug.LogWarning("HQStairsVisual : image introuvable (Resources/" + TexturePath + ".png). L'ancien escalier est utilise.");
        }
        return texture;
    }

    static Material GetMaterial()
    {
        if (material != null) return material;

        Texture2D tex = GetTexture();
        if (tex == null) return null;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Texture");

        material = new Material(shader);
        material.name = "HQ_Stairs";
        material.mainTexture = tex;
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", tex);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0f);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0f);
        if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);   // visible des deux cotes
        return material;
    }

    // carre de 1 x 1 pose a plat (face vers le haut), le haut de l'image vers le nord (+Z)
    static Mesh GetMesh()
    {
        if (mesh != null) return mesh;

        mesh = new Mesh();
        mesh.name = "HQ_StairsQuad";
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f),
            new Vector3(0.5f, 0f, 0.5f), new Vector3(-0.5f, 0f, 0.5f)
        };
        mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
        mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
        mesh.triangles = new[] { 0, 3, 2, 0, 2, 1 };
        mesh.RecalculateBounds();
        return mesh;
    }

    // cree l'escalier a plat, centre sur l'origine ; l'appelant le positionne et le range
    // width / depth en unites du monde (nombre de cases x taille d'une case)
    // rotation : 0, 90, 180 ou 270, sens des aiguilles d'une montre vu de dessus
    // renvoie null si l'image est introuvable
    public static GameObject Create(string name, float width, float depth, int rotation = 0)
    {
        Material m = GetMaterial();
        if (m == null) return null;

        GameObject go = new GameObject(name);
        go.AddComponent<MeshFilter>().sharedMesh = GetMesh();
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = m;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        // un quart de tour echange largeur et profondeur : le rectangle de cases reste entierement couvert
        bool turned = rotation == 90 || rotation == 270;
        go.transform.localScale = turned ? new Vector3(depth, 1f, width) : new Vector3(width, 1f, depth);
        go.transform.rotation = Quaternion.Euler(0f, rotation, 0f);

        if (!logged)
        {
            logged = true;
            Debug.Log("HQStairsVisual : escalier affiche avec l'image " + texture.name + " (" + width + " x " + depth + ").");
        }
        return go;
    }
}
