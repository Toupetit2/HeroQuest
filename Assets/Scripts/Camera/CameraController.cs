using UnityEngine;

// Camera orbitale semi libre autour du plateau.
// Clic droit + glisser : tourner / incliner
// Clic molette + glisser, ou ZQSD / fleches : deplacer
// Molette : zoom / dezoom
// L'inclinaison minimale empeche la camera de passer sous le plateau.
public class CameraController : MonoBehaviour
{
    public BoardBuilder board;

    [Header("Zoom")]
    public float distance = 25f;
    public float minDistance = 5f;
    public float maxDistanceFactor = 0.6f; // distance max = plus grand cote du plateau x ce facteur
    public float zoomSpeed = 4f;

    [Header("Rotation")]
    public float yaw = 0f;
    public float pitch = 55f;
    public float minPitch = 15f;   // > 0 : jamais sous le plateau
    public float maxPitch = 89f;
    public float rotateSpeed = 3f;

    [Header("Deplacement")]
    public float keyPanSpeed = 0.8f;   // multiplie par la distance
    public float dragPanSpeed = 0.004f; // multiplie par la distance

    Vector3 target;
    bool initialized;

    void Start()
    {
        if (board == null) board = FindObjectOfType<BoardBuilder>();
        if (board == null) Debug.LogError("CameraController : aucun BoardBuilder trouve dans la scene.");
    }

    void LateUpdate()
    {
        if (board == null) return;

        // board.map n'est pas serialise : il est vide au lancement tant que Generate() n'a pas ete appele
        if (board.map == null)
        {
            if (board.boardFile == null) return;
            board.Generate();
        }
        if (board.map == null) return;

        if (!initialized)
        {
            target = BoardCenter();
            initialized = true;
        }

        HandleRotation();
        HandleZoom();
        HandlePan();
        Apply();
    }

    void HandleRotation()
    {
        if (!Input.GetMouseButton(1)) return;
        yaw += Input.GetAxis("Mouse X") * rotateSpeed;
        pitch -= Input.GetAxis("Mouse Y") * rotateSpeed;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.0001f) return;
        distance = Mathf.Clamp(distance - scroll * zoomSpeed * distance, minDistance, MaxDistance());
    }

    void HandlePan()
    {
        // axes horizontaux de la camera projetes sur le plan du plateau
        Quaternion flat = Quaternion.Euler(0f, yaw, 0f);
        Vector3 right = flat * Vector3.right;
        Vector3 forward = flat * Vector3.forward;

        Vector3 move = Vector3.zero;
        move += right * Input.GetAxisRaw("Horizontal") * keyPanSpeed * distance * Time.deltaTime;
        move += forward * Input.GetAxisRaw("Vertical") * keyPanSpeed * distance * Time.deltaTime;

        if (Input.GetMouseButton(2))
        {
            move -= right * Input.GetAxis("Mouse X") * dragPanSpeed * distance;
            move -= forward * Input.GetAxis("Mouse Y") * dragPanSpeed * distance;
        }

        target += move;
        target = ClampToBoard(target);
    }

    // distance max basee sur la taille du plateau
    float MaxDistance()
    {
        float side = Mathf.Max(board.map.width, board.map.height) * board.cellSize;
        return Mathf.Max(minDistance + 1f, side * maxDistanceFactor);
    }

    void Apply()
    {
        distance = Mathf.Clamp(distance, minDistance, MaxDistance());
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        transform.rotation = rot;
        transform.position = target + rot * new Vector3(0f, 0f, -distance);
    }

    // coins du plateau en monde (meme repere que BoardBuilder.CellToWorld)
    Vector3 BoardCenter()
    {
        Vector3 a = board.CellToWorld(0, 0);
        Vector3 b = board.CellToWorld(board.map.width - 1, board.map.height - 1);
        return (a + b) * 0.5f;
    }

    Vector3 ClampToBoard(Vector3 p)
    {
        Vector3 a = board.CellToWorld(0, 0);
        Vector3 b = board.CellToWorld(board.map.width - 1, board.map.height - 1);
        float h = board.cellSize * 0.5f;
        p.x = Mathf.Clamp(p.x, Mathf.Min(a.x, b.x) - h, Mathf.Max(a.x, b.x) + h);
        p.z = Mathf.Clamp(p.z, Mathf.Min(a.z, b.z) - h, Mathf.Max(a.z, b.z) + h);
        p.y = board.transform.position.y;
        return p;
    }
}