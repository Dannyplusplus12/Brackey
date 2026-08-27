using UnityEngine;

// Bật ShopRoot / ArenaRoot theo GameManager.CurrentState. Gắn 1 lần lên Canvas, kéo 2 root
// tương ứng vào field bên dưới. Thay thế ArenaPreviewUI (giờ ArenaPreviewAnchor chỉ cần nằm
// trong ShopRoot là tự ẩn/hiện theo, không cần script riêng nữa).
public class ShopArenaCanvasController : MonoBehaviour
{
    [SerializeField] GameObject shopRoot;
    [SerializeField] GameObject arenaRoot;

    void OnEnable()
    {
        GameManager.OnGameStateChanged += HandleStateChanged;
        if (GameManager.Instance != null) HandleStateChanged(GameManager.Instance.CurrentState);
    }

    void OnDisable()
    {
        GameManager.OnGameStateChanged -= HandleStateChanged;
    }

    void HandleStateChanged(GameState state)
    {
        if (shopRoot == null || arenaRoot == null)
        {
            Debug.LogError("[ShopArenaCanvasController] shopRoot hoặc arenaRoot chưa được gán trong Inspector!", this);
            return;
        }
        shopRoot.SetActive(state == GameState.Shop);
        arenaRoot.SetActive(state == GameState.Arena);
    }
}
