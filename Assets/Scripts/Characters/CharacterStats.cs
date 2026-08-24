using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterStats", menuName = "Characters/Character Stats")]
public class CharacterStats : ScriptableObject
{
    [Header("Combat")]
    public float maxHP = 100f;
    public float damage = 10f;
    public float attackInterval = 1f;
    public float attackRange = 1.2f;
    [Tooltip("Thời gian hiển thị sprite tấn công mỗi đòn, phải nhỏ hơn Attack Interval")]
    public float attackVisualDuration = 0.15f;

    [Header("Movement")]
    public float moveSpeed = 2.5f;
    public float leashRadius = 2.5f;
    public float separationRadius = 0.6f;
    public float separationStrength = 2f;

    [Header("Visual Sway (lắc lư khi di chuyển)")]
    [Tooltip("Góc nghiêng trái/phải tối đa (độ), thay cho animation đi bộ")]
    public float swayTiltAngle = 15f;
    [Tooltip("Độ cao nảy lên khi di chuyển")]
    public float swayBounceHeight = 0.06f;
    public float swayFrequency = 8f;

    [Header("Drag Feel (khi bị kéo bằng chuột)")]
    [Tooltip("Khoảng lệch để 'đầu' nằm đúng vị trí chuột, thân/chân treo bên dưới")]
    public float dragHeadOffset = 0.3f;
    [Tooltip("Độ nghiêng (độ) mỗi 1 unit/giây tốc độ kéo, nghiêng ngược hướng kéo")]
    public float dragTiltPerSpeed = 4f;
    public float dragMaxTilt = 35f;
    [Tooltip("Độ 'cứng' lò xo kéo thân về đúng góc mục tiêu")]
    public float dragSpringStiffness = 120f;
    [Tooltip("Độ giảm dao động; càng thấp càng lắc nhiều lần trước khi dừng hẳn")]
    public float dragSpringDamping = 8f;

    [Header("Hit Reaction (khi bị đánh trúng)")]
    public Color hitFlashColor = Color.red;
    public float hitFlashDuration = 0.08f;
    public float hitShakeStrength = 0.06f;
    public float hitShakeDuration = 0.15f;

    [Header("Angry (chưa xử lý logic, chỉ lưu số liệu)")]
    public float initialAngry = 0f;
    public float angryPerHunger = 0f;
    public float angryOnAllyDeath = 0f;
    public float angryOnRoundStart = 0f;
    public float maxAngry = 100f;

    [Header("Food")]
    public int foodRequiredPerRound = 1;

    [Header("Sprites")]
    public Sprite idleSprite;
    public Sprite attackSprite;
    public Sprite skillSprite;
}
