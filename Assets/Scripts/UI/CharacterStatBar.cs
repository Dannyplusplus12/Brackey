using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Thanh stats trong tooltip — hiện khi hover vào roster entry hoặc shop item, ẩn khi rời.
// Slots: 0=HP, 1=DMG, 2=ATKSPD, 3=ANGRY, 4=FOOD
// Setup: chạy Tools > Shop-Arena Setup > Build StatBar In TooltipPanel.
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
    [SerializeField] Sprite iconAtkSpeed;
    [SerializeField] Sprite iconAngry;
    [SerializeField] Sprite iconFood;

    [Header("Slots (thứ tự: HP, Damage, AtkSpeed, Angry, Food)")]
    [SerializeField] StatSlotUI[] slots = new StatSlotUI[5];

    void Awake()
    {
        Instance = this;
        gameObject.SetActive(false);
    }

    // Dùng trong Shop: hiện base stats (từ CharacterStats SO — bất biến)
    public void ShowBase(CharacterStats s)
    {
        gameObject.SetActive(true);
        SetSlot(0, iconHP,     FormatStat(s.maxHP),              "HP",    s.maxHP.ToString("0"));
        SetSlot(1, iconDamage, FormatStat(s.damage),             "DMG",   s.damage.ToString("0"));
        SetSlot(2, iconAtkSpeed, FormatStat(s.attackSpeed),       "ATKSPD", s.attackSpeed.ToString("0.##"));
        SetSlot(3, iconAngry,  $"{FormatStat(s.angryOnRoundStart)}/{FormatStat(s.maxAngry)}", "ANGRY",
                               $"{s.angryOnRoundStart:0}/{s.maxAngry:0}");
        SetSlot(4, iconFood,   s.foodRequiredPerRound.ToString(), "FOOD");
    }

    // Dùng trong Roster: hiện trạng thái thực tế (HP live + effective stats có item bonus)
    public void ShowLive(CharacterBase character)
    {
        gameObject.SetActive(true);
        var   s     = character.Stats;
        float maxHP = character.MaxHP;
        float dmg   = character.LiveDamage;
        float spd   = character.LiveSpeed;
        SetSlot(0, iconHP,     $"{FormatStat(character.CurrentHP)}/{FormatStat(maxHP)}", "HP",
                               $"{character.CurrentHP:0}/{maxHP:0}");
        SetSlot(1, iconDamage, FormatStat(dmg),  "DMG", dmg.ToString("0"));
        SetSlot(2, iconAtkSpeed, FormatStat(character.EffectiveAPS), "ATKSPD", character.EffectiveAPS.ToString("0.##"));
        SetSlot(3, iconAngry,  $"{FormatStat(character.CurrentAngry)}/{FormatStat(s.maxAngry)}", "ANGRY",
                               $"{character.CurrentAngry:0}/{s.maxAngry:0}");
        SetSlot(4, iconFood,   character.EffectiveFoodCost.ToString(), "FOOD");
    }

    public void Hide() => gameObject.SetActive(false);

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
