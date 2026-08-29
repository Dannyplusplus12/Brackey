using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class nền cho mọi nhân vật. Nhân vật cụ thể kế thừa class này, gán CharacterStats
// riêng và override ExecuteAttack() (hoặc OnDeath()) nếu cần hành vi đặc biệt.
// Vị trí "logic" (transform.position của root) dùng cho leash/grid/separation;
// việc lắc lư khi di chuyển chỉ tác động lên visualRoot (thường là 1 child SpriteRenderer)
// để không ảnh hưởng tới tính toán gameplay.
//
// Effective stats = CharacterStats (base, bất biến) + GlobalStatBonus (runtime, từ item).
// Dùng EffectiveDamage / EffectiveMoveSpeed / v.v. thay vì đọc stats.xxx trực tiếp.
public abstract class CharacterBase : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] protected CharacterStats stats;
    [SerializeField] protected Faction faction = Faction.Ally;

    [Header("Visual (để trống sẽ tự tìm SpriteRenderer trong con)")]
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] protected Transform visualRoot;

    [Header("VFX (để trống = dùng vị trí của chính nhân vật)")]
    [SerializeField] Transform vfxSpawnPoint;

    [Header("Shadow (để trống sẽ tự tìm CharacterShadow trong con)")]
    [SerializeField] CharacterShadow shadow;

    public Faction Faction => faction;
    public CharacterStats Stats => stats;
    public CharacterState State { get; private set; } = CharacterState.Idle;
    public float CurrentHP { get; protected set; }
    public float CurrentAngry { get; protected set; }
    public bool IsDead { get; protected set; }
    public bool IsDragging { get; private set; }
    public CharacterBase CurrentTarget => currentTarget;
    public float DebugEffectiveMoveSpeed => EffectiveMoveSpeed;
    public float MaxHP      => EffectiveMaxHP;
    public float LiveDamage => EffectiveDamage;
    public float LiveSpeed  => EffectiveMoveSpeed;

    // Số đòn đánh đã thực hiện từ đầu wave. Reset mỗi ExitCombat.
    // Dùng trong subclass để trigger skill mỗi X đòn.
    protected int attackCount { get; private set; }

    // ── Per-instance bonus — áp lên đúng 1 nhân vật cụ thể, không ảnh hưởng type khác ──
    // Dùng khi skill 1 char cần buff trực tiếp 1 char khác (VD: Bagger beg cho Viking gần nhất).
    private StatDelta _instanceBonus;
    public void AddInstanceBonus(StatDelta delta)    { _instanceBonus.Add(delta); }
    public void RemoveInstanceBonus(StatDelta delta) { _instanceBonus.Subtract(delta); }

    // ── Effective stats = (base + flat) * (1 + percent) ────────────────────
    // flat   = GlobalStatBonus + perType flat + instanceBonus flat
    // percent= GlobalStatBonus + perType percent + instanceBonus percent
    protected float EffectiveDamage
    {
        get {
            var t = GlobalStatBonus.GetTypeBonus(stats);
            float flat = stats.damage + GlobalStatBonus.damage + t.damage + _instanceBonus.damage;
            float pct  = GlobalStatBonus.damagePercent + t.damagePercent + _instanceBonus.damagePercent;
            return flat * (1f + pct);
        }
    }
    protected float EffectiveMoveSpeed
    {
        get {
            var t = GlobalStatBonus.GetTypeBonus(stats);
            float flat = stats.moveSpeed + GlobalStatBonus.moveSpeed + t.moveSpeed + _instanceBonus.moveSpeed;
            float pct  = GlobalStatBonus.moveSpeedPercent + t.moveSpeedPercent + _instanceBonus.moveSpeedPercent;
            return flat * (1f + pct);
        }
    }
    protected float EffectiveMaxHP
    {
        get {
            var t = GlobalStatBonus.GetTypeBonus(stats);
            float flat = stats.maxHP + GlobalStatBonus.maxHP + t.maxHP + _instanceBonus.maxHP;
            float pct  = GlobalStatBonus.maxHPPercent + t.maxHPPercent + _instanceBonus.maxHPPercent;
            return flat * (1f + pct);
        }
    }
    // Tổng APS = (base + flat) * (1 + percent)
    public float EffectiveAPS
    {
        get {
            var t = GlobalStatBonus.GetTypeBonus(stats);
            float flat = stats.attackSpeed + GlobalStatBonus.attackSpeed + t.attackSpeed + _instanceBonus.attackSpeed;
            float pct  = GlobalStatBonus.attackSpeedPercent + t.attackSpeedPercent + _instanceBonus.attackSpeedPercent;
            return flat * (1f + pct);
        }
    }
    // Interval (giây) dùng nội bộ — clamp min 0.05s (tối đa 20 APS)
    protected float EffectiveAttackInterval => 1f / Mathf.Max(0.05f, EffectiveAPS);
    protected float EffectiveAttackRange    => stats.attackRange + GlobalStatBonus.attackRange + GlobalStatBonus.GetTypeBonus(stats).attackRange;
    // Public: UI và FeedingManager đọc chi phí ăn thực tế (có thể bị item thay đổi)
    public  int    EffectiveFoodCost       => Mathf.Max(0, stats.foodRequiredPerRound
                                                + (int)GlobalStatBonus.foodCost
                                                + (int)GlobalStatBonus.GetTypeBonus(stats).foodCost);

    // Tâm thân nhân vật (giữa chân và đầu) — dùng cho mọi tính toán distance/separation
    // thay vì transform.position (chân) để khớp với vòng tròn Gizmos và cảm giác gameplay.
    // centerOffset là local space → nhân lossyScale.y để ra world space.
    public Vector2 BodyCenter => (Vector2)transform.position + Vector2.up * (stats != null ? stats.centerOffset * transform.lossyScale.y : 0f);

    protected Vector2 leashCenter;
    protected CharacterBase currentTarget;
    protected float attackTimer;
    float waitTimer;
    CharacterState waitNextState;
    Coroutine attackVisualRoutine;
    bool skipLeashClaim;

    readonly List<CharacterBase> nearbyBuffer = new();

    protected float swayTimer;
    float swayStopTimer;
    const float SwayStopDelay = 0.12f; // giây đứng yên trước khi sway tắt hẳn
    float lastFlipTime = -999f;
    Vector3 visualBaseLocalPos;
    Quaternion visualBaseLocalRot;
    Vector3 lastPositionForSway;

    Vector2 dragVelocity;
    float dragTiltAngle;
    float dragTiltAngularVelocity;

    float hitShakeTimeRemaining;
    Coroutine hitFlashRoutine;

    // Màu nền của sprite: Color.white khi còn là Ally, đỏ nhạt khi đã SwitchToEnemy.
    // HitFlashRoutine reset về baseColor thay vì Color.white để tránh mất tint đỏ.
    Color baseColor = Color.white;
    static readonly Color EnemyTintColor = new Color(1f, 0.35f, 0.35f, 1f);

    protected virtual void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (visualRoot == null && spriteRenderer != null) visualRoot = spriteRenderer.transform;
        if (visualRoot == null) visualRoot = transform;
        if (shadow == null) shadow = GetComponentInChildren<CharacterShadow>();
        visualBaseLocalPos = visualRoot.localPosition;
        visualBaseLocalRot = visualRoot.localRotation;
        lastPositionForSway = transform.position;
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (stats == null) return;
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) spriteRenderer.sprite = stats.idleSprite;
    }
#endif

    // Fire khi 1 ally chết — các ally khác subscribe để tăng angry.
    public static event System.Action<CharacterBase> OnAllyDied;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStaticEvents() => OnAllyDied = null;

    protected virtual void OnEnable()
    {
        WaveManager.OnWaveStart += EnterCombat;
        WaveManager.OnWaveEnd += ExitCombat;
        OnAllyDied += HandleAllyDied;
        GameManager.OnGameStateChanged += OnGameStateChanged;
    }

    protected virtual void OnDisable()
    {
        WaveManager.OnWaveStart -= EnterCombat;
        WaveManager.OnWaveEnd -= ExitCombat;
        OnAllyDied -= HandleAllyDied;
        GameManager.OnGameStateChanged -= OnGameStateChanged;
    }

    void HandleAllyDied(CharacterBase deadAlly)
    {
        if (IsDead || deadAlly == this || faction != Faction.Ally) return;
        AddAngry(stats.angryOnAllyDeath, AngryReason.AllyDied);
    }

    protected virtual void Start()
    {
        CurrentHP = EffectiveMaxHP;
        CurrentAngry = stats.initialAngry;

        if (!skipLeashClaim)
            ClaimLeashSlot();

        CharacterGrid.Register(this);
        SetSprite(stats.idleSprite);
        VFXManager.PlaySpawnPop(BodyCenter);

        // Random phase để sway không bị sync giữa các char spawn cùng lúc
        swayTimer = Random.Range(0f, Mathf.PI * 2f);
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

    // Force phe trước khi Start() chạy. Gọi ngay sau Instantiate (cùng lúc SetSpawnPosition).
    // CharacterGrid.Register dùng faction tại thời điểm Start → phải set trước đó.
    public void ForceSetFaction(Faction f)
    {
        faction = f;
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
                // Shop phase: đứng yên, separation tự đẩy. Không chạy về leash.
                break;

            case CharacterState.Waiting:
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0f)
                    EnterState(waitNextState);
                break;

            case CharacterState.Seeking:
                desiredMove = TickSeeking();
                break;

            case CharacterState.Attacking:
                TickAttacking();
                break;

            case CharacterState.Leashing:
                Vector2 toLeash = leashCenter - (Vector2)transform.position;
                if (toLeash.magnitude < 0.05f)
                    EnterState(CharacterState.Idle);
                else
                {
                    desiredMove = toLeash.normalized;
                    SetFacing(toLeash);
                }
                break;
        }

        Vector2 separation = ComputeSeparation();
        ApplyMovement(desiredMove + separation);

        // Knockback — áp trực tiếp lên vị trí, ngoài vòng ApplyMovement (không cần normalize)
        if (_knockbackVelocity.sqrMagnitude > 0.5f)
        {
            transform.position += (Vector3)(_knockbackVelocity * Time.deltaTime);
            CharacterGrid.UpdatePosition(this);
            _knockbackVelocity = Vector2.MoveTowards(_knockbackVelocity, Vector2.zero, KnockbackDecay * Time.deltaTime);
        }
        else
        {
            _knockbackVelocity = Vector2.zero;
        }
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
            // Shadow ở lại chân (root đang ở đầu/cursor khi drag)
            if (shadow != null)
                shadow.transform.localPosition = new Vector3(0f, -stats.dragHeadOffset, 0f);
            lastPositionForSway = transform.position; // tránh 1 cú "giật" lắc đi bộ ngay khi thả ra
        }
        else
        {
            if (shadow != null)
                shadow.transform.localPosition = Vector3.zero;
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
        // Target vừa chết → delay trước khi tìm địch tiếp theo
        if (currentTarget != null && currentTarget.IsDead)
        {
            currentTarget = null;
            EnterWaiting(CharacterState.Seeking);
            return Vector2.zero;
        }

        if (currentTarget == null)
            currentTarget = FindNearestEnemy();

        if (currentTarget == null)
            return Vector2.zero; // không còn địch nào: đứng yên tại chỗ, chờ hết wave mới về vị trí

        float distToTarget = Vector2.Distance(transform.position, currentTarget.transform.position);
        SetFacing(currentTarget.BodyCenter - BodyCenter);

        if (distToTarget <= EffectiveAttackRange)
        {
            EnterState(CharacterState.Attacking);
            // Random pha đầu tiên để các char cùng loại không sync đòn với nhau
            attackTimer = Random.Range(0f, EffectiveAttackInterval);
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
        if (distToTarget > EffectiveAttackRange)
        {
            EnterState(CharacterState.Seeking);
            return;
        }

        SetFacing(currentTarget.BodyCenter - BodyCenter);

        attackTimer += Time.deltaTime;
        if (attackTimer >= EffectiveAttackInterval)
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

    // Sprite mặc định nhìn trái → flipX khi target ở bên phải.
    // Cooldown 0.4s giữa các lần flip để tránh giật khi đứng gần điểm đến.
    protected void SetFacing(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) < 0.01f) return;
        bool wantFlip = direction.x > 0f;
        if (wantFlip == spriteRenderer.flipX) return; // không đổi, bỏ qua
        if (Time.time - lastFlipTime < stats.flipCooldown) return; // còn cooldown
        spriteRenderer.flipX = wantFlip;
        lastFlipTime = Time.time;
    }

    // Dừng ngẫu nhiên [actionDelayMin, actionDelayMax] giây rồi chuyển sang nextState.
    void EnterWaiting(CharacterState nextState)
    {
        waitNextState = nextState;
        waitTimer = Random.Range(stats.actionDelayMin, stats.actionDelayMax);
        EnterState(CharacterState.Waiting);
    }

    // ---------- Movement / Separation ----------

    protected virtual Vector2 MoveToward(Vector2 target)
    {
        Vector2 pos = transform.position;
        Vector2 dir = target - pos;
        if (dir.sqrMagnitude < 0.0001f) return Vector2.zero;
        return dir.normalized;
    }

    protected Vector2 ComputeSeparation()
    {
        Vector2 pos = BodyCenter;
        CharacterGrid.GetNearby(transform.position, stats.separationRadius, nearbyBuffer, faction, this);

        Vector2 push = Vector2.zero;
        foreach (CharacterBase other in nearbyBuffer)
        {
            Vector2 offset = pos - other.BodyCenter;
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

        Vector2 delta = direction.normalized * EffectiveMoveSpeed * Time.deltaTime;
        transform.position += (Vector3)delta;
        CharacterGrid.UpdatePosition(this);
    }

    // Giả lập dáng đi bằng cách nghiêng sprite qua lại + nảy lên xuống, thay vì vẽ
    // animation đi bộ thật. Đo bằng delta vị trí thực tế mỗi frame (không dùng cờ nội
    // bộ) để không bị "giật về pha 0" khi separation làm di chuyển dao động quanh 0.
    protected virtual void UpdateVisualSway()
    {
        float movedDist = ((Vector2)transform.position - (Vector2)lastPositionForSway).magnitude;
        lastPositionForSway = transform.position;

        if (movedDist > 0.001f)
            swayStopTimer = SwayStopDelay;
        else
            swayStopTimer -= Time.deltaTime;
        bool moving = swayStopTimer > 0f;

        swayTimer += Time.deltaTime * stats.swayFrequency;

        float sinV  = Mathf.Sin(swayTimer);
        float tilt   = moving ? sinV * stats.swayTiltAngle : 0f;
        // Bounce tính qua virtual hook — subclass có thể đổi hình dạng (vd: DogZ asymmetric).
        float bounce = ComputeSwayBounce(sinV, moving);

        // Xoay quanh GIỮA nhân vật (feet + halfH) thay vì quanh chân (sprite pivot).
        // headToFeet phải âm (từ center xuống chân), sau đó xoay rồi cộng bounce ở center.
        float halfH = stats.dragHeadOffset * 0.5f;
        Quaternion swayRot = visualBaseLocalRot * Quaternion.Euler(0f, 0f, tilt);
        Vector3 center = visualBaseLocalPos + new Vector3(0f, halfH, 0f);
        Vector3 centerToFeet = new Vector3(0f, -halfH, 0f);
        visualRoot.localRotation = swayRot;
        visualRoot.localPosition = center + swayRot * centerToFeet + new Vector3(0f, bounce, 0f);

        // Truyền normalized factor (0→1) thay vì raw units để không phụ thuộc swayBounceHeight
        float bounceFactor = moving ? Mathf.Abs(Mathf.Sin(swayTimer)) : 0f;
        shadow?.UpdateShadow(bounceFactor);
    }

    // Override trong subclass để thay đổi hình dạng bounce (vd: DogZ hất đầu cao hơn đuôi).
    // sinValue = Mathf.Sin(swayTimer) ∈ [-1, 1]; moving = nhân vật đang di chuyển.
    protected virtual float ComputeSwayBounce(float sinValue, bool moving)
        => moving ? Mathf.Abs(sinValue) * stats.swayBounceHeight : 0f;

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

        // Xoay quanh ĐẦU (= root/cursor = gốc local), không phải quanh chân (sprite pivot).
        // Kỹ thuật: tính vector "đầu → chân" rồi xoay nó, đặt visualRoot tại vị trí chân sau xoay.
        // Kết quả: đầu luôn ghim tại cursor, thân/chân đung đưa sang hai bên theo hướng kéo.
        Quaternion rot = visualBaseLocalRot * Quaternion.Euler(0f, 0f, dragTiltAngle);
        Vector3 headToFeet = visualBaseLocalPos - new Vector3(0f, stats.dragHeadOffset, 0f);
        visualRoot.localRotation = rot;
        visualRoot.localPosition = rot * headToFeet;
    }

    // ---------- Leash slot (vị trí đứng trong shop) ----------

    protected virtual void ClaimLeashSlot()
    {
        // Char đặt sẵn trong scene: giữ nguyên vị trí, ghi nhận làm home.
        // Char spawn mới (qua CharacterSpawner): đã được đặt tại SpawnArea trước khi Start() chạy.
        leashCenter = transform.position;
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
        // Lấy vị trí feet trong world TRƯỚC KHI reset bất cứ thứ gì.
        // visualRoot.position = world pos của sprite pivot (= chân khi pivot ở chân).
        // Cách này chính xác bất kể tilt angle đang là bao nhiêu khi thả chuột.
        Vector2 feetWorld = visualRoot.position;

        IsDragging = false;
        dragVelocity = Vector2.zero;
        dragTiltAngle = 0f;
        dragTiltAngularVelocity = 0f;

        // Đặt root đúng chỗ feet đang đứng → visual sẽ reset về localBase mà không giật
        transform.position = new Vector3(feetWorld.x, feetWorld.y, transform.position.z);
        visualRoot.localPosition = visualBaseLocalPos;
        visualRoot.localRotation = visualBaseLocalRot;

        CharacterGrid.UpdatePosition(this);

        if (!WaveManager.IsWaveActive)
            SetLeashCenter(feetWorld);
    }

    // ---------- Public API ----------

    public virtual void EnterCombat()
    {
        if (IsDead) return;
        leashCenter = transform.position;
        AddAngry(stats.angryOnRoundStart, AngryReason.RoundStart);
        currentTarget = null;
        EnterWaiting(CharacterState.Seeking); // delay ngẫu nhiên trước khi bắt đầu di chuyển
    }

    public virtual void ExitCombat()
    {
        if (IsDead) return;
        currentTarget = null;
        attackCount = 0;
        EnterWaiting(CharacterState.Leashing); // delay ngẫu nhiên trước khi quay về
    }

    void OnGameStateChanged(GameState state)
    {
        if (state == GameState.Shop)
        {
            // Vào shop: dừng leashing (char đứng ở chỗ hiện tại, separation lo phần còn lại)
            if (State == CharacterState.Leashing)
                EnterState(CharacterState.Idle);
        }
    }

    // ── Knockback ─────────────────────────────────────────────────────────────
    // Gọi từ bất kỳ nhân vật nào để đẩy nhân vật này ra xa theo hướng cho trước.
    // force: pixels/giây ban đầu. Tự giảm dần mỗi frame theo KnockbackDecay.
    private Vector2 _knockbackVelocity;
    private const float KnockbackDecay = 600f; // pixels/s²

    public void ApplyKnockback(Vector2 direction, float force)
    {
        if (IsDead) return;
        _knockbackVelocity += direction.normalized * force;
    }

    // Debug toggle — tất cả nhân vật không nhận sát thương khi true
    public static bool DebugInvincible = false;

    public virtual void TakeDamage(float amount)
    {
        if (IsDead) return;
        if (DebugInvincible) return;
        CurrentHP -= amount;
        VFXManager.PlayBloodHit(BodyCenter, amount, EffectiveMaxHP);
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
        spriteRenderer.color = baseColor; // reset về baseColor (trắng hoặc đỏ tint nếu đã SwitchToEnemy)
        hitFlashRoutine = null;
    }

    public virtual void Heal(float amount)
    {
        if (IsDead) return;
        CurrentHP = Mathf.Min(EffectiveMaxHP, CurrentHP + amount);
        VFXManager.PlayBuffArrow(BodyCenter, VFXManager.ColorHP);
    }

    public virtual void AddAngry(float amount, AngryReason reason)
    {
        if (IsDead || amount <= 0f) return;
        CurrentAngry = Mathf.Min(CurrentAngry + amount, stats.maxAngry);
        if (CurrentAngry >= stats.maxAngry && faction == Faction.Ally)
            SwitchToEnemy();
    }

    // Debug only: thay đổi angry tự do (kể cả giảm). Trigger SwitchToEnemy khi chạm max.
    public void DebugAddAngry(float delta)
    {
        CurrentAngry = Mathf.Clamp(CurrentAngry + delta, 0f, stats.maxAngry);
        if (CurrentAngry >= stats.maxAngry && faction == Faction.Ally)
            SwitchToEnemy();
    }

    // Đổi phe: unregister khỏi Ally grid → đổi faction → re-register là Enemy.
    // Nếu đang trong wave thì lập tức EnterCombat đánh lại đồng đội cũ.
    void SwitchToEnemy()
    {
        CharacterGrid.Unregister(this);
        faction = Faction.Enemy;
        CharacterGrid.Register(this);
        currentTarget = null;

        // Phủ màu đỏ lên sprite để báo hiệu đã chuyển phe.
        // Nếu đang flash hit, dừng ngay để tránh flash trả về Color.white.
        if (hitFlashRoutine != null) { StopCoroutine(hitFlashRoutine); hitFlashRoutine = null; }
        baseColor = EnemyTintColor;
        if (spriteRenderer != null) spriteRenderer.color = baseColor;

        if (WaveManager.IsWaveActive)
            EnterCombat();
    }

    // Gọi bởi FeedingManager khi có đủ corn. Hồi full HP, angry không tăng từ đói.
    public virtual void Feed()
    {
        CurrentHP = EffectiveMaxHP;
        VFXManager.PlayFeedHappy(BodyCenter);
    }

    // Gọi bởi FeedingManager khi thiếu corn.
    public void SkipFeed()
    {
        AddAngry(stats.angryPerHunger, AngryReason.Hungry);
        VFXManager.PlayFeedAngry(BodyCenter);
    }

    protected virtual void Die()
    {
        VFXManager.PlayDeathBurst(BodyCenter);
        IsDead = true;
        CharacterGrid.Unregister(this);
        if (faction == Faction.Ally)
            OnAllyDied?.Invoke(this);
        else
            PlayerWallet.Instance?.Earn(stats.killReward);
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
        attackCount++;
        target.TakeDamage(EffectiveDamage);
        FlashSprite(stats.attackSprite);
    }

    // Gọi trong ExecuteAttack() override của subclass khi skill kích hoạt (đòn X).
    // Truyền màu tùy loại buff: VFXManager.ColorHP / ColorDamage / ColorSpeed.
    protected void PlaySkillVFX(Color color)
    {
        Vector2 pos = vfxSpawnPoint != null ? (Vector2)vfxSpawnPoint.position : BodyCenter;
        VFXManager.PlayBuffArrow(pos, color);
    }

    // Spawn VFX prefab tại vfxSpawnPoint (fallback về gốc nhân vật nếu chưa gán).
    // Prefab tự hủy sau khi xong (particle Stop Action = Destroy, hoặc dùng Destroy(go, t)).
    protected void SpawnVFX(GameObject prefab)
    {
        if (prefab == null) return;
        Vector3 pos = vfxSpawnPoint != null ? vfxSpawnPoint.position : transform.position;
        Object.Instantiate(prefab, pos, Quaternion.identity);
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

    // Luôn hiển thị (không cần chọn): separation radius + attack range + line to target.
    // Màu theo phe: xanh lá = Ally, đỏ = Enemy. Độ trong suốt = đang Idle.
    protected virtual void OnDrawGizmos()
    {
        if (stats == null) return;

        bool isAlly = faction == Faction.Ally;
        bool active = Application.isPlaying && State != CharacterState.Idle;

        Vector3 bodyCenter = transform.position + Vector3.up * (stats.centerOffset * transform.lossyScale.y);

        // Separation radius — cyan
        Gizmos.color = new Color(0f, 1f, 1f, active ? 0.4f : 0.15f);
        Gizmos.DrawWireSphere(bodyCenter, stats.separationRadius);

        // Attack range — đỏ cam
        Gizmos.color = new Color(1f, 0.3f, 0f, active ? 0.7f : 0.2f);
        Gizmos.DrawWireSphere(bodyCenter, stats.attackRange);

        // Line đến target khi đang Seeking/Attacking
        if (Application.isPlaying && currentTarget != null && !currentTarget.IsDead)
        {
            Gizmos.color = isAlly ? new Color(0.2f, 1f, 0.2f, 0.8f) : new Color(1f, 0.2f, 0.2f, 0.8f);
            Gizmos.DrawLine(bodyCenter, currentTarget.transform.position + Vector3.up * (currentTarget.stats.centerOffset * currentTarget.transform.lossyScale.y));
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (stats == null) return;

#if UNITY_EDITOR
        // Label số liệu ngay trong Scene view khi chọn char
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(transform.position + Vector3.up * (stats.centerOffset * transform.lossyScale.y * 2f + 0.5f),
            $"spd:{stats.moveSpeed}  sep:{stats.separationRadius}  atk:{stats.attackRange}");
#endif
    }
}
