using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class nền cho mọi nhân vật. Nhân vật cụ thể kế thừa class này, gán CharacterStats
// riêng và override ExecuteAttack() (hoặc OnDeath()) nếu cần hành vi đặc biệt.
// Vị trí "logic" (transform.position của root) dùng cho leash/grid/separation;
// việc lắc lư khi di chuyển chỉ tác động lên visualRoot (thường là 1 child SpriteRenderer)
// để không ảnh hưởng tới tính toán gameplay.
public abstract class CharacterBase : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] protected CharacterStats stats;
    [SerializeField] protected Faction faction = Faction.Ally;

    [Header("Visual (để trống sẽ tự tìm SpriteRenderer trong con)")]
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] protected Transform visualRoot;

    public Faction Faction => faction;
    public CharacterState State { get; private set; } = CharacterState.Idle;
    public float CurrentHP { get; protected set; }
    public float CurrentAngry { get; protected set; }
    public bool IsDead { get; protected set; }
    public bool IsDragging { get; private set; }

    protected Vector2 leashCenter;
    protected float leashRadius;
    protected CharacterBase currentTarget;
    protected float attackTimer;
    Coroutine attackVisualRoutine;
    bool skipLeashClaim;

    readonly List<CharacterBase> nearbyBuffer = new();

    float swayTimer;
    Vector3 visualBaseLocalPos;
    Quaternion visualBaseLocalRot;
    Vector3 lastPositionForSway;

    Vector2 dragVelocity;
    float dragTiltAngle;
    float dragTiltAngularVelocity;

    float hitShakeTimeRemaining;
    Coroutine hitFlashRoutine;

    protected virtual void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (visualRoot == null && spriteRenderer != null) visualRoot = spriteRenderer.transform;
        if (visualRoot == null) visualRoot = transform;
        visualBaseLocalPos = visualRoot.localPosition;
        visualBaseLocalRot = visualRoot.localRotation;
        lastPositionForSway = transform.position;
    }

    protected virtual void OnEnable()
    {
        WaveManager.OnWaveStart += EnterCombat;
        WaveManager.OnWaveEnd += ExitCombat;
    }

    protected virtual void OnDisable()
    {
        WaveManager.OnWaveStart -= EnterCombat;
        WaveManager.OnWaveEnd -= ExitCombat;
    }

    protected virtual void Start()
    {
        CurrentHP = stats.maxHP;
        CurrentAngry = stats.initialAngry;
        leashRadius = stats.leashRadius;

        if (!skipLeashClaim)
            ClaimLeashSlot();

        CharacterGrid.Register(this);
        SetSprite(stats.idleSprite);
    }

    // Dùng cho spawn debug/thủ công: đặt thẳng vào vị trí chỉ định thay vì tự rải quân
    // quanh ShopArea. Phải gọi ngay sau Instantiate (trước khi Start chạy) để có tác dụng.
    public void SetSpawnPosition(Vector2 position)
    {
        transform.position = position;
        leashCenter = position;
        skipLeashClaim = true;

        if (WaveManager.IsWaveActive)
            EnterCombat();
    }

    protected virtual void OnDestroy()
    {
        CharacterGrid.Unregister(this);
    }

    protected virtual void Update()
    {
        if (IsDead || IsDragging) return;

        Vector2 desiredMove = Vector2.zero;

        switch (State)
        {
            case CharacterState.Idle:
                desiredMove = MoveToward(leashCenter);
                break;

            case CharacterState.Seeking:
                desiredMove = TickSeeking();
                break;

            case CharacterState.Attacking:
                TickAttacking();
                break;

            case CharacterState.Leashing:
                desiredMove = MoveToward(leashCenter);
                if (Vector2.Distance(transform.position, leashCenter) < 0.05f)
                    EnterState(CharacterState.Idle);
                break;
        }

        Vector2 separation = ComputeSeparation();
        ApplyMovement(desiredMove + separation);
    }

    // Tách sway ra LateUpdate và đo bằng delta vị trí thực tế (thay vì 1 cờ isMoving
    // được set rải rác trong nhiều nhánh) để không bị miss frame khi lực separation
    // làm vector di chuyển trong Update dao động quanh 0.
    protected virtual void LateUpdate()
    {
        if (IsDead) return;

        if (IsDragging)
        {
            UpdateDragVisual();
            lastPositionForSway = transform.position; // tránh 1 cú "giật" lắc đi bộ ngay khi thả ra
        }
        else
        {
            UpdateVisualSway();
        }

        ApplyHitShake(); // cộng thêm rung lên trên tư thế vừa tính, không phụ thuộc đang ở nhánh nào
    }

    void ApplyHitShake()
    {
        if (hitShakeTimeRemaining <= 0f) return;

        hitShakeTimeRemaining -= Time.deltaTime;
        float t = Mathf.Clamp01(hitShakeTimeRemaining / stats.hitShakeDuration);
        visualRoot.localPosition += (Vector3)(Random.insideUnitCircle * stats.hitShakeStrength * t);
    }

    // ---------- State logic ----------

    protected virtual Vector2 TickSeeking()
    {
        if (currentTarget == null || currentTarget.IsDead)
            currentTarget = FindNearestEnemy();

        if (currentTarget == null)
            return Vector2.zero; // không còn địch nào: đứng yên tại chỗ, chờ hết wave mới về vị trí

        float distToTarget = Vector2.Distance(transform.position, currentTarget.transform.position);
        if (distToTarget <= stats.attackRange)
        {
            EnterState(CharacterState.Attacking);
            attackTimer = stats.attackInterval; // đánh ngay đòn đầu khi vừa vào tầm
            return Vector2.zero;
        }

        return MoveToward(currentTarget.transform.position);
    }

    protected virtual void TickAttacking()
    {
        if (currentTarget == null || currentTarget.IsDead)
        {
            EnterState(CharacterState.Seeking);
            return;
        }

        float distToTarget = Vector2.Distance(transform.position, currentTarget.transform.position);
        if (distToTarget > stats.attackRange)
        {
            EnterState(CharacterState.Seeking);
            return;
        }

        attackTimer += Time.deltaTime;
        if (attackTimer >= stats.attackInterval)
        {
            attackTimer = 0f;
            ExecuteAttack(currentTarget);
        }
    }

    protected void EnterState(CharacterState newState)
    {
        State = newState;
        if (newState != CharacterState.Attacking)
            SetSprite(stats.idleSprite);
    }

    // ---------- Movement / Separation ----------

    protected Vector2 MoveToward(Vector2 target)
    {
        Vector2 pos = transform.position;
        Vector2 dir = target - pos;
        if (dir.sqrMagnitude < 0.0001f) return Vector2.zero;
        return dir.normalized;
    }

    protected Vector2 ComputeSeparation()
    {
        Vector2 pos = transform.position;
        CharacterGrid.GetNearby(pos, stats.separationRadius, nearbyBuffer, faction, this);

        Vector2 push = Vector2.zero;
        foreach (CharacterBase other in nearbyBuffer)
        {
            Vector2 offset = pos - (Vector2)other.transform.position;
            float dist = offset.magnitude;
            if (dist < 0.0001f)
            {
                offset = Random.insideUnitCircle;
                dist = 0.01f;
            }
            float strength = Mathf.Clamp01(1f - dist / stats.separationRadius);
            push += offset.normalized * strength;
        }
        return push * stats.separationStrength;
    }

    protected void ApplyMovement(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) return;

        Vector2 delta = direction.normalized * stats.moveSpeed * Time.deltaTime;
        transform.position += (Vector3)delta;
        CharacterGrid.UpdatePosition(this);
    }

    // Giả lập dáng đi bằng cách nghiêng sprite qua lại + nảy lên xuống, thay vì vẽ
    // animation đi bộ thật. Đo bằng delta vị trí thực tế mỗi frame (không dùng cờ nội
    // bộ) để không bị "giật về pha 0" khi separation làm di chuyển dao động quanh 0.
    protected void UpdateVisualSway()
    {
        float movedDist = ((Vector2)transform.position - (Vector2)lastPositionForSway).magnitude;
        bool moving = movedDist > 0.0005f;
        lastPositionForSway = transform.position;

        swayTimer += Time.deltaTime * stats.swayFrequency;

        float tilt = moving ? Mathf.Sin(swayTimer) * stats.swayTiltAngle : 0f;
        // Abs(Sin) để nảy luôn hướng lên, 2 lần nảy mỗi chu kỳ nghiêng trái-phải (giống bước chân trái/phải).
        float bounce = moving ? Mathf.Abs(Mathf.Sin(swayTimer)) * stats.swayBounceHeight : 0f;

        visualRoot.localRotation = visualBaseLocalRot * Quaternion.Euler(0f, 0f, tilt);
        visualRoot.localPosition = visualBaseLocalPos + new Vector3(0f, bounce, 0f);
    }

    // Giả lập cảm giác "cầm đầu kéo đi": root (= vị trí chuột) đại diện điểm đầu, thân/chân
    // (visualRoot) treo lệch xuống dưới 1 khoảng cố định, và nghiêng ngược hướng kéo theo
    // tốc độ kéo tức thời. Dùng spring-damper (không phải Lerp) để khi dừng đột ngột, thân
    // còn lắc qua lại vài nhịp rồi mới tắt dần, thay vì dừng cứng ngay lập tức.
    void UpdateDragVisual()
    {
        float targetTilt = Mathf.Clamp(-dragVelocity.x * stats.dragTiltPerSpeed, -stats.dragMaxTilt, stats.dragMaxTilt);

        float accel = stats.dragSpringStiffness * (targetTilt - dragTiltAngle) - stats.dragSpringDamping * dragTiltAngularVelocity;
        dragTiltAngularVelocity += accel * Time.deltaTime;
        dragTiltAngle += dragTiltAngularVelocity * Time.deltaTime;

        visualRoot.localRotation = visualBaseLocalRot * Quaternion.Euler(0f, 0f, dragTiltAngle);
        visualRoot.localPosition = visualBaseLocalPos - new Vector3(0f, stats.dragHeadOffset, 0f);
    }

    // ---------- Leash slot (vị trí đứng trong shop) ----------

    protected virtual void ClaimLeashSlot()
    {
        Vector2 baseCenter = ShopArea.Instance != null ? ShopArea.Instance.Center : (Vector2)transform.position;
        float clusterRadius = ShopArea.Instance != null ? ShopArea.Instance.clusterRadius : stats.leashRadius;

        const int maxAttempts = 12;
        Vector2 chosen = baseCenter;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 candidate = baseCenter + Random.insideUnitCircle * clusterRadius;
            CharacterGrid.GetNearby(candidate, stats.separationRadius, nearbyBuffer, faction, this);
            chosen = candidate;
            if (nearbyBuffer.Count == 0) break; // chỗ trống, chọn luôn
        }

        leashCenter = chosen;
        transform.position = chosen;
    }

    public void SetLeashCenter(Vector2 newCenter)
    {
        leashCenter = newCenter;
    }

    // Gọi khi bắt đầu/kết thúc kéo-thả bằng chuột. Trong lúc kéo, AI/di chuyển tự động
    // tạm dừng hoàn toàn (xem Update()); vị trí mới chỉ được lưu làm home nếu KHÔNG
    // trong wave — kéo giữa trận chỉ là tạm thời, không đổi vị trí mặc định.
    public void BeginDrag()
    {
        IsDragging = true;
        dragVelocity = Vector2.zero;
    }

    // CharacterDragHandler gọi hàm này mỗi frame thay vì tự set transform.position,
    // để CharacterBase tính được vận tốc kéo (dùng cho góc nghiêng thân/chân).
    public void UpdateDrag(Vector2 targetPosition)
    {
        dragVelocity = (targetPosition - (Vector2)transform.position) / Mathf.Max(Time.deltaTime, 0.0001f);
        transform.position = targetPosition;
        CharacterGrid.UpdatePosition(this);
    }

    public void EndDrag()
    {
        IsDragging = false;
        dragVelocity = Vector2.zero;

        // Lúc kéo, root = vị trí chuột (đại diện "đầu"), còn thân/chân hiển thị lệch xuống
        // dragHeadOffset. Thả ra thì dịch root xuống đúng khoảng đó để vị trí logic khớp với
        // chỗ thân đang đứng trên màn hình, tránh sprite bị "giật" lên khi trở về dáng bình thường.
        Vector2 dropPosition = (Vector2)transform.position - new Vector2(0f, stats.dragHeadOffset);
        transform.position = dropPosition;
        CharacterGrid.UpdatePosition(this);

        if (!WaveManager.IsWaveActive)
            SetLeashCenter(dropPosition);
    }

    // ---------- Public API ----------

    public virtual void EnterCombat()
    {
        if (IsDead) return;
        currentTarget = null;
        EnterState(CharacterState.Seeking);
    }

    public virtual void ExitCombat()
    {
        if (IsDead) return;
        currentTarget = null;
        EnterState(CharacterState.Leashing);
    }

    public virtual void TakeDamage(float amount)
    {
        if (IsDead) return;
        CurrentHP -= amount;
        PlayHitReaction();

        if (CurrentHP <= 0f)
        {
            CurrentHP = 0f;
            Die();
        }
    }

    protected void PlayHitReaction()
    {
        hitShakeTimeRemaining = stats.hitShakeDuration;

        if (hitFlashRoutine != null) StopCoroutine(hitFlashRoutine);
        hitFlashRoutine = StartCoroutine(HitFlashRoutine());
    }

    IEnumerator HitFlashRoutine()
    {
        spriteRenderer.color = stats.hitFlashColor;
        yield return new WaitForSeconds(stats.hitFlashDuration);
        spriteRenderer.color = Color.white; // luôn trả về màu gốc, tránh kẹt đỏ nếu bị đánh trúng dồn dập
        hitFlashRoutine = null;
    }

    public virtual void Heal(float amount)
    {
        if (IsDead) return;
        CurrentHP = Mathf.Min(stats.maxHP, CurrentHP + amount);
    }

    // Stub: chỉ cộng dồn số liệu, CHƯA xử lý logic đổi phe khi Angry đầy.
    public virtual void AddAngry(float amount, AngryReason reason)
    {
        CurrentAngry = Mathf.Clamp(CurrentAngry + amount, 0f, stats.maxAngry);
    }

    protected virtual void Die()
    {
        IsDead = true;
        CharacterGrid.Unregister(this);
        OnDeath();
    }

    // Mặc định: ẩn nhân vật. Override để thêm hiệu ứng chết, rơi đồ, cộng Angry cho đồng minh...
    protected virtual void OnDeath()
    {
        gameObject.SetActive(false);
    }

    // ---------- Hàm ảo để nhân vật con override ----------

    protected virtual void ExecuteAttack(CharacterBase target)
    {
        target.TakeDamage(stats.damage);
        FlashSprite(stats.attackSprite);
    }

    // Không giới hạn phạm vi: quét toàn bộ phe đối lập, luôn ưu tiên gần nhất.
    protected CharacterBase FindNearestEnemy()
    {
        Faction opposing = faction == Faction.Ally ? Faction.Enemy : Faction.Ally;
        return CharacterGrid.FindNearest(transform.position, opposing, this);
    }

    protected CharacterBase FindLowestHpAlly()
    {
        return CharacterGrid.FindLowestHp(faction, this);
    }

    // Đổi sprite tạm thời (VD: 1 đòn đánh, 1 lần dùng skill) rồi tự quay lại idle sau
    // "Attack Visual Duration". Dùng cho cả ExecuteAttack và các skill override.
    protected void FlashSprite(Sprite sprite)
    {
        if (attackVisualRoutine != null) StopCoroutine(attackVisualRoutine);
        attackVisualRoutine = StartCoroutine(FlashSpriteRoutine(sprite));
    }

    IEnumerator FlashSpriteRoutine(Sprite sprite)
    {
        SetSprite(sprite);
        yield return new WaitForSeconds(stats.attackVisualDuration);
        SetSprite(stats.idleSprite);
        attackVisualRoutine = null;
    }

    protected void SetSprite(Sprite sprite)
    {
        if (sprite != null && spriteRenderer != null)
            spriteRenderer.sprite = sprite;
    }

    // ---------- Debug ----------

    protected virtual void OnDrawGizmosSelected()
    {
        if (stats == null) return;

        Vector3 center = Application.isPlaying ? (Vector3)leashCenter : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, stats.leashRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stats.attackRange);
    }
}
