using UnityEngine;
using UnityEngine.UI;

// 1 trong 4 ô item rao bán trong Shop (cột dọc bên trái). slotIndex khớp vị trí trong
// ShopOfferManager - gán tay trong Inspector cho từng ô (0-3).
public class ShopOfferSlotUI : MonoBehaviour, IItemSlot
{
    [SerializeField] int slotIndex;
    [SerializeField] Image icon;

    void OnEnable()
    {
        ShopOfferManager.OnOffersChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        ShopOfferManager.OnOffersChanged -= Refresh;
    }

    void Refresh()
    {
        ItemData item = ShopOfferManager.Instance != null ? ShopOfferManager.Instance.GetOffer(slotIndex) : null;
        icon.sprite = item != null ? item.icon : null;
        icon.enabled = item != null;
    }

    // Gán vào OnClick() của Button trên cùng GameObject trong Inspector.
    public void OnClickBuy()
    {
        ShopOfferManager.Instance?.BuyOffer(slotIndex);
    }

    // ── IItemSlot ─────────────────────────────────────────────────────────────
    public ItemData GetCurrentItem() =>
        ShopOfferManager.Instance != null ? ShopOfferManager.Instance.GetOffer(slotIndex) : null;

    // Offer slot chưa mua → không có sell value
    public bool IsSellable => false;
}
