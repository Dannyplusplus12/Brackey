using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Đếm ngược 3-2-1 trong arenaStartDelay trước khi wave bắt đầu.
// Hỗ trợ 2 chế độ hiển thị — chỉ cần điền 1 trong 2:
//   Sprite mode : gán numberSprites[0]=3, [1]=2, [2]=1 và displayImage
//   Text mode   : gán countdownText (TextMeshProUGUI)
// countdownRoot là panel cha — script tự bật/tắt nó.
public class WaveCountdown : MonoBehaviour
{
    [Header("Root panel (tự bật/tắt)")]
    [SerializeField] GameObject countdownRoot;

    [Header("Sprite mode (ưu tiên nếu có)")]
    [Tooltip("Index 0 = sprite số '3', 1 = '2', 2 = '1'")]
    [SerializeField] Sprite[] numberSprites;
    [SerializeField] Image displayImage;

    [Header("Text mode (fallback nếu không có sprites)")]
    [SerializeField] TMPro.TextMeshProUGUI countdownText;

    [Header("Animation (tuỳ chọn)")]
    [Tooltip("Scale nhảy lên khi hiện số mới, rồi lerp về 1")]
    [SerializeField] float punchScale = 1.4f;
    [SerializeField] float punchDuration = 0.15f;

    Coroutine activeRoutine;

    void Start()
    {
        if (countdownRoot != null) countdownRoot.SetActive(false);
    }

    void OnEnable()  => GameManager.OnGameStateChanged += OnStateChanged;
    void OnDisable() => GameManager.OnGameStateChanged -= OnStateChanged;

    void OnStateChanged(GameState state)
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);

        if (state == GameState.Arena)
            activeRoutine = StartCoroutine(CountdownRoutine());
        else
            Hide();
    }

    IEnumerator CountdownRoutine()
    {
        float delay = GameManager.Instance != null ? GameManager.Instance.postFeedDelay : 0.5f;
        int steps = Mathf.Max(1, Mathf.RoundToInt(delay));

        Show();

        for (int n = steps; n >= 1; n--)
        {
            SetNumber(n);
            if (punchScale > 1f) StartCoroutine(PunchRoutine());
            yield return new WaitForSeconds(1f);
        }

        Hide();
        activeRoutine = null;
    }

    void SetNumber(int n)
    {
        bool useSprite = displayImage != null
                         && numberSprites != null
                         && numberSprites.Length > 0;

        if (useSprite)
        {
            // Sprites: index 0 = số lớn nhất, đếm ngược xuống
            int idx = numberSprites.Length - n;
            if (idx >= 0 && idx < numberSprites.Length)
                displayImage.sprite = numberSprites[idx];
            displayImage.gameObject.SetActive(true);
            if (countdownText != null) countdownText.gameObject.SetActive(false);
        }
        else if (countdownText != null)
        {
            countdownText.text = n.ToString();
            countdownText.gameObject.SetActive(true);
            if (displayImage != null) displayImage.gameObject.SetActive(false);
        }
    }

    IEnumerator PunchRoutine()
    {
        Transform t = displayImage != null ? displayImage.transform : countdownText?.transform;
        if (t == null) yield break;

        t.localScale = Vector3.one * punchScale;
        float elapsed = 0f;
        while (elapsed < punchDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            t.localScale = Vector3.Lerp(Vector3.one * punchScale, Vector3.one, elapsed / punchDuration);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    void Show() { if (countdownRoot != null) countdownRoot.SetActive(true); }
    void Hide() { if (countdownRoot != null) countdownRoot.SetActive(false); }
}
