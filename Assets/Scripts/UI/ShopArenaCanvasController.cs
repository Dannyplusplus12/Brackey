using UnityEngine;

/// <summary>
/// Bật ShopRoot / ArenaRoot theo GameManager.CurrentState.
///
/// alwaysVisible[] : các panel luôn hiển thị bất kể state (ví dụ: ShopInventoryBar, StaticItemBox).
///   - QUAN TRỌNG: các GO này phải là con trực tiếp của Canvas (hoặc bất kỳ GO nào luôn active),
///     KHÔNG phải con của ShopRoot / ArenaRoot — vì parent.SetActive(false) ẩn hết children.
///   - Nếu chúng đang nằm trong ShopRoot: kéo ra thành sibling của ShopRoot trong Hierarchy,
///     rồi kéo vào array alwaysVisible ở đây.
/// </summary>
public class ShopArenaCanvasController : MonoBehaviour
{
    [SerializeField] GameObject shopRoot;
    [SerializeField] GameObject arenaRoot;

    [Tooltip("Các panel luôn hiển thị (kể cả Arena). Phải là con của Canvas, KHÔNG phải con của ShopRoot.")]
    [SerializeField] GameObject[] alwaysVisible;

    void OnEnable()
    {
        GameManager.OnGameStateChanged += HandleStateChanged;
        if (GameManager.Instance != null) HandleStateChanged(GameManager.Instance.CurrentState);
        else EnsureAlwaysVisible(); // hiện ngay kể cả chưa có state
    }

    void OnDisable()
    {
        GameManager.OnGameStateChanged -= HandleStateChanged;
    }

    void HandleStateChanged(GameState state)
    {
        if (shopRoot  != null) shopRoot .SetActive(state == GameState.Shop);
        if (arenaRoot != null) arenaRoot.SetActive(state == GameState.Arena);
        EnsureAlwaysVisible();
    }

    void EnsureAlwaysVisible()
    {
        if (alwaysVisible == null) return;
        foreach (var go in alwaysVisible)
            if (go != null) go.SetActive(true);
    }
}
