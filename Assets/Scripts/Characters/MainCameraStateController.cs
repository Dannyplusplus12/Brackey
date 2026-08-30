using UnityEngine;

// Camera duy nhất, luôn Viewport Rect = full screen (0,0,1,1).
// Khi Shop: zoom ra + dịch vị trí để arena hiện ở góc trên phải, ShopRoot bật lên.
// Khi Arena (Wave): zoom vào arena, ShopRoot tắt.
// Transition mượt qua Lerp trong Update.
//
// Mouse Follow: camera lerp về (baseTarget + mouseOffset), trong đó mouseOffset bị
// clamp trong hình chữ nhật [±mouseOffsetMax]. Gizmo màu vàng visualize vùng này.
public class MainCameraStateController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Camera mainCamera;
    [SerializeField] GameObject shopRoot;

    [Header("Arena State (Wave)")]
    [SerializeField] Transform arenaCamTarget;
    [SerializeField] float arenaOrthographicSize = 5f;

    [Header("Shop State")]
    [SerializeField] Transform shopCamTarget;
    [SerializeField] float shopOrthographicSize = 9f;

    [Header("Transition")]
    [Tooltip("Tốc độ Lerp camera về target. Cao hơn = chuyển nhanh hơn.")]
    [SerializeField] float transitionSpeed = 4f;

    [Header("Mouse Follow")]
    [Tooltip("Vùng chết (units) quanh tâm — khi chuột ở trong này camera không di chuyển. " +
             "Gizmo màu trắng trong Scene view.")]
    [SerializeField] Vector2 safeZone = new Vector2(1f, 0.75f);
    [Tooltip("Khoảng tối đa (units) camera bị kéo theo chuột theo mỗi trục. " +
             "Gizmo màu vàng trong Scene view.")]
    [SerializeField] Vector2 mouseOffsetMax = new Vector2(2f, 1.5f);

    // Vị trí gốc của state hiện tại (không bao gồm mouse offset)
    Vector3 _baseTargetPos;
    float _targetSize;

    void OnEnable()
    {
        GameManager.OnGameStateChanged += HandleStateChanged;
        if (GameManager.Instance != null)
            HandleStateChanged(GameManager.Instance.CurrentState);
    }

    void OnDisable()
    {
        GameManager.OnGameStateChanged -= HandleStateChanged;
    }

    void HandleStateChanged(GameState state)
    {
        bool isShop = state == GameState.Shop;
        if (shopRoot != null) shopRoot.SetActive(isShop);

        _baseTargetPos = isShop
            ? (shopCamTarget  != null ? shopCamTarget.position  : mainCamera.transform.position)
            : (arenaCamTarget != null ? arenaCamTarget.position : mainCamera.transform.position);

        _targetSize = isShop ? shopOrthographicSize : arenaOrthographicSize;
        mainCamera.rect = new Rect(0, 0, 1, 1);
    }

    void Update()
    {
        if (mainCamera == null) return;

        // ── Mouse offset (chỉ trong Arena) ───────────────────────────────────
        Vector3 finalTarget = _baseTargetPos;
        bool isArena = GameManager.Instance != null &&
                       GameManager.Instance.CurrentState == GameState.Arena;
        if (isArena)
        {
            float depth = Mathf.Abs(mainCamera.transform.position.z);
            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(
                new Vector3(Input.mousePosition.x, Input.mousePosition.y, depth));

            Vector2 rawOffset = (Vector2)mouseWorld - (Vector2)_baseTargetPos;

            // Dead zone: bên trong safeZone → offset = 0, bên ngoài → offset tăng dần từ 0
            Vector2 effectiveOffset = new Vector2(
                DeadZone(rawOffset.x, safeZone.x, mouseOffsetMax.x),
                DeadZone(rawOffset.y, safeZone.y, mouseOffsetMax.y));

            finalTarget = _baseTargetPos + new Vector3(effectiveOffset.x, effectiveOffset.y, 0f);
        }

        // ── Lerp vị trí + size ────────────────────────────────────────────────
        mainCamera.transform.position = Vector3.Lerp(
            mainCamera.transform.position, finalTarget, Time.deltaTime * transitionSpeed);

        mainCamera.orthographicSize = Mathf.Lerp(
            mainCamera.orthographicSize, _targetSize, Time.deltaTime * transitionSpeed);
    }

    // Trong dead zone → 0. Ngoài dead zone → tăng tuyến tính từ 0 đến (max - dead), clamp ở max.
    static float DeadZone(float raw, float dead, float max)
    {
        float abs = Mathf.Abs(raw);
        if (abs <= dead) return 0f;
        float range = max - dead;
        if (range <= 0f) return 0f;
        return Mathf.Sign(raw) * Mathf.Min(abs - dead, range);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (arenaCamTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(arenaCamTarget.position, 0.5f);
            DrawCameraGizmo(arenaCamTarget.position, arenaOrthographicSize, Color.red);
            UnityEditor.Handles.Label(arenaCamTarget.position + Vector3.up * 1.5f, "Arena Cam");
            DrawMouseOffsetGizmo(arenaCamTarget.position);
        }

        if (shopCamTarget != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(shopCamTarget.position, 0.5f);
            DrawCameraGizmo(shopCamTarget.position, shopOrthographicSize, Color.cyan);
            UnityEditor.Handles.Label(shopCamTarget.position + Vector3.up * 1.5f, "Shop Cam");
            // Không vẽ mouse offset gizmo cho Shop — tính năng chỉ hoạt động trong Arena
        }
    }

    // Gizmo vùng mouse follow: safe zone (trắng) + offset max (vàng)
    void DrawMouseOffsetGizmo(Vector3 center)
    {
        Vector3 c = new Vector3(center.x, center.y, 0f);

        // ── Outer: offset max (vàng) ──────────────────────────────────────────
        Vector3 outerSize = new Vector3(mouseOffsetMax.x * 2f, mouseOffsetMax.y * 2f, 0f);
        Gizmos.color = new Color(1f, 0.92f, 0f, 0.85f);
        Gizmos.DrawWireCube(c, outerSize);
        Gizmos.color = new Color(1f, 0.92f, 0f, 0.05f);
        Gizmos.DrawCube(c, outerSize);

        // ── Inner: safe zone (trắng) ──────────────────────────────────────────
        Vector3 innerSize = new Vector3(safeZone.x * 2f, safeZone.y * 2f, 0f);
        Gizmos.color = new Color(1f, 1f, 1f, 0.7f);
        Gizmos.DrawWireCube(c, innerSize);
        Gizmos.color = new Color(1f, 1f, 1f, 0.08f);
        Gizmos.DrawCube(c, innerSize);

        // ── Labels ────────────────────────────────────────────────────────────
        UnityEditor.Handles.color = new Color(1f, 1f, 1f, 0.8f);
        UnityEditor.Handles.Label(c + Vector3.right * safeZone.x + Vector3.up * 0.25f,
            $"safe ±({safeZone.x:F1}, {safeZone.y:F1})");

        UnityEditor.Handles.color = new Color(1f, 0.92f, 0f, 0.8f);
        UnityEditor.Handles.Label(c + Vector3.right * mouseOffsetMax.x + Vector3.up * 0.25f,
            $"max ±({mouseOffsetMax.x:F1}, {mouseOffsetMax.y:F1})");
    }

    void DrawCameraGizmo(Vector3 center, float orthoSize, Color color)
    {
        Gizmos.color = new Color(color.r, color.g, color.b, 0.2f);
        float aspect = mainCamera != null ? mainCamera.aspect : 16f / 9f;
        float h = orthoSize * 2f;
        float w = h * aspect;
        Gizmos.DrawWireCube(new Vector3(center.x, center.y, 0), new Vector3(w, h, 0));
    }
#endif
}
