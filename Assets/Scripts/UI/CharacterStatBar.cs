using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Thanh stats cuối RosterPanel — hiện khi hover vào 1 entry, ẩn khi rời.
// Setup:
//   1. Tạo GO "StatBar" ở cuối RosterPanel, add HorizontalLayoutGroup.
//   2. Tạo 4 child GO, mỗi cái có Image (icon) + TextMeshProUGUI (giá trị).
//   3. Wire 4 slot vào Inspector, gán icons HP/Damage/Speed/Angry.
[System.Serializable]
public struct StatSlotUI
{
    public Image     icon;
    public TextMeshProUGUI label;
}

public class CharacterStatBar : MonoBehaviour
{
    public static CharacterStatBar Instance { get; private set; }

    [Header("Icons (gán sprite vào đây)")]
    [SerializeField] Sprite iconHP;
    [SerializeField] Sprite iconDamage;
    [SerializeField] Sprite iconSpeed;
    [SerializeField] Sprite iconAngry;

    [Header("Slots (thứ tự: HP, Damage, Speed, Angry)")]
    [SerializeField] StatSlotUI[] slots = new StatSlotUI[4];

    void Awake()
    {
        Instance = this;
        gameObject.SetActive(false);
    }

    // Dùng trong Shop: hiện base stats, angry base / maxAngry
    public void ShowBase(CharacterStats s)
    {
        gameObject.SetActive(true);
        SetSlot(0, iconHP,     FormatStat(s.maxHP),              "HP",    s.maxHP.ToString("0"));
        SetSlot(1, iconDamage, FormatStat(s.damage),             "DMG",   s.damage.ToString("0"));
        SetSlot(2, iconSpeed,  FormatStat(s.moveSpeed),          "SPD",   s.moveSpeed.ToString("0.#"));
        SetSlot(3, iconAngry,  $"{FormatStat(s.angryOnRoundStart)}/{FormatStat(s.maxAngry)}", "ANGRY",
                               $"{s.angryOnRoundStart:0}/{s.maxAngry:0}");
    }

    // Dùng trong Roster: hiện HP hiện tại + effective stats (base + item bonus)
    public void ShowLive(CharacterBase character)
    {
        gameObject.SetActive(true);
        var   s      = character.Stats;
        float maxHP  = character.MaxHP;
        float dmg    = character.LiveDamage;
        float spd    = character.LiveSpeed;
        SetSlot(0, iconHP,     $"{FormatStat(character.CurrentHP)}/{FormatStat(maxHP)}", "HP",
                               $"{character.CurrentHP:0}/{maxHP:0}");
        SetSlot(1, iconDamage, FormatStat(dmg),  "DMG", dmg.ToString("0"));
        SetSlot(2, iconSpeed,  FormatStat(spd),  "SPD", spd.ToString("0.#"));
        SetSlot(3, iconAngry,  $"{FormatStat(character.CurrentAngry)}/{FormatStat(s.maxAngry)}", "ANGRY",
                               $"{character.CurrentAngry:0}/{s.maxAngry:0}");
    }

    public void Hide() => gameObject.SetActive(false);

    // ─────────────────────────────────────────────────────────────────────────
    // label: tên hiện khi không có icon, hoặc luôn hiện phía trên nếu muốn 2 dòng
    // value: số / giá trị hiển thị
    void SetSlot(int i, Sprite sprite, string value, string abbrev, string displayValue = null)
    {
        if (i >= slots.Length) return;
        var slot = slots[i];

        if (slot.icon != null)
        {
            slot.icon.sprite  = sprite;
            slot.icon.enabled = sprite != null;
        }

        if (slot.label != null)
        {
            slot.label.color = new Color(0.15f, 0.15f, 0.15f); // dark — đọc được trên nền sáng
            string val = displayValue ?? value;
            // Format 2 dòng: tên viết tắt trên, số dưới
            slot.label.text = sprite != null
                ? $"<size=70%>{abbrev}</size>\n{val}"
                : $"{abbrev}\n{val}";
        }
    }

    static string FormatStat(float v) =>
        (v % 1f == 0f) ? v.ToString("0") : v.ToString("0.#");
}
