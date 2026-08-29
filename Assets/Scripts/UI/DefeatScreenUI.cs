using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Overlay màn hình thua. Hiển thị khi GameManager.OnDefeat được fire.
///
/// Hierarchy tạo bởi DefeatScreenSetupTool (Tools > UI > Build Defeat Screen).
/// GameObject này nên inactive từ đầu — script tự Show() khi cần.
/// </summary>
public class DefeatScreenUI : MonoBehaviour
{
    [Header("Overlay")]
    [Tooltip("CanvasGroup trên root để fade in toàn bộ overlay")]
    public CanvasGroup canvasGroup;

    [Header("Stats Text")]
    public TMP_Text wavesText;
    public TMP_Text killsText;
    public TMP_Text charsBoughtText;
    public TMP_Text itemsBoughtText;
    public TMP_Text damageText;
    public TMP_Text cornText;

    [Header("Button")]
    public Button restartButton;

    [Header("Animation")]
    [Tooltip("Thời gian fade in (giây)")]
    public float fadeInDuration = 0.6f;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha          = 0f;
            canvasGroup.interactable   = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (restartButton != null)
            restartButton.onClick.AddListener(OnClickRestart);
    }

    void OnEnable()  => GameManager.OnDefeat += Show;
    void OnDisable() => GameManager.OnDefeat -= Show;

    // ── Show ──────────────────────────────────────────────────────────────────

    void Show()
    {
        PopulateStats();
        StopAllCoroutines();
        StartCoroutine(FadeIn());
    }

    void PopulateStats()
    {
        var t = RunTracker.Instance;
        if (t == null) return;

        if (wavesText      != null) wavesText.text      = t.WavesStarted.ToString();
        if (killsText      != null) killsText.text      = t.EnemiesKilled.ToString();
        if (charsBoughtText!= null) charsBoughtText.text= t.CharsBought.ToString();
        if (itemsBoughtText!= null) itemsBoughtText.text= t.ItemsBought.ToString();
        if (damageText     != null) damageText.text     = Mathf.RoundToInt(t.DamageDealt).ToString();
        if (cornText       != null) cornText.text       = t.CornEarned.ToString();
    }

    IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        canvasGroup.alpha          = 0f;
        canvasGroup.interactable   = false;
        canvasGroup.blocksRaycasts = true;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime; // dùng unscaled để timescale = 0 vẫn chạy
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha        = 1f;
        canvasGroup.interactable = true;
    }

    // ── Button ────────────────────────────────────────────────────────────────

    void OnClickRestart()
    {
        Time.timeScale = 1f; // reset timescale phòng debug đang chỉnh
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
