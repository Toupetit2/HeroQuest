using UnityEngine;

public class MouseInput : MonoBehaviour
{
    public BoardBuilder board;
    public Camera cam;

    // resultat du dernier clic : { "x", "y" }
    public string[] clickedTile = new string[2];

    void Start()
    {
        if (cam == null) cam = Camera.main;
        if (board == null) board = FindObjectOfType<BoardBuilder>();
        if (cam == null) Debug.LogError("MouseInput : aucune camera (mets le tag MainCamera ou renseigne le champ cam).");
        if (board == null) Debug.LogError("MouseInput : aucun BoardBuilder trouve dans la scene.");
    }

    void Update()
    {
        if (!ClickPressed() || board == null || cam == null) return;
        if (board.map == null)
        {
            Debug.LogWarning("MouseInput : le plateau n'est pas encore genere (board.map est null).");
            return;
        }

        // plan horizontal a la hauteur du plateau : pas besoin de collider
        Ray ray = cam.ScreenPointToRay(MousePosition());
        Plane ground = new Plane(Vector3.up, board.transform.position);
        if (!ground.Raycast(ray, out float dist)) return;

        Vector3 p = ray.GetPoint(dist) - board.transform.position;

        // inverse de BoardBuilder.CellToWorld
        int x = Mathf.RoundToInt(p.x / board.cellSize);
        int y = Mathf.RoundToInt(-p.z / board.cellSize);

        Tile t = board.map.Get(x, y);
        if (t == null) return; // clic hors du plateau

        clickedTile = new string[] { t.x.ToString(), t.y.ToString() };
        Debug.Log("Tile cliquee : X=" + clickedTile[0] + " Y=" + clickedTile[1] + " (zone " + t.zone + ")");
    }

    bool ClickPressed()
    {
        return Input.GetMouseButtonDown(0);
    }

    Vector3 MousePosition()
    {
        return Input.mousePosition;
    }
}