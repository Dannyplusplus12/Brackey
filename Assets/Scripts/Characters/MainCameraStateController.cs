using UnityEngine;

// Camera duy nhất, luôn Viewport Rect = full screen (0,0,1,1).
// Khi Shop: zoom ra + dịch vị trí để arena hiện ở góc trên phải, ShopRoot bật lên.
// Khi Arena (Wave): zoom vào arena, ShopRoot tắt.
// Transition mượt qua Lerp trong Update.
//
// Setup:
// 1. Tạo 2 empty GameObject trong scene: ShopCamTarget và ArenaCamTarget
//    - ArenaCamTarget: đặt ở trung tâm arena, Z = -10
//    - ShopCamTarget: đặt lệch trái + xuống dưới so với arena, Z = -10
//      (để arena nằm ở góc trên phải trong viewport)
// 2. Gán vào Inspector, chỉnh orthographicSize 2 state, rồi fine-tune trong Play mode.
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
    [Tooltip("Tốc độ Lerp. Cao hơn = chuyển nhanh hơn.")]
    [SerializeField] float transitionSpeed = 4f;

    Vector3 _targetPos;
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

        _targetPos = isShop
            ? (shopCamTarget  != null ? shopCamTarget.position  : mainCamera.transform.position)
            : (arenaCamTarget != null ? arenaCamTarget.position : mainCamera.transform.position);

        _targetSize = isShop ? shopOrthographicSize : arenaOrthographicSize;
        mainCamera.rect = new Rect(0, 0, 1, 1);
    }

    void Update()
    {
        if (mainCamera == null) return;

        mainCamera.transform.position = Vector3.Lerp(
            mainCamera.transform.position, _targetPos, Time.deltaTime * transitionSpeed);

        mainCamera.orthographicSize = Mathf.Lerp(
            mainCamera.orthographicSize, _targetSize, Time.deltaTime * transitionSpeed);
    }

#if UNITY_EDITOR
    // Gizmo giúp visualize 2 camera target trong Scene view
    void OnDrawGizmosSelected()
    {
        if (arenaCamTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(arenaCamTarget.position, 0.5f);
            DrawCameraGizmo(arenaCamTarget.position, arenaOrthographicSize, Color.red);
            UnityEditor.Handles.Label(arenaCamTarget.position + Vector3.up * 1.5f, "Arena Cam");
        }

        if (shopCamTarget != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(shopCamTarget.position, 0.5f);
            DrawCameraGizmo(shopCamTarget.position, shopOrthographicSize, Color.cyan);
            UnityEditor.Handles.Label(shopCamTarget.position + Vector3.up * 1.5f, "Shop Cam");
        }
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
