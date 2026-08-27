using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// 1 card trong roster: portrait + angry bar dọc (bên phải) + HP bar ngang (bên dưới).
// Hỗ trợ kéo-thả để đổi thứ tự trong danh sách.
public class CharacterRosterEntry : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] Image portraitImage;
    [SerializeField] Sprite cornIcon;   // drag corn sprite vào đây trong prefab

    // Fill dùng anchorMax (không cần sprite, fill kín edge-to-edge)
    RectTransform angryFillRT;
    RectTransform hpFillRT;

    CharacterBase target;

    // ── Drag ──────────────────────────────────────────────────────────
    Canvas rootCanvas;
    RectTransform rectTransform;
    CanvasGroup canvasGroup;
    Transform contentParent;
    GameObject placeholder;
    Vector2 dragOffset;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    void OnEnable()  => FeedingManager.OnFeedResult += HandleFeedResult;
    void OnDisable() => FeedingManager.OnFeedResult -= HandleFeedResult;

    void HandleFeedResult(CharacterBase fed, int cost, bool wasFed)
    {
        if (fed != target) return;
        StartCoroutine(FeedPopupCoroutine(cost, wasFed));
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

    // ── Drag handlers ─────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        rootCanvas    = GetComponentInParent<Canvas>().rootCanvas;
        contentParent = transform.parent;

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
        transform.SetParent(contentParent, false);
        transform.SetSiblingIndex(placeholder.transform.GetSiblingIndex());

        Destroy(placeholder);
        placeholder = null;

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha          = 1f;
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

    // ── Feed popup ────────────────────────────────────────────────────
    // Coroutine chạy trên CharacterRosterEntry chỉ dùng để BUILD popup.
    // Animation chạy trên FeedPopupAnimator gắn lên popup GO (sống trên canvas root
    // nên không bị kill khi ShopRoot bị deactivate).

    IEnumerator FeedPopupCoroutine(int cost, bool wasFed)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) yield break;
        canvas = canvas.rootCanvas;

        // Tạo popup trên canvas root (không nằm trong ShopRoot → không bị mask / hide)
        GameObject popup = new GameObject("_FeedPopup",
            typeof(RectTransform), typeof(CanvasGroup),
            typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        popup.transform.SetParent(canvas.transform, false);

        CanvasGroup cg    = popup.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable   = false;
        cg.alpha          = 0f;

        HorizontalLayoutGroup hlg  = popup.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing                = 4f;
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;

        ContentSizeFitter csf = popup.GetComponent<ContentSizeFitter>();
        csf.horizontalFit     = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit       = ContentSizeFitter.FitMode.PreferredSize;

        // Text
        Color textColor = wasFed
            ? new Color(1f, 0.85f, 0.15f)
            : new Color(1f, 0.35f, 0.2f);

        GameObject textGO = new GameObject("Label", typeof(RectTransform));
        textGO.transform.SetParent(popup.transform, false);
        var tmp       = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = $"-{cost}";
        tmp.color     = textColor;
        tmp.fontSize  = 20f;
        tmp.fontStyle = FontStyles.Bold;

        // Corn icon (tuỳ chọn)
        if (cornIcon != null)
        {
            GameObject iconGO = new GameObject("Icon",
                typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            iconGO.transform.SetParent(popup.transform, false);
            iconGO.GetComponent<Image>().sprite         = cornIcon;
            iconGO.GetComponent<Image>().preserveAspect = true;
            var le             = iconGO.GetComponent<LayoutElement>();
            le.preferredWidth  = 22f;
            le.preferredHeight = 22f;
        }

        // 1 frame để ContentSizeFitter layout
        yield return null;

        // Vị trí: góc trên-phải của card
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        RectTransform canvasRT = canvas.transform as RectTransform;
        Camera uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT,
            RectTransformUtility.WorldToScreenPoint(uiCam, corners[2]),
            uiCam, out Vector2 localPos);

        RectTransform popupRT    = popup.GetComponent<RectTransform>();
        popupRT.pivot            = new Vector2(0f, 1f);
        popupRT.anchorMin        = popupRT.anchorMax = Vector2.zero;
        popupRT.anchoredPosition = localPos + new Vector2(8f, 0f);

        // Giao animation cho component trên popup GO — tránh bị kill cùng ShopRoot
        FeedPopupAnimator.Attach(popup, cg);
    }
}

// ── Tự animate và tự destroy — chạy trên popup GO (canvas root), không bị ShopRoot.SetActive(false) kill ──
public class FeedPopupAnimator : MonoBehaviour
{
    public static void Attach(GameObject popupGO, CanvasGroup cg)
    {
        var a = popupGO.AddComponent<FeedPopupAnimator>();
        a._cg = cg;
        a.StartCoroutine(a.Run());
    }

    CanvasGroup _cg;

    System.Collections.IEnumerator Run()
    {
        // Fade in
        float t = 0f;
        while (t < 0.15f) { _cg.alpha = Mathf.Lerp(0f, 1f, t / 0.15f); t += Time.unscaledDeltaTime; yield return null; }
        _cg.alpha = 1f;

        yield return new WaitForSecondsRealtime(0.8f);

        // Fade out
        t = 0f;
        while (t < 0.25f) { _cg.alpha = Mathf.Lerp(1f, 0f, t / 0.25f); t += Time.unscaledDeltaTime; yield return null; }

        Destroy(gameObject);
    }
}
