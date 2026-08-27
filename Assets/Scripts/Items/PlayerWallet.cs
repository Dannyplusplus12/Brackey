using UnityEngine;

// Quản lý đơn vị tiền tệ "Corn" — dùng để mua item trong shop VÀ feed quân mỗi round.
// Earn: thắng wave + giết địch (xem CharacterStats.killReward) + item đặc biệt.
// TrySpend: trả false nếu không đủ, không trừ gì cả.
public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet Instance { get; private set; }
    public static event System.Action OnCornChanged;

    [Tooltip("Lượng corn ban đầu khi bắt đầu game")]
    [SerializeField] int startingCorn = 15;

    public int Corn { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Corn = startingCorn;
    }

    public void Earn(int amount)
    {
        if (amount <= 0) return;
        Corn += amount;
        OnCornChanged?.Invoke();
    }

    // Trừ corn nếu đủ. Trả true = thành công, false = không đủ (không trừ gì).
    public bool TrySpend(int amount)
    {
        if (amount <= 0) return true;
        if (Corn < amount) return false;
        Corn -= amount;
        OnCornChanged?.Invoke();
        return true;
    }
}
