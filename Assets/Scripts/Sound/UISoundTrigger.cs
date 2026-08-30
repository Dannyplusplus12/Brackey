using UnityEngine;
using UnityEngine.EventSystems;

// Gắn lên bất kỳ UI element nào để tự động phát sound khi hover / click.
// Không cần code thêm — chỉ Add Component và chỉnh SoundId trong Inspector nếu muốn.
//
// Mặc định:
//   hoverSound = SoundId.UIHover
//   clickSound = SoundId.UIClick
//
// Dùng playHover = false hoặc playClick = false để tắt riêng từng loại.
[AddComponentMenu("Sound/UI Sound Trigger")]
public class UISoundTrigger : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] bool      playHover  = true;
    [SerializeField] SoundId   hoverSound = SoundId.UIHover;

    [SerializeField] bool      playClick  = true;
    [SerializeField] SoundId   clickSound = SoundId.UIClick;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (playHover) SoundManager.Play(hoverSound);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (playClick) SoundManager.Play(clickSound);
    }
}
