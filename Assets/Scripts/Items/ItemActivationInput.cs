using UnityEngine;

// Xử lý input kích hoạt item Active (phím 1-4), độc lập với UI hotbar.
// Tự bootstrap — KHÔNG cần gắn tay vào scene. Script tự tạo GO persistent.
[DefaultExecutionOrder(100)]
public class ItemActivationInput : MonoBehaviour
{
    static readonly KeyCode[] Keys =
    {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        var go = new GameObject("[ItemActivationInput]");
        DontDestroyOnLoad(go);
        go.AddComponent<ItemActivationInput>();
    }

    void Update()
    {
        // Hoạt động ở cả Shop lẫn Arena
        for (int i = 0; i < Keys.Length; i++)
        {
            if (Input.GetKeyDown(Keys[i]))
            {
                PlayerInventory.Instance?.ActivateSlot(i);
                break;
            }
        }
    }
}
