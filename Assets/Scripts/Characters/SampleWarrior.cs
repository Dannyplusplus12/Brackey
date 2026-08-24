// Nhân vật ví dụ không có skill riêng — dùng nguyên hành vi tấn công mặc định của
// CharacterBase (đa số nhân vật trong game sẽ chỉ cần kế thừa trống như thế này).
// Nhân vật nào cần skill thì override ExecuteAttack() trong class con riêng, dùng
// FlashSprite()/FindLowestHpAlly() có sẵn trên CharacterBase làm nền.
public class SampleWarrior : CharacterBase
{
}
