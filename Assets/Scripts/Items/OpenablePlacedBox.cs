using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class OpenablePlacedBox : MonoBehaviour, IInteractable
{
    [Header("1. Chữ Nhắc Phím Tương Tác (Prompt UI)")]
    public string englishPrompt = "[F] Open Box";
    public string vietnamesePrompt = "[F] Mở thùng hàng";

    [Header("2. Hộp Đã Mở Cần Đổi (BoxOpen Prefab / Scene Object)")]
    [Tooltip("Kéo Prefab hoặc GameObject 'BoxOpen' (mô hình hộp đã mở) vào đây")]
    public GameObject openBoxPrefab;

    [Header("3. Cấu Hình Thời Gian Fade Đen Màn Hình")]
    public float delayBeforeFade = 0.2f;
    public float fadeDuration = 1.5f;
    public float holdBlackDuration = 0.5f;
    public float fadeInDuration = 1.5f;

    [Header("4. Âm Thanh Mở Hộp (Tùy chọn)")]
    public AudioClip openBoxSound;
    [Range(0f, 1f)] public float soundVolume = 0.8f;

    [Header("5. UI Màn Hình Đen (Fade Image - Tùy chọn)")]
    public Image fadeScreenImage;

    private Collider boxCollider;
    private InteractPrompt interactPrompt;
    private bool hasOpened = false;
    private bool isProcessingOpen = false;
    private FadeCoroutineRunner coroutineRunner;

    void Awake()
    {
        boxCollider = GetComponent<Collider>();
        interactPrompt = GetComponent<InteractPrompt>();

        // TẮT HOẶC XÓA CÁC SCRIPT TƯƠNG TÁC CŨ ĐỂ TRÁNH XUNG ĐỘT (NPCDeliveryBox, InteractableItem)
        NPCDeliveryBox oldDelivery = GetComponent<NPCDeliveryBox>();
        if (oldDelivery != null) oldDelivery.enabled = false;

        InteractableItem oldItem = GetComponent<InteractableItem>();
        if (oldItem != null) oldItem.enabled = false;

        // TỰ ĐỘNG CĂN CHỈNH LẠI TÂM BOX COLLIDER
        BoxCollider boxCol = GetComponent<BoxCollider>();
        if (boxCol != null)
        {
            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr == null) mr = GetComponentInChildren<MeshRenderer>();

            if (mr != null)
            {
                boxCol.center = transform.InverseTransformPoint(mr.bounds.center);
            }
            else if (Mathf.Abs(boxCol.center.x) > 1f || Mathf.Abs(boxCol.center.y) > 1f || Mathf.Abs(boxCol.center.z) > 1f)
            {
                boxCol.center = Vector3.zero;
            }
        }
    }

    void Start()
    {
        // Đảm bảo không bị các script khác tắt collider
        if (boxCollider == null) boxCollider = GetComponent<Collider>();
        if (boxCollider != null)
        {
            boxCollider.enabled = true;
        }

        if (interactPrompt == null)
        {
            interactPrompt = GetComponent<InteractPrompt>();
            if (interactPrompt == null) interactPrompt = gameObject.AddComponent<InteractPrompt>();
        }
        interactPrompt.enabled = true;
        interactPrompt.englishPrompt = englishPrompt;
        interactPrompt.vietnamesePrompt = vietnamesePrompt;
    }

    public void Interact()
    {
        if (hasOpened || isProcessingOpen) return;

        EnsureFadeImageExists();
        Debug.Log("[OpenablePlacedBox] 📦 Bấm [F] mở thùng hàng! Khởi động fade đen màn hình & đổi sang hộp mở...");

        if (coroutineRunner != null)
        {
            coroutineRunner.StartCoroutine(OpenSequenceRoutine());
        }
        else
        {
            StartCoroutine(OpenSequenceRoutine());
        }
    }

    IEnumerator OpenSequenceRoutine()
    {
        isProcessingOpen = true;
        if (boxCollider != null) boxCollider.enabled = false;

        // Khóa di chuyển & camera của Player
        MovePl playerMovePl = Object.FindFirstObjectByType<MovePl>();
        if (playerMovePl != null)
        {
            playerMovePl.isCameraLocked = true;
            playerMovePl.SetMovementState(false);
        }

        // 1. Chờ ngắn trước khi fade
        if (delayBeforeFade > 0f)
        {
            yield return new WaitForSeconds(delayBeforeFade);
        }

        EnsureFadeImageExists();

        // 2. Fade màn hình tối dần sang đen
        float elapsed = 0f;
        Color color = fadeScreenImage.color;
        color.a = 0f;
        fadeScreenImage.color = color;
        fadeScreenImage.gameObject.SetActive(true);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Clamp01(elapsed / fadeDuration);
            fadeScreenImage.color = color;
            yield return null;
        }
        color.a = 1f;
        fadeScreenImage.color = color;

        // 3. Trong bóng tối: Đổi sang Hộp Đã Mở (BoxOpen)
        Vector3 boxPos = transform.position;
        Quaternion boxRot = transform.rotation;
        Vector3 boxScale = transform.localScale;

        if (openBoxSound != null)
        {
            AudioSource.PlayClipAtPoint(openBoxSound, boxPos, soundVolume);
        }

        if (openBoxPrefab != null)
        {
            GameObject openedBox = null;
            if (openBoxPrefab.scene.rootCount > 0)
            {
                openedBox = openBoxPrefab;
                openedBox.transform.position = boxPos;
                openedBox.transform.rotation = boxRot;
                openedBox.transform.localScale = boxScale;
                openedBox.SetActive(true);
            }
            else
            {
                openedBox = Instantiate(openBoxPrefab, boxPos, boxRot);
                openedBox.transform.localScale = boxScale;
            }
        }

        // Ẩn hộp đóng ban đầu
        gameObject.SetActive(false);

        // 4. Giữ màn hình đen một khoảng thời gian
        if (holdBlackDuration > 0f)
        {
            yield return new WaitForSeconds(holdBlackDuration);
        }

        // 5. Fade màn hình sáng trở lại bình thường
        elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Clamp01(1f - (elapsed / fadeInDuration));
            fadeScreenImage.color = color;
            yield return null;
        }
        color.a = 0f;
        fadeScreenImage.color = color;
        fadeScreenImage.gameObject.SetActive(false);

        // 6. Trả lại quyền di chuyển cho Player
        if (playerMovePl != null)
        {
            playerMovePl.isCameraLocked = false;
            playerMovePl.SetMovementState(true);
            playerMovePl.SyncRotationWithCurrentCamera();
        }

        hasOpened = true;
        isProcessingOpen = false;
        Debug.Log("[OpenablePlacedBox] ✅ Đã mở thùng hàng hoàn tất!");
    }

    void EnsureFadeImageExists()
    {
        if (fadeScreenImage != null) return;

        if (coroutineRunner == null)
        {
            GameObject runnerObj = new GameObject("OpenBoxFadeRunner");
            coroutineRunner = runnerObj.AddComponent<FadeCoroutineRunner>();
            DontDestroyOnLoad(runnerObj);
        }

        Canvas existingCanvas = Object.FindFirstObjectByType<Canvas>();
        GameObject canvasObj = null;

        if (existingCanvas != null)
        {
            canvasObj = existingCanvas.gameObject;
        }
        else
        {
            canvasObj = new GameObject("OpenBoxFadeCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasObj);
        }

        GameObject imageObj = new GameObject("OpenBoxFadePanel");
        imageObj.transform.SetParent(canvasObj.transform, false);

        fadeScreenImage = imageObj.AddComponent<Image>();
        fadeScreenImage.color = new Color(0f, 0f, 0f, 0f);
        fadeScreenImage.raycastTarget = false;

        RectTransform rect = fadeScreenImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        imageObj.SetActive(false);
    }

    public void ShowPrompt()
    {
        if (hasOpened || isProcessingOpen) return;
        if (interactPrompt != null) interactPrompt.ShowPrompt();
    }

    public void HidePrompt()
    {
        if (interactPrompt != null) interactPrompt.HidePrompt();
    }
}
