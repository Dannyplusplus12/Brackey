// Danh sách tất cả sound event trong game.
// Thêm entry mới vào đây, rồi gán clip tương ứng trong SoundLibrary asset.
public enum SoundId
{
    // ── UI ──────────────────────────────────────────────────────────────────
    UIHover,        // con trỏ hover vào button / item
    UIClick,        // bấm nút bất kỳ
    Reroll,         // bấm nút Reroll trong shop
    Buy,            // mua item thành công
    WaveStart,      // bắt đầu wave
    WheelSpin,      // quay bánh xe gacha (clip dài ~3-4s, tự fade theo ease)

    // ── Combat ──────────────────────────────────────────────────────────────
    Attack,         // nhân vật tấn công
    Hit,            // nhân vật bị trúng đòn
    Death,          // nhân vật chết
    Heal,           // nhân vật được heal / feed
}
