# Tooltip & StatBar — Tài liệu tham khảo

## Cấu trúc scene (TooltipPanel)

```
Canvas
└── TooltipPanel          ← TooltipSystem.cs, VerticalLayoutGroup, ContentSizeFitter
    ├── Arrow             ← mũi tên chỉ hướng
    ├── DescText          ← TextMeshProUGUI — hiện item.description
    └── StatsRow          ← CharacterStatBar.cs, HorizontalLayoutGroup (bắt đầu ACTIVE)
        ├── SlotHP        ← Icon(Image) + Label(TMP)
        ├── SlotDMG
        ├── SlotSPD
        └── SlotAngry
```

**Lưu ý quan trọng:** StatsRow phải bắt đầu ACTIVE trong scene.
`CharacterStatBar.Awake()` tự gọi `Hide()` ngay → ẩn đúng cách + `Instance` được set.
Nếu GO bắt đầu inactive → `Awake` không chạy → `Instance = null` mãi mãi.

---

## TMP Rich Text — Copy-paste

### Màu sắc
```
<color=#4CAF50>xanh lá — buff tốt</color>
<color=#E82020>đỏ — debuff xấu</color>
<color=#FFD700>vàng — highlight / cost</color>
<color=#A0A0A0>xám — phụ / lore / note</color>
<color=#FF8C00>cam — cảnh báo</color>
<color=#00BFFF>xanh dương — kỹ năng</color>
<color=#FF69B4>hồng — đặc biệt / charm</color>
<color=#B48EE0>tím nhạt — ma thuật / rare</color>
```

### Sprite icon (phải có TMP Sprite Asset đúng tên)
```
<sprite name="stat_hp">
<sprite name="stat_damage">
<sprite name="stat_speed">
<sprite name="stat_angry">
<sprite name="stat_atkspeed">
<sprite name="stat_food">
<sprite name="coin">
```

### Format chữ
```
<b>đậm</b>
<i>nghiêng</i>
<size=80%>nhỏ hơn</size>
<size=120%>to hơn</size>
\n   ← xuống dòng
```

---

## Description mẫu — dán vào ItemData.description

### Nhân vật
```
<b>Chiến binh Viking</b>
<color=#A0A0A0><i>Từ vùng đất băng giá phương Bắc.</i></color>
```
*(Stat tự hiện trong StatsRow — không cần viết lại)*

### StatBoost — buff
```
Tăng <color=#4CAF50>+20</color> <sprite name="stat_hp">
Tăng <color=#4CAF50>+5</color> <sprite name="stat_damage">
```

### StatBoost — debuff
```
Giảm <color=#E82020>-10</color> <sprite name="stat_speed">
```

### Active item
```
<b>Kích hoạt:</b> Hồi <color=#4CAF50>50</color> <sprite name="stat_hp"> cho tất cả ally.
<color=#A0A0A0>Dùng 1 lần mỗi trận.</color>
```

### Mixed buff/debuff
```
<b>Bình máu rồng</b>
Hồi <color=#4CAF50>+30</color> <sprite name="stat_hp"> · Tăng <color=#4CAF50>+10</color> <sprite name="stat_damage">
Tốn <color=#FFD700>3</color> <sprite name="coin"> mỗi round
<color=#A0A0A0><i>Chế từ vảy rồng cổ đại.</i></color>
```

### Item có điều kiện angry
```
+<color=#E82020>2</color> <sprite name="stat_angry"> khi đói
+<color=#E82020>1</color> <sprite name="stat_angry"> khi 1 ally chết
```

---

## API CharacterStatBar

```csharp
// Shop: hiện base stats từ ScriptableObject
CharacterStatBar.Instance?.ShowBase(characterStats);

// Roster: hiện HP/Angry hiện tại từ CharacterBase đang chạy
CharacterStatBar.Instance?.ShowLive(characterBase);

// Ẩn (gọi trong OnPointerExit)
CharacterStatBar.Instance?.Hide();
```

---

## API TooltipSystem

```csharp
// Hiện ngay (không delay) — dùng cho roster hover
TooltipSystem.ShowImmediate(TooltipData.FromCharacterLive(character, rectTransform));

// Hiện có delay — dùng cho shop slot
TooltipSystem.Show(TooltipData.FromItem(item, rectTransform, direction, alignEnd, gap));

// Ẩn
TooltipSystem.Hide();
```

---

## Hover handlers — pattern chuẩn

### Roster entry (CharacterRosterEntry.cs)
```csharp
public void OnPointerEnter(PointerEventData _)
{
    if (target == null) return;
    TooltipSystem.ShowImmediate(TooltipData.FromCharacterLive(target, rectTransform));
    CharacterStatBar.Instance?.ShowLive(target);
}
public void OnPointerExit(PointerEventData _)
{
    TooltipSystem.Hide();
    CharacterStatBar.Instance?.Hide();
}
```

### Shop offer slot (ItemTooltipTrigger.cs)
```csharp
public void OnPointerEnter(PointerEventData eventData)
{
    var item = GetItem();
    if (item == null) return;

    if (item.itemType == ItemType.Character && item.characterPrefab != null)
    {
        var stats = item.characterPrefab.GetComponent<CharacterBase>()?.Stats;
        if (stats != null) CharacterStatBar.Instance?.ShowBase(stats);
    }

    if (!string.IsNullOrEmpty(item.description))
        TooltipSystem.Show(TooltipData.FromItem(item, _rect, direction, alignEnd, gap));
}
public void OnPointerExit(PointerEventData eventData)
{
    TooltipSystem.Hide();
    CharacterStatBar.Instance?.Hide();
}
```

---

## Khi muốn thêm stat mới vào StatBar

1. Thêm field `[SerializeField] Sprite iconNewStat;` trong `CharacterStatBar.cs`
2. Tăng `slots` array size lên 5
3. Thêm `SetSlot(4, iconNewStat, ...)` trong `ShowBase()` và `ShowLive()`
4. Chạy lại **Tools > Shop-Arena Setup > Build StatBar In TooltipPanel** để tạo thêm slot

---

## Lỗi thường gặp

| Triệu chứng | Nguyên nhân | Fix |
|---|---|---|
| `Instance` null, stat không hiện | StatsRow bắt đầu inactive | Đừng SetActive(false) trong editor, để Awake tự Hide() |
| Stat hiện sai chỗ (dưới màn hình) | Còn GO CharacterStatBar cũ trong scene | Xóa GO cũ trong Hierarchy |
| Tooltip hiện nhưng không có stat slots | TooltipPanel chưa có StatsRow | Chạy lại tool Build StatBar |
| Icon trắng / không hiện | Chưa gán sprite vào Inspector của CharacterStatBar | Gán HP/DMG/SPD/Angry sprite trong Inspector |
