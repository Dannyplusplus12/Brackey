# Character System (Assets/Scripts/Characters)

Top-down auto-battler kiểu "How many dude". Không dùng Physics/Rigidbody2D cho gameplay — mọi di chuyển/tách khoảng cách đều tính tay bằng vector mỗi frame.

## File map

| File | Vai trò |
|---|---|
| `CharacterBase.cs` | Class trừu tượng nền cho mọi nhân vật. State machine, di chuyển, sway, drag, hit reaction. |
| `CharacterStats.cs` | ScriptableObject chứa số liệu (máu, dame, tốc đánh, sway, drag feel, hit reaction, angry-stub, food-stub, 3 sprite). Tạo nhân vật mới = tạo asset mới, không cần code. |
| `CharacterGrid.cs` | Spatial hash grid tĩnh: `GetNearby` (cục bộ, dùng cho separation/spawn-slot), `FindNearest`/`FindLowestHp` (toàn cục, không giới hạn phạm vi, dùng tìm địch/heal-target). |
| `Faction.cs`, `CharacterState.cs`, `AngryReason.cs` | Enum. |
| `ShopArea.cs` | Singleton: tâm + bán kính cụm để nhân vật tự chọn vị trí đứng lúc spawn. |
| `WaveManager.cs` | Broadcaster thuần (`OnWaveStart`/`OnWaveEnd`/`IsWaveActive`). KHÔNG tự biết khi nào hết địch — quyết định gọi `EndWave()` khi nào phải do 1 GameManager cấp cao hơn (chưa có). |
| `CharacterDragHandler.cs` | Kéo-thả bằng chuột, dùng `Physics2D.OverlapPoint` (không dùng `OnMouseDown`). Cần `Collider2D` (Is Trigger) trên root. |
| `SampleWarrior.cs` | Ví dụ class rỗng, không skill — pattern mặc định khi tạo nhân vật mới không cần logic riêng. |
| `DebugOverlay.cs` | Overlay IMGUI: hiện wave state + time scale, phím tắt debug (xem bên dưới). |

## Cách tạo nhân vật mới

1. Tạo `CharacterStats` asset (`Create > Characters > Character Stats`), chỉnh số + gán 3 sprite (idle/attack/skill).
2. Class kế thừa `CharacterBase` — rỗng nếu không cần gì đặc biệt (xem `SampleWarrior`). Cần skill/hành vi riêng thì override `ExecuteAttack(target)` (gọi `FlashSprite()`/`FindLowestHpAlly()`/`base.ExecuteAttack()` tuỳ ý) hoặc `OnDeath()`.
3. Prefab: root có script nhân vật + `Collider2D` (Is Trigger, để kéo-thả) + `CharacterDragHandler`; child "Visual" có `SpriteRenderer` (script tự tìm nếu để trống field).

## Core concepts

- **State**: Idle → Seeking → Attacking → Leashing. Trigger chuyển Idle↔combat: `WaveManager.OnWaveStart/OnWaveEnd` (mọi `CharacterBase` tự subscribe).
- **Tìm địch**: không giới hạn phạm vi, luôn ưu tiên gần nhất (`CharacterGrid.FindNearest`), khoá mục tiêu tới khi chết.
- **Không có địch giữa wave** → đứng yên tại chỗ, KHÔNG tự về home. Chỉ về home (`leashCenter`) khi `ExitCombat()` (wave kết thúc).
- **Tấn công**: vào tầm đánh ngay, sprite tấn công chỉ lóe lên `attackVisualDuration` giây rồi về idle, cooldown = `attackInterval`.
- **Leash center**: tự chọn ngẫu nhiên quanh `ShopArea` lúc spawn (né chỗ đông); đổi vĩnh viễn khi kéo-thả lúc KHÔNG trong wave (`EndDrag`).
- **Kéo-thả**: mọi state đều kéo được; AI tạm dừng hoàn toàn lúc kéo (`IsDragging`); thân/chân lệch xuống `dragHeadOffset` so với chuột, nghiêng theo spring-damper; thả ra thì bù lại offset vào vị trí thật.
- **Separation**: lực đẩy mềm cộng vào hướng di chuyển mỗi frame (không phải collision cứng) — chấp nhận đè nhẹ, không kẹt.
- **Sway đi bộ**: nghiêng trục Z + nảy Y, chỉ chạy khi không bị kéo.
- **Hit reaction**: mọi `TakeDamage()` tự shake + flash đỏ (`PlayHitReaction`).
- **Angry / Food**: field đã có trong `CharacterStats` (`initialAngry`, `angryOnAllyDeath`...) nhưng **chưa có logic** — chỉ cộng số, chưa đổi phe.

## Sorting (URP 2D)

`Assets/Settings/Renderer2D.asset` → `Transparency Sort Mode` cần = **Custom Axis**, axis `(0,1,0)` (đã set sẵn) để sprite Y thấp hơn đè lên sprite Y cao hơn.

## Debug (component `DebugOverlay`, gắn 1 lần trong scene)

- `E`: spawn Enemy (cần gán `Enemy Prefab` trong Inspector) tại vị trí chuột.
- `1` / `2`: giảm/tăng `Time.timeScale` theo mốc 0.25/0.5/1/2/4x.
- `WaveManager` component: chuột phải → `Start Wave (Test)` / `End Wave (Test)`.
