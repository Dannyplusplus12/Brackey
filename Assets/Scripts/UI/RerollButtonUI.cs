using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Gắn lên GameObject chứa nút Reroll trong ShopOfferPanel.
// Hiển thị nhãn "ROLL" và badge giá reroll hiện tại (tự cập nhật sau mỗi lần roll).
[RequireComponent(typeof(Button))]
public class RerollButtonUI : MonoBehaviour
{
    [Header("Label")]
    [SerializeField] TMP_Text rollLabel;   // text "ROLL"

    [Header("Price Badge")]
    [SerializeField] GameObject priceBadge; // GO chứa Image nền + text giá
    [SerializeField] TMP_Text   priceText;  // hiển thị rerollCost hiện tại

    void OnEnable()
    {
        ShopOfferManager.OnRerollCostChanged += OnCostChanged;
        Refresh();
    }

    void OnDisable()
    {
        ShopOfferManager.OnRerollCostChanged -= OnCostChanged;
    }

    void OnCostChanged(int newCost) => SetCost(newCost);

    void Refresh()
    {
        int cost = ShopOfferManager.Instance != null
            ? ShopOfferManager.Instance.CurrentRerollCost
            : 0;
        SetCost(cost);
    }

    void SetCost(int cost)
    {
        if (priceText  != null) priceText.text = cost.ToString();
        if (priceBadge != null) priceBadge.SetActive(true);
    }
}
