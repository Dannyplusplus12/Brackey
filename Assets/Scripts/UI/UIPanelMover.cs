using UnityEngine;

// Gắn lên bất kỳ UI panel nào cần đổi vị trí khi chuyển state Shop ↔ Arena.
// Panel phải là con trực tiếp của Canvas (hoặc bất kỳ parent cố định nào).
//
// Workflow:
//   1. Kéo panel đến vị trí Shop → bấm "Capture Shop Position" trong Inspector
//   2. Kéo panel đến vị trí Arena → bấm "Capture Arena Position" trong Inspector
//   3. Đặt lại vị trí tuỳ ý — code tự snap đúng chỗ khi chuyển state
public class UIPanelMover : MonoBehaviour
{
    [Header("Saved Positions (anchoredPosition)")]
    public Vector2 shopPosition;
    public Vector2 arenaPosition;

    [Header("Transition")]
    [Tooltip("Snap ngay lập tức nếu = 0. Lerp nếu > 0 (giây).")]
    public float lerpDuration = 0f;

    RectTransform _rect;
    Vector2 _targetPos;
    bool _isLerping;
    float _lerpTime;
    Vector2 _lerpFrom;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        GameManager.OnGameStateChanged += OnStateChanged;
        // Áp vị trí đúng ngay khi enable (phòng khi panel bị disable rồi enable lại)
        ApplyState(GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.Shop, snap: true);
    }

    void OnDisable()
    {
        GameManager.OnGameStateChanged -= OnStateChanged;
    }

    void OnStateChanged(GameState state) => ApplyState(state, snap: lerpDuration <= 0f);

    void ApplyState(GameState state, bool snap)
    {
        _targetPos = state == GameState.Shop ? shopPosition : arenaPosition;

        if (snap || _rect == null)
        {
            if (_rect != null) _rect.anchoredPosition = _targetPos;
            _isLerping = false;
            return;
        }

        _lerpFrom = _rect.anchoredPosition;
        _lerpTime  = 0f;
        _isLerping = true;
    }

    void Update()
    {
        if (!_isLerping) return;

        _lerpTime += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(_lerpTime / lerpDuration);
        _rect.anchoredPosition = Vector2.Lerp(_lerpFrom, _targetPos, t);

        if (t >= 1f) _isLerping = false;
    }

#if UNITY_EDITOR
    // ── Editor helpers — hiện nút Capture trong Inspector ─────────────────────
    [ContextMenu("Capture Current → Shop Position")]
    void CaptureShop()
    {
        if (_rect == null) _rect = GetComponent<RectTransform>();
        shopPosition = _rect.anchoredPosition;
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[UIPanelMover] {name} Shop position: {shopPosition}");
    }

    [ContextMenu("Capture Current → Arena Position")]
    void CaptureArena()
    {
        if (_rect == null) _rect = GetComponent<RectTransform>();
        arenaPosition = _rect.anchoredPosition;
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[UIPanelMover] {name} Arena position: {arenaPosition}");
    }

    [ContextMenu("Preview Shop Position")]
    void PreviewShop()
    {
        if (_rect == null) _rect = GetComponent<RectTransform>();
        _rect.anchoredPosition = shopPosition;
    }

    [ContextMenu("Preview Arena Position")]
    void PreviewArena()
    {
        if (_rect == null) _rect = GetComponent<RectTransform>();
        _rect.anchoredPosition = arenaPosition;
    }
#endif
}
