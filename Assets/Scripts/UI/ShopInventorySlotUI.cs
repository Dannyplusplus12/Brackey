using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 1 trong 4 ô Inventory — dùng chung cho Shop lẫn Arena.
// Shop: left click chọn→swap, right click bán.
// Arena: left click kích hoạt item Active.
public class ShopInventorySlotUI : MonoBehaviour, IPointerClickHandler, IItemSlot
{
    [SerializeField] int   slotIndex;
    [SerializeField] Image cardBg;   // Image nền thẻ — đổi sprite theo ItemType
    [SerializeField] Image icon;     // Icon item (đặt đè lên cardBg)

    [Header("Card sprites theo loại item")]
    [SerializeField] Sprite cardActive;
    [SerializeField] Sprite cardStatBoost;
    [SerializeField] Sprite cardCharacter;
    [SerializeField] Sprite cardEmpty;

    [Header("Price Badge (sell)")]
    [SerializeField] GameObject priceBadge; // GO chứa Image nền + text giá bán — chỉ hiện trong Shop
    [SerializeField] TMP_Text   priceText;  // text hiển thị sellValue

    static ShopInventorySlotUI selected;

    void OnEnable()
    {
        PlayerInventory.OnInventoryChanged += Refresh;
        GameManager.OnGameStateChanged     += OnStateChanged;
        Refresh();
    }

    void OnDisable()
    {
        PlayerInventory.OnInventoryChanged -= Refresh;
        GameManager.OnGameStateChanged     -= OnStateChanged;
        if (selected == this) selected = null;
    }

    void OnStateChanged(GameState state) => Refresh();

    void Refresh()
    {
        ItemData item = PlayerInventory.Instance != null ? PlayerInventory.Instance.GetSlot(slotIndex) : null;

        // Icon — disabled nếu không có sprite để tránh hiện hình trắng
        icon.sprite  = item?.icon;
        icon.enabled = item != null && item.icon != null;

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

        // Price badge — chỉ hiện trong Shop và khi có item
        bool inShop = GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Shop;
        if (priceBadge != null)
            priceBadge.SetActive(item != null && inShop);
        if (priceText != null && item != null)
            priceText.text = item.sellValue.ToString();
    }

    Sprite GetCardSprite(ItemType type) => type switch
    {
        ItemType.Active    => cardActive,
        ItemType.StatBoost => cardStatBoost,
        ItemType.Character => cardCharacter,
        _                  => null,
    };

    public void OnPointerClick(PointerEventData eventData)
    {
        if (PlayerInventory.Instance == null) return;

        // Arena: left click = kích hoạt, right click bỏ qua
        bool inArena = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Arena;
        if (inArena)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                PlayerInventory.Instance.ActivateSlot(slotIndex);
            return;
        }

        // Shop: right click = bán, left click = swap
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            PlayerInventory.Instance.SellSlot(slotIndex);
            if (selected == this) selected = null;
            return;
        }

        if (selected == null)
        {
            selected = this;
            return;
        }

        if (selected != this)
            PlayerInventory.Instance.SwapSlots(selected.slotIndex, slotIndex);

        selected = null;
    }

    // ── IItemSlot ─────────────────────────────────────────────────────────────
    public ItemData GetCurrentItem() =>
        PlayerInventory.Instance != null ? PlayerInventory.Instance.GetSlot(slotIndex) : null;

    // Inventory slot → có thể bán → hiện sell value trong tooltip
    public bool IsSellable => true;
}
