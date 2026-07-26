using UnityEngine;
using UnityEngine.EventSystems;

public class MapButtonHoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Số thứ tự Map tương ứng (1 cho Map 01, 2 cho Map 02, 3 cho Map 03)")]
    public int mapIndex = 1;

    [Tooltip("Kéo ContinuePanel vào đây (nếu để trống script sẽ tự tìm)")]
    public ContinueManager continueManager;

    void Start()
    {
        if (continueManager == null)
        {
            continueManager = GetComponentInParent<ContinueManager>();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (continueManager != null)
        {
            continueManager.ShowPreview(mapIndex);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (continueManager != null)
        {
            continueManager.HidePreview();
        }
    }
}
