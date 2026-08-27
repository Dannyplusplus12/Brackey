# Brackey — Top-down Auto-battler

Kiểu "How many dude". Không dùng Physics/Rigidbody2D cho gameplay — mọi di chuyển/separation đều tính tay bằng vector mỗi frame.

---

## Cấu trúc Scene (SampleScene)

```
SampleScene
├── SpawnArea          — ShopArea.cs: tâm + bán kính để character tự chọn vị trí đứng khi spawn
├── WaveManager        — WaveManager.cs: broadcaster OnWaveStart/OnWaveEnd
├── Debug              — DebugOverlay.cs: phím E spawn enemy, 1/2 đổi timescale
├── GameManager        — GameManager.cs: state machine Shop↔Arena, quản lý delay giữa state
├── Main Camera        — MainCameraStateController.cs: lerp camera giữa 2 target
├── ArenaCamTarget     — Empty GO, đánh dấu vị trí camera lúc Arena
├── ShopCamTarget      — Empty GO, đánh dấu vị trí camera lúc Shop (arena ở góc trên phải)
├── Canvas (Screen Space Overlay)
│   ├── ShopRoot       — ẩn/hiện theo state. Chứa toàn bộ UI Shop
│   │   ├── RosterPanel          — ScrollView danh sách nhân vật ally
│   │   ├── ShopOfferPanel       — 4 ô item rao bán + nút Reroll
│   │   ├── ShopInventoryBar     — 4 ô item Active của player
│   │   ├── StaticItemBox        — danh sách item StatBoost đang sở hữu
│   │   └── GachaPanel           — 2 nút mở pack nhân vật
│   └── ArenaRoot      — (trống, chứa UI Arena sau: hotbar, static item strip)
├── Managers           — PlayerInventory, ShopOfferManager, GachaManager
├── Character_Warrior  — prefab ally mẫu
└── Items_0            — sprite sàn / background world
```

**Canvas controller:** `ShopArenaCanvasController.cs` trên Canvas — subscribe `OnGameStateChanged`, bật/tắt ShopRoot và ArenaRoot.

---

## Flow State Machine

```
Start → Arena → đợi arenaStartDelay (3s) → StartWave()
       ↓ hết địch
       đợi shopDelay (2s)
       ↓
       Shop (ShopRoot hiện, camera zoom ra, arena vẫn visible ở góc trên phải)
       ↓ bấm nút "Bắt đầu Wave"
       Arena (camera zoom vào ngay) → đợi arenaStartDelay (3s) → StartWave()
```

**Lưu ý:** Wave đầu tiên cũng chờ `arenaStartDelay` (3s) — không start ngay.

---

## Character State Machine

```
EnterCombat → Waiting(random) → Seeking → [vào tầm] → Attacking
                                    ↑ target chết → Waiting(random) ┘
ExitCombat → Waiting(random) → Leashing → Idle
OnGameStateChanged(Shop) → nếu Leashing → Idle (dừng leash ngay)
```

**CharacterState enum:** `Idle`, `Waiting`, `Seeking`, `Attacking`, `Leashing`

---

## Script Index

### `Assets/Scripts/Characters/` — Logic nhân vật & game loop

| File | Vai trò | Khi nào cần sửa |
|---|---|---|
| `CharacterBase.cs` | Abstract base: state machine, di chuyển, sway, drag, hit reaction, effective stats, attackCount, SpawnVFX, shadow, facing flip | Thêm mechanic mới cho mọi nhân vật |
| `CharacterStats.cs` | ScriptableObject: máu, dame, tốc đánh, sway, drag feel, hit reaction, angry/food, 3 sprite, centerOffset, flipCooldown, actionDelay | Thêm stat mới (base value) |
| `CharacterShadow.cs` | `[ExecuteAlways]` blob shadow tự generate texture. `UpdateShadow(bounceFactor 0→1)`. Child của root (không phải Visual) | Đổi hình dạng/behavior shadow |
| `CharacterGrid.cs` | Spatial hash grid tĩnh: `GetNearby` (local), `FindNearest`/`FindLowestHp`/`CountAlive` (global). Dùng `BodyCenter` cho distance calc | Thay đổi cách tìm target/neighbor |
| `CharacterDragHandler.cs` | Kéo-thả chuột qua `Physics2D.OverlapPoint`. Static lock `currentlyDragged` ngăn multi-grab. Cần `Collider2D (Is Trigger)` trên root | Sửa UX kéo-thả |
| `CharacterSpawner.cs` | Static class: `Spawn(prefab)` → instantiate tại `ShopArea.Center` | Đổi vị trí spawn |
| `GameManager.cs` | State machine Shop↔Arena, delay trước/sau wave, detect hết địch | Thêm state mới, đổi timing |
| `WaveManager.cs` | Broadcaster `OnWaveStart/OnWaveEnd/IsWaveActive`. KHÔNG tự biết hết địch — GameManager quyết định | Spawn system, wave config |
| `MainCameraStateController.cs` | Lerp camera.position + orthographicSize giữa ArenaCamTarget và ShopCamTarget theo state | Đổi camera behavior, thêm shake |
| `ShopArea.cs` | Singleton: tâm + radius cho character tự chọn leash point khi spawn | Đổi vùng đứng chờ |
| `DebugOverlay.cs` | IMGUI overlay: wave state, timescale, ally/enemy count, per-char state+faction+target+speed | Thêm debug info |
| `SampleWarrior.cs` | Class rỗng kế thừa CharacterBase — template tạo nhân vật mới | Không cần sửa |
| `GameState.cs` | Enum: `Shop`, `Arena` | — |
| `Faction.cs`, `CharacterState.cs`, `AngryReason.cs` | Enum | — |

### `Assets/Char/` — Nhân vật có sẵn

Mỗi folder chứa: sprites (IDLE, ATK), CharacterStats asset, script, prefab.

| Folder | Class | Ghi chú |
|---|---|---|
| `VIKING/` | `Viking` | Có shadow, đã test combat |
| `BAGGER/` | `Bagger` | Prefab tạo bằng tool |
| `DOG Z/` | `DogZ` | Prefab tạo bằng tool |
| `FRANK Z/` | `FrankZ` | Prefab tạo bằng tool |
| `NGUOI SOI/` | `NguoiSoi` | Prefab tạo bằng tool |
| `SUMO/` | `Sumo` | Prefab tạo bằng tool |
| `ZOMIBIE/` | `Zombie` | Prefab tạo bằng tool |

Tất cả skill stub — override `ExecuteAttack()` để implement.

### `Assets/Scripts/Items/` — Kinh tế & inventory

| File | Vai trò | Khi nào cần sửa |
|---|---|---|
| `GlobalStatBonus.cs` | Stat runtime bonus: global (all chars) + per-type (key = CharacterStats asset). `StatDelta` struct. Reset tự động mỗi domain load | Thêm stat mới vào delta |
| `PlayerWallet.cs` | Singleton: quản lý Corn. `Earn()`, `TrySpend()`, `OnCornChanged` event. Starting corn gán trong Inspector | Thêm nguồn earn mới |
| `FeedingManager.cs` | Subscribe `OnGameStateChanged → Arena`: iterate ally theo roster order, spend corn → `Feed()` hoặc `SkipFeed()` | Đổi thứ tự feed, thêm điều kiện |
| `PlayerInventory.cs` | 4 slot item Active + list item StatBoost. `SellSlot` trả corn. Event `OnInventoryChanged`/`OnStaticItemsChanged`/`OnSlotActivated` | Thêm slot, áp StatDelta khi mua/bán |
| `ShopOfferManager.cs` | Random 4 item, `Reroll()` tốn `rerollCost` corn, `BuyOffer()` xử lý 3 loại: Character/Active/StatBoost | Thêm rarity, đổi giá |
| `ItemData.cs` | ScriptableObject: tên, icon, ItemType, `buyCost`, `sellValue`, `characterPrefab`, **ItemTargetType**, **targetCharacterType**, **StatDelta** | Thêm field effect mới |
| `ItemType.cs` | Enum: `Active`, `StatBoost`, `Character` | Thêm type mới |
| `GachaManager.cs` | Stub 2 pack — chưa có logic roll/popup | Implement gacha thật |
| `GachaPackData.cs` | ScriptableObject: tên + icon pack | Thêm pool nhân vật |

**Data assets:** `Assets/Data/Items/` — BloodPot, DragonTail, QuickFeather, StoneSword (ItemData).

### `Assets/Scripts/UI/` — UI display only, không chứa logic game

| File | Vai trò | Gắn lên |
|---|---|---|
| `ShopArenaCanvasController.cs` | Bật/tắt ShopRoot + ArenaRoot theo state | Canvas |
| `CharacterRosterUI.cs` | Tạo RosterEntry cho mỗi ally lúc Start (scan 1 lần) | RosterPanel |
| `CharacterRosterEntry.cs` | 1 dòng roster: portrait + thanh Angry. Bind với CharacterBase | Prefab `RosterEntry` |
| `ShopOfferSlotUI.cs` | 1 ô item bán, slotIndex 0-3 gán tay | OfferSlot0-3 |
| `ShopInventorySlotUI.cs` | 1 ô inventory Shop: click chọn→swap, right-click bán | InvSlot0-3 |
| `ArenaHotbarSlotUI.cs` | 1 ô hotbar Arena: phím 1-4 hoặc click kích hoạt | ArenaHotbar (chưa dựng) |
| `StaticItemListUI.cs` | Hiển thị list StatBoost. Dùng chung Shop (Grid) + Arena (Horizontal) | StaticItemBox/ArenaStrip |
| `GachaPackSlotUI.cs` | 1 nút gacha, packIndex 0-1 gán tay | PackSlot0-1 |

### `Assets/Editor/` — Unity Editor tools

| File | Menu | Dùng khi |
|---|---|---|
| `CharacterPrefabCreator.cs` | Tools > Characters > **Create All Character Prefabs** | Tạo/update prefab + CharacterStats asset cho tất cả char trong Assets/Char/ |
| `GameUISetupTool.cs` | Tools > Shop-Arena Setup > **Build UI In Scene** | Dựng lại khung Canvas từ đầu |
| | Tools > Shop-Arena Setup > **Setup Camera (Simple Move)** | Tạo ArenaCamTarget + ShopCamTarget, wire MainCameraStateController |

---

## Hệ thống Stat (Effective Stats)

`CharacterStats` (ScriptableObject) = **bất biến**, là base value. Không sửa ở runtime.

`GlobalStatBonus` (static) = **runtime delta**, item cộng/trừ vào đây.

`CharacterBase` expose các property:
```
EffectiveDamage        = stats.damage        + GlobalStatBonus.damage        + perTypeBonus.damage
EffectiveMoveSpeed     = stats.moveSpeed     + GlobalStatBonus.moveSpeed     + perTypeBonus.moveSpeed
EffectiveMaxHP         = stats.maxHP         + GlobalStatBonus.maxHP         + perTypeBonus.maxHP
EffectiveAttackInterval= stats.attackInterval+ GlobalStatBonus.attackInterval+ perTypeBonus.attackInterval
EffectiveAttackRange   = stats.attackRange   + GlobalStatBonus.attackRange   + perTypeBonus.attackRange
```

**BodyCenter** = `transform.position + up * (stats.centerOffset * lossyScale.y)` — dùng cho mọi distance calc (attack, separation, gizmos). Khác với `dragHeadOffset` (chỉ dùng cho drag visual).

---

## CharacterStats — Các field quan trọng

| Field | Mô tả |
|---|---|
| `centerOffset` | Khoảng từ chân → tâm thân (local). Dùng cho BodyCenter, gizmos, shadow |
| `dragHeadOffset` | Khoảng từ chân → đầu (local). Dùng khi drag (cursor ở đầu) |
| `actionDelayMin/Max` | Random delay (s) giữa các action: wave start → seek, kill → seek next, wave end → leash |
| `flipCooldown` | Thời gian tối thiểu (s) giữa 2 lần flip sprite (tránh giật khi đứng gần điểm đến) |
| `separationRadius` | Bán kính đẩy cùng phe |
| `separationStrength` | Lực đẩy |

---

## Prefab Structure (mỗi character)

```
Root (CharacterBase subclass + BoxCollider2D isTrigger + CharacterDragHandler)
├── Visual   (SpriteRenderer — bị sway/tilt khi di chuyển/drag)
├── Shadow   (SpriteRenderer + CharacterShadow — KHÔNG bị sway, ở chân)
└── VFXPoint (Transform optional — fallback về root nếu chưa gán)
```

**Quan trọng:** Shadow là child của Root, không phải Visual — nên không bị ảnh hưởng bởi sway animation. Khi drag, shadow tự offset xuống `-dragHeadOffset` để ở lại chân.

---

## Hệ thống Item — Kiến trúc & Kế hoạch

### Phân loại item

| Loại | ItemType | Mô tả | Trạng thái |
|---|---|---|---|
| **Nhân vật** | `Character` | `BuyOffer` gọi `CharacterSpawner.Spawn(characterPrefab)` | Hoàn chỉnh |
| **Stat tĩnh** | `StatBoost` | Áp `StatDelta` ngay khi mua, hoàn khi bán | Nền sẵn sàng — cần handler trong `PlayerInventory` |
| **Stat kích hoạt** | `Active` | Áp `StatDelta` khi player bấm kích hoạt | Nền sẵn sàng — `OnSlotActivated` event đã có |
| **Điều kiện đặc biệt** | `Active` | Logic custom: respawn, hồi máu... | Cần `ItemEffectHandler.cs` riêng |

---

## Hệ thống Skill trên Character

`attackCount` (protected int trên CharacterBase) — reset mỗi `ExitCombat()`.

Override `ExecuteAttack()` trong subclass để trigger skill theo đòn:
```csharp
protected override void ExecuteAttack(CharacterBase target)
{
    base.ExecuteAttack(target); // tăng attackCount, deal damage, flash sprite
    if (attackCount % 3 == 0)  // mỗi 3 đòn
        FlashSprite(stats.skillSprite); // hoặc SpawnVFX(myVfxPrefab)
}
```

---

## Hệ thống VFX

`SpawnVFX(GameObject prefab)` — protected method trên CharacterBase.
- Spawn prefab tại `vfxSpawnPoint` (nếu gán) hoặc `transform.position`.
- Prefab tự hủy: Particle System → Stop Action = Destroy, hoặc `Destroy(go, duration)`.

---

## Tạo nhân vật mới

1. Đặt sprites vào `Assets/Char/TÊNCHAR/` (file có chữ IDLE và ATK)
2. Tạo class kế thừa `CharacterBase` trong cùng folder — rỗng nếu không cần skill riêng
3. Thêm entry vào `FolderToClass` dict trong `CharacterPrefabCreator.cs`
4. Chạy **Tools > Characters > Create All Character Prefabs**
5. Chỉnh số liệu trong CharacterStats asset vừa tạo

---

## Camera System

**Nguyên lý:** 1 Main Camera duy nhất, `Viewport Rect` luôn = `(0,0,1,1)` full screen.

- **Arena state:** camera lerp đến `ArenaCamTarget.position`, `arenaOrthographicSize` nhỏ
- **Shop state:** camera lerp đến `ShopCamTarget.position` (lệch trái+xuống), `shopOrthographicSize` lớn hơn

---

## Sorting (URP 2D)

`Assets/Settings/Renderer2D.asset` → Transparency Sort Mode = **Custom Axis**, axis `(0,1,0)` — sprite Y thấp hơn render đè lên sprite Y cao hơn.

---

## Debug

- Phím **`E`** (Play mode): spawn enemy tại vị trí chuột (gán Enemy Prefab trong `DebugOverlay` Inspector)
- Phím **`1`/`2`**: giảm/tăng timescale (0.25→0.5→1→2→4x)
- `GameManager` Inspector → chuột phải → **Enter Arena (Test)** / **Enter Shop (Test)**
- DebugOverlay hiện: wave state, corn, ally/enemy count, per-char: faction, state, speed, target + distance

---

## Hệ thống Angry & Food

**Nguồn tăng angry:**
- `angryOnRoundStart`: cộng vào mỗi wave start (luôn áp)
- `angryPerHunger`: cộng vào khi `SkipFeed()` (thiếu corn)
- `angryOnAllyDeath`: cộng vào khi 1 ally cùng phe chết

**Feeding flow (mỗi khi vào Arena):**
`FeedingManager` → `PlayerWallet.TrySpend(foodRequiredPerRound)`:
- Đủ corn → `Feed()`: hồi full HP
- Thiếu corn → `SkipFeed()`: +angryPerHunger

**Angry full → SwitchToEnemy():** unregister Ally → faction = Enemy → re-register → EnterCombat nếu wave active

---

## Hệ thống Economy (Corn)

- Wave win: `GameManager.waveWinReward` (default 5)
- Kill: `CharacterStats.killReward` per enemy (default 1)
- Mua item: `ItemData.buyCost` | Bán: `ItemData.sellValue`
- Feed: `CharacterStats.foodRequiredPerRound` per char per round
- Reroll: `ShopOfferManager.rerollCost` (default 1)

---

## Những gì chưa làm (stub)

- **Gacha**: `GachaManager.OpenPack()` chỉ log, chưa có popup/roll
- **Item StatBoost effect**: `PlayerInventory` chưa áp `StatDelta` vào `GlobalStatBonus` khi mua/bán
- **ItemEffectHandler**: chưa có — cần để xử lý item đặc biệt
- **ArenaRoot UI**: hotbar item Arena + static item strip chưa dựng
- **Hover tooltip**: `ItemData.description` có nhưng chưa có UI
- **Corn UI**: chưa có display số corn trên màn hình
- **Shadow cho các char ngoài VIKING**: thêm tay hoặc chạy lại prefab tool
