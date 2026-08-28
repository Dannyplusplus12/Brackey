using UnityEngine;
using UnityEngine.UI;

// 1 trong 4 ô item rao bán trong Shop (cột dọc bên trái). slotIndex khớp vị trí trong
// ShopOfferManager - gán tay trong Inspector cho từng ô (0-3).
public class ShopOfferSlotUI : MonoBehaviour, IItemSlot
{
    [SerializeField] int   slotIndex;
    [SerializeField] Image cardBg;   // Image nền thẻ — đổi sprite theo ItemType
    [SerializeField] Image icon;     // Icon item (đặt đè lên cardBg)

    [Header("Card sprites theo loại item")]
    [SerializeField] Sprite cardActive;
    [SerializeField] Sprite cardStatBoost;
    [SerializeField] Sprite cardCharacter;
    [SerializeField] Sprite cardEmpty;   // (tuỳ chọn) sprite hiện khi ô trống

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

        // Icon — disabled nếu không có sprite để tránh hiện hình trắng
        icon.sprite  = item?.icon;
        icon.enabled = item != null && item.icon != null;

        // Card background
        if (cardBg != null)
        {
            if (item != null)
            {
                cardBg.sprite  = GetCardSprite(item.itemType);
                cardBg.enabled = true;
            }
            else
            {
                cardBg.sprite  = cardEmpty;
                cardBg.enabled = cardEmpty != null;
            }
        }
    }

    Sprite GetCardSprite(ItemType type) => type switch
    {
        ItemType.Active    => cardActive,
        ItemType.StatBoost => cardStatBoost,
        ItemType.Character => cardCharacter,
        _                  => null,
    };

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
