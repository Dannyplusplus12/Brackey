using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Popup kết quả Gacha:
///   • Overlay xám nhẹ che toàn màn hình
///   • Card frame ở giữa (asset artist chuẩn bị)
///   • Hình nhân vật bên trong card
///   • Nút "Nhận" ở dưới
///   • Hover trên card → tooltip character (dùng ItemTooltipTrigger + IItemSlot)
///
/// Hierarchy tạo bởi GachaWheelSetupTool — không cần tạo tay.
/// </summary>
public class GachaResultPopup : MonoBehaviour, IItemSlot
{
    [Header("References")]
    [Tooltip("Panel card (Image frame) — ItemTooltipTrigger gắn ở đây")]
    public RectTransform cardPanel;

    [Tooltip("Image hình nhân vật bên trong card")]
    public Image charImage;

    [Tooltip("Text tên nhân vật (optional)")]
    public TMP_Text charNameText;

    [Tooltip("Nút Nhận")]
    public Button receiveButton;

    // ── Private ───────────────────────────────────────────────────────────────

    ItemData _wonItem;
    Action<ItemData> _onReceive;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        // Không SetActive(false) ở đây — object đã inactive từ Setup Tool.
        // Nếu Awake chạy khi inactive thì SetActive(false) sẽ hủy Show() ngay lập tức.
        if (receiveButton != null)
            receiveButton.onClick.AddListener(OnClickReceive);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Hiện popup với nhân vật vừa trúng.
    /// onReceive được gọi khi player bấm "Nhận".
    /// </summary>
    public void Show(ItemData wonItem, Action<ItemData> onReceive)
    {
        _wonItem   = wonItem;
        _onReceive = onReceive;

        // Lấy sprite idle của nhân vật
        Sprite portrait = null;
        if (wonItem?.characterPrefab != null)
        {
            var stats = wonItem.characterPrefab.GetComponent<CharacterBase>()?.Stats;
            if (stats != null) portrait = stats.idleSprite;
        }
        if (portrait == null && wonItem != null) portrait = wonItem.icon;

        if (charImage != null)
        {
            charImage.sprite  = portrait;
            charImage.enabled = portrait != null;
        }

        if (charNameText != null)
            charNameText.text = wonItem?.displayName ?? wonItem?.name ?? "";

        gameObject.SetActive(true);
    }

    // ── Button callback ───────────────────────────────────────────────────────

    void OnClickReceive()
    {
        gameObject.SetActive(false);
        _onReceive?.Invoke(_wonItem);
        _wonItem   = null;
        _onReceive = null;
    }

    // ── IItemSlot (cho ItemTooltipTrigger trên card) ──────────────────────────

    public ItemData GetCurrentItem() => _wonItem;
    public bool IsSellable => false;
}
