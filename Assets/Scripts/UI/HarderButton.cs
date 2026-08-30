using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Gắn lên Button "Harder" trong ShopRoot (bên cạnh nút Bắt đầu Wave).
// Click → LevelManager.SpawnHarder(): giữ nguyên quái hiện tại, spawn thêm quái từ level tiếp theo.
// Button tự disable khi:
//   - Đang ở Arena (không phải Shop)
//   - Không còn level tiếp theo
//   - Đã bấm Harder lần này rồi
// Label "HARDER (+X%)" hiển thị hard% của wave SẮP TỚI (sau khi StartWave bình thường).
[RequireComponent(typeof(Button))]
public class HarderButton : MonoBehaviour
{
    [Tooltip("(Tùy chọn) TextMeshPro để hiển thị 'HARDER (+X%)' — để trống nếu không cần label động")]
    [SerializeField] TMP_Text label;

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

    void OnStateChanged(GameState _) => SyncState();

    // Refresh mỗi frame trong Shop để bắt được thay đổi CanSpawnHarder ngay khi click
    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Shop)
            SyncState();
    }

    void SyncState()
    {
        if (btn == null) return;

        bool isShop = GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Shop;
        bool canHarder = isShop && LevelManager.Instance != null && LevelManager.Instance.CanSpawnHarder;

        btn.interactable = canHarder;

        if (label != null)
            label.text = "HARDER";
    }

    void OnClick()
    {
        LevelManager.Instance?.SpawnHarder();
        SyncState(); // disable ngay sau click
    }
}
