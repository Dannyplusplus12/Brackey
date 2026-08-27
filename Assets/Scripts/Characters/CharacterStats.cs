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
    public float separationRadius = 0.6f;
    public float separationStrength = 2f;
    [Tooltip("Thời gian tối thiểu (giây) giữa 2 lần flip sprite. Tăng nếu bị giật khi đứng gần điểm đến.")]
    public float flipCooldown = 0.4f;

    [Header("Action Delay (khoảng dừng ngẫu nhiên giữa các hành động)")]
    [Tooltip("Thời gian dừng tối thiểu trước khi bắt đầu di chuyển / tìm địch tiếp theo / quay về")]
    public float actionDelayMin = 0.2f;
    [Tooltip("Thời gian dừng tối đa — mỗi char tự chọn ngẫu nhiên trong khoảng này")]
    public float actionDelayMax = 0.8f;

    [Header("Visual Sway (lắc lư khi di chuyển)")]
    [Tooltip("Góc nghiêng trái/phải tối đa (độ), thay cho animation đi bộ")]
    public float swayTiltAngle = 15f;
    [Tooltip("Độ cao nảy lên khi di chuyển")]
    public float swayBounceHeight = 0.06f;
    public float swayFrequency = 8f;

    [Header("Character Size (local space)")]
    [Tooltip("Khoảng cách từ chân (transform.position) lên tới tâm thân nhân vật (local units). Dùng cho BodyCenter, shadow, gizmos.")]
    public float centerOffset = 0.3f;

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

    [Header("Angry")]
    public float initialAngry = 0f;
    [Tooltip("Angry tăng mỗi round nếu KHÔNG được cho ăn")]
    public float angryPerHunger = 25f;
    [Tooltip("Angry tăng cho ally còn sống khi 1 ally khác chết")]
    public float angryOnAllyDeath = 0f;
    [Tooltip("Angry tăng mỗi round start (luôn áp, bất kể có ăn không)")]
    public float angryOnRoundStart = 0f;
    public float maxAngry = 100f;

    [Header("Food & Economy")]
    [Tooltip("Corn cần để feed character này mỗi round")]
    public int foodRequiredPerRound = 1;
    [Tooltip("Corn player nhận khi giết character này (chỉ có ý nghĩa với Enemy)")]
    public int killReward = 1;

    [Header("Sprites")]
    public Sprite idleSprite;
    public Sprite attackSprite;
    public Sprite skillSprite;

    [Header("Portrait (roster UI - crop khung vuông từ Idle Sprite)")]
    [Tooltip("Lệch vị trí sprite trong khung vuông, dùng để chọn phần hiện ra (VD: nửa người trên)")]
    public Vector2 portraitOffset = Vector2.zero;
    [Tooltip("Phóng to sprite trong khung vuông")]
    public float portraitScale = 1f;
}
