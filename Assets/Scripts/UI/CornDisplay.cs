using TMPro;
using UnityEngine;

// Gắn lên một TextMeshProUGUI — tự cập nhật khi corn thay đổi.
// Có thể đặt trong ShopRoot, ArenaRoot, hoặc thẳng trên Canvas (luôn hiện).
[RequireComponent(typeof(TextMeshProUGUI))]
public class CornDisplay : MonoBehaviour
{
    [Tooltip("Prefix hiện trước số. Ví dụ: '🌽 ' hoặc 'Corn: '")]
    [SerializeField] string prefix = "🌽 ";

    TextMeshProUGUI label;

    void Awake() => label = GetComponent<TextMeshProUGUI>();

    void OnEnable()
    {
        PlayerWallet.OnCornChanged += Refresh;
        Refresh();
    }

    void OnDisable() => PlayerWallet.OnCornChanged -= Refresh;

    void Refresh()
    {
        label.text = PlayerWallet.Instance != null
            ? $"{prefix}{PlayerWallet.Instance.Corn}"
            : $"{prefix}0";
    }
}
