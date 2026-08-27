using UnityEngine;
using UnityEngine.UI;

// Gắn lên Button "Bắt đầu Wave" trong ShopRoot.
// Click → GameManager.EnterArena(). Button tự disable trong Arena để tránh spam.
// Visibility của ShopRoot đã do ShopArenaCanvasController quản lý — script này
// chỉ lo phần tương tác.
[RequireComponent(typeof(Button))]
public class StartWaveButton : MonoBehaviour
{
    Button btn;

    void Awake()
    {
        btn = GetComponent<Button>();
        btn.onClick.AddListener(OnClick);
    }

    void OnEnable()
    {
        GameManager.OnGameStateChanged += OnStateChanged;
        SyncState();
    }

    void OnDisable()
    {
        GameManager.OnGameStateChanged -= OnStateChanged;
    }

    void OnStateChanged(GameState state) => SyncState();

    void SyncState()
    {
        if (btn == null) return;
        bool isShop = GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Shop;
        btn.interactable = isShop;
    }

    void OnClick()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.EnterArena();
    }
}
