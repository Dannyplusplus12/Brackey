using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// 1 card trong roster: portrait + angry bar dọc (bên phải) + HP bar ngang (bên dưới).
// Hỗ trợ kéo-thả để đổi thứ tự trong danh sách.
public class CharacterRosterEntry : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image portraitImage;

    [Header("Feed Popup (gán sẵn trong prefab, mặc định inactive)")]
    [SerializeField] GameObject feedPopup;
    [SerializeField] TextMeshProUGUI feedCostText;
    [SerializeField] Image feedCornIcon;

    [Header("Feed Popup - Chỉnh tại đây (không chỉnh trong FeedPopup prefab)")]
    [Tooltip("Offset X: sang phải card. Offset Y: lên/xuống so với giữa card")]
    [SerializeField] Vector2 popupOffset = new Vector2(8f, 0f);
    [Tooltip("Scale của popup (1 = mặc định)")]
    [SerializeField] float popupScale = 1f;

    // Fill dùng anchorMax (không cần sprite, fill kín edge-to-edge)
    RectTransform angryFillRT;
    RectTransform hpFillRT;

    CharacterBase target;
    public CharacterBase BoundCharacter => target;

    // ── Drag ──────────────────────────────────────────────────────────
    Canvas rootCanvas;
    RectTransform rectTransform;
    CanvasGroup canvasGroup;
    Transform contentParent;
    GameObject placeholder;
    Vector2 dragOffset;

    [Header("Auto-scroll khi drag")]
    [SerializeField] float autoScrollZone  = 70f;   // screen px từ mép viewport
    [SerializeField] float autoScrollSpeed = 0.8f;  // normalised units / giây

    ScrollRect    _scrollRect;   // cached: dùng cho IScrollHandler + auto-scroll
    RectTransform _viewportRT;   // bounds để detect vùng edge
    bool          _isDragging;
    Coroutine     _autoScrollCR;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Cache scroll rect ngay lúc Awake (trước khi bị reparent khi drag)
        _scrollRect = GetComponentInParent<ScrollRect>();
        if (_scrollRect != null)
            _viewportRT = _scrollRect.viewport != null
                ? _scrollRect.viewport
                : _scrollRect.GetComponent<RectTransform>();
    }

    void OnEnable()  => FeedingManager.OnFeedResult += HandleFeedResult;
    void OnDisable() => FeedingManager.OnFeedResult -= HandleFeedResult;

    void HandleFeedResult(CharacterBase fed, int cost, bool wasFed)
    {
        if (fed != target) return;
        if (feedPopup == null || feedCostText == null) return;

        Canvas rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        if (rootCanvas == null) return;

        // Set nội dung
        Color col = wasFed
            ? new Color(1f, 0.85f, 0.15f)
            : new Color(1f, 0.35f, 0.2f);
        feedCostText.text  = $"-{cost}";
        feedCostText.color = col;

        // Reparent lên canvas root ngay (thoát RectMask2D của ScrollView)
        feedPopup.transform.SetParent(rootCanvas.transform, false);
        feedPopup.SetActive(true);

        // Đặt vị trí bên phải card — không cần yield vì không dùng size của popup
        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                       ? null : rootCanvas.worldCamera;
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        // Dùng trung điểm cạnh phải (giữa corners[2] top-right và corners[3] bottom-right)
        Vector3 rightMid = (corners[2] + corners[3]) * 0.5f;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform,
            RectTransformUtility.WorldToScreenPoint(uiCam, rightMid),
            uiCam, out Vector2 localPos);

        RectTransform popupRT    = feedPopup.GetComponent<RectTransform>();
        popupRT.localScale       = Vector3.one * popupScale;
        popupRT.anchorMin        = new Vector2(0.5f, 0.5f);
        popupRT.anchorMax        = new Vector2(0.5f, 0.5f);
        popupRT.pivot            = new Vector2(0f, 0.5f);
        popupRT.anchoredPosition = localPos + popupOffset;

        // FeedPopupAnimator chạy độc lập trên popup GO — không bị ShopRoot kill
        FeedPopupAnimator.Attach(feedPopup, transform);
    }

    // ─────────────────────────────────────────────────────────────────
    public void Bind(CharacterBase character)
    {
        target = character;

        portraitImage = FindImageByName("PortraitImage");
        angryFillRT   = FindTransformByName("AngryBarFill");
        hpFillRT      = FindTransformByName("HPBarFill");

        // Anchor ban đầu: full stretch, sau đó chỉ chỉnh anchorMax
        SetFullStretch(angryFillRT);
        SetFullStretch(hpFillRT);

        CharacterStats stats = character.Stats;
        if (portraitImage != null)
        {
            portraitImage.sprite = stats.idleSprite;
            portraitImage.preserveAspect = true;
            portraitImage.rectTransform.anchoredPosition = stats.portraitOffset;
            portraitImage.rectTransform.localScale = Vector3.one * stats.portraitScale;
        }

        RefreshAngry();
        RefreshHP();
    }

    Image FindImageByName(string goName)
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
            if (t.name == goName) { var img = t.GetComponent<Image>(); if (img) return img; }
        return null;
    }

    RectTransform FindTransformByName(string goName)
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
            if (t.name == goName) return t as RectTransform;
        return null;
    }

    static void SetFullStretch(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    void Update()
    {
        if (target == null || target.IsDead)
        {
            gameObject.SetActive(false);
            return;
        }
        RefreshAngry();
        RefreshHP();
    }

    // Angry bar dọc: anchorMax.y = pct
    void RefreshAngry()
    {
        if (angryFillRT == null || target == null) return;
        float max = target.Stats.maxAngry;
        float pct = max > 0f ? Mathf.Clamp01(target.CurrentAngry / max) : 0f;
        angryFillRT.anchorMax = new Vector2(1f, pct);
        angryFillRT.offsetMax = Vector2.zero;
    }

    // HP bar ngang: anchorMax.x = pct
    void RefreshHP()
    {
        if (hpFillRT == null || target == null) return;
        float max = target.MaxHP;
        float pct = max > 0f ? Mathf.Clamp01(target.CurrentHP / max) : 1f;
        hpFillRT.anchorMax = new Vector2(pct, 1f);
        hpFillRT.offsetMax = Vector2.zero;
    }

    // ── Hover → StatBar ───────────────────────────────────────────────
    [Header("Tooltip Position")]
    [SerializeField] TooltipDirection tooltipDirection = TooltipDirection.Right;
    [SerializeField] bool             tooltipAlignEnd  = false;
    [SerializeField] float            tooltipGap       = 8f;

    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData _)
    {
        if (target == null) return;
        TooltipSystem.ShowImmediate(TooltipData.FromCharacterLive(target, rectTransform,
            tooltipDirection, tooltipAlignEnd, tooltipGap));
        CharacterStatBar.Instance?.ShowLive(target);
    }

    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData _)
    {
        TooltipSystem.Hide();
        CharacterStatBar.Instance?.Hide();
    }

    // ── Drag handlers ─────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        rootCanvas    = GetComponentInParent<Canvas>().rootCanvas;
        contentParent = transform.parent;

        // Bắt đầu auto-scroll (phải capture trước khi reparent)
        _isDragging = true;
        if (_scrollRect != null)
        {
            if (_autoScrollCR != null) StopCoroutine(_autoScrollCR);
            _autoScrollCR = StartCoroutine(AutoScrollWhileDragging());
        }

        placeholder = new GameObject("_DragPlaceholder", typeof(RectTransform));
        placeholder.transform.SetParent(contentParent, false);
        placeholder.transform.SetSiblingIndex(transform.GetSiblingIndex());

        LayoutElement le   = placeholder.AddComponent<LayoutElement>();
        le.preferredHeight = rectTransform.rect.height;
        le.minHeight       = rectTransform.rect.height;
        le.flexibleWidth   = 1f;

        transform.SetParent(rootCanvas.transform, true);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform,
            eventData.position, eventData.pressEventCamera,
            out Vector2 pointerLocal);
        dragOffset = rectTransform.anchoredPosition - pointerLocal;

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha          = 0.85f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform,
            eventData.position, eventData.pressEventCamera,
            out Vector2 localPos))
        {
            rectTransform.anchoredPosition = localPos + dragOffset;
        }

        int newIndex = GetNearestSlotIndex(eventData.position);
        if (placeholder.transform.GetSiblingIndex() != newIndex)
            placeholder.transform.SetSiblingIndex(newIndex);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
        if (_autoScrollCR != null) { StopCoroutine(_autoScrollCR); _autoScrollCR = null; }
        // Reset velocity do auto-scroll tích lũy khi set normalizedPosition trực tiếp,
        // tránh inertia lạ sau khi drag xong làm scroll wheel bị lệch.
        _scrollRect?.StopMovement();

        transform.SetParent(contentParent, false);
        transform.SetSiblingIndex(placeholder.transform.GetSiblingIndex());

        Destroy(placeholder);
        placeholder = null;

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha          = 1f;
    }

    // ── Auto-scroll khi drag gần mép viewport ─────────────────────────
    IEnumerator AutoScrollWhileDragging()
    {
        while (_isDragging && _scrollRect != null && _viewportRT != null)
        {
            Vector3[] corners = new Vector3[4];
            _viewportRT.GetWorldCorners(corners);
            // corners[0]=BL, [1]=TL, [2]=TR, [3]=BR (screen space for Overlay canvas)
            float top    = corners[1].y;
            float bottom = corners[0].y;

            float mouseY = Input.mousePosition.y;
            float delta  = 0f;

            if (mouseY < top && mouseY > bottom)   // pointer ở trong viewport
            {
                float fromTop    = top    - mouseY;
                float fromBottom = mouseY - bottom;

                if (fromTop < autoScrollZone)
                {
                    // Gần mép trên → scroll lên (normalised tăng)
                    delta = +(1f - fromTop / autoScrollZone);
                }
                else if (fromBottom < autoScrollZone)
                {
                    // Gần mép dưới → scroll xuống (normalised giảm)
                    delta = -(1f - fromBottom / autoScrollZone);
                }
            }

            if (Mathf.Abs(delta) > 0.001f)
            {
                _scrollRect.verticalNormalizedPosition = Mathf.Clamp01(
                    _scrollRect.verticalNormalizedPosition + delta * autoScrollSpeed * Time.unscaledDeltaTime
                );
            }

            yield return null;
        }
        _autoScrollCR = null;
    }

    int GetNearestSlotIndex(Vector2 screenPos)
    {
        int childCount    = contentParent.childCount;
        float closestDist = float.MaxValue;
        int closestIndex  = 0;

        for (int i = 0; i < childCount; i++)
        {
            RectTransform child = contentParent.GetChild(i) as RectTransform;
            if (child == null) continue;
            float dist = Mathf.Abs(screenPos.y - GetScreenCenterY(child));
            if (dist < closestDist) { closestDist = dist; closestIndex = i; }
        }
        return closestIndex;
    }

    static float GetScreenCenterY(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        return (corners[0].y + corners[2].y) * 0.5f;
    }

}

// Tự fade và trả popup về parent ban đầu sau khi xong.
// Gắn động lên feedPopup GO — sống trên canvas root nên không bị ShopRoot kill.
public class FeedPopupAnimator : MonoBehaviour
{
    Transform _originalParent;

    public static void Attach(GameObject popupGO, Transform originalParent)
    {
        var a = popupGO.GetComponent<FeedPopupAnimator>();
        if (a == null) a = popupGO.AddComponent<FeedPopupAnimator>();
        a._originalParent = originalParent;
        a.StopAllCoroutines();
        a.StartCoroutine(a.Run());
    }

    System.Collections.IEnumerator Run()
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 1f;

        yield return new WaitForSecondsRealtime(0.8f);

        float t = 0f;
        while (t < 0.25f)
        {
            cg.alpha = Mathf.Lerp(1f, 0f, t / 0.25f);
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        gameObject.SetActive(false);
        if (_originalParent != null)
            transform.SetParent(_originalParent, false);
    }
}
