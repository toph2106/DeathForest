using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class OpenablePlacedBox : MonoBehaviour, IInteractable
{
    [Header("1. Chữ Nhắc Tương Tác (Prompt UI)")]
    public string englishPrompt = "Open Box";
    public string vietnamesePrompt = "Mở thùng hàng";

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

    [Header("6. Thoại Sau Khi Mở Thùng Hàng (Post Open Dialogues)")]
    [Tooltip("Danh sách câu thoại phát ngay khi vừa mở nắp thùng hàng nhìn thấy máy quay")]
    public SmartInteractionDialogue.DialogueLine[] postOpenDialogues = new SmartInteractionDialogue.DialogueLine[]
    {
        new SmartInteractionDialogue.DialogueLine
        {
            vietnameseDialogue = "Một chiếc máy quay cũ... kèm theo mấy cuộn băng cát-xét à?",
            englishDialogue = "An old camcorder... along with some cassette tapes?",
            holdDuration = 3.0f
        },
        new SmartInteractionDialogue.DialogueLine
        {
            vietnameseDialogue = "Cô ta gửi mớ đồ này cho mình với mục đích gì cơ chứ...",
            englishDialogue = "Why on earth did she send me all of this...",
            holdDuration = 3.0f
        }
    };

    [Header("6.1. Âm Thanh Thoại Gõ Chữ (Dialogue SFX)")]
    [Tooltip("Gói âm thanh lồng tiếng / gõ chữ khi hiện phụ đề (5s blip)")]
    public AudioClip dialogueSound;
    [Range(0f, 1f)] public float dialogueVolume = 0.8f;

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

        GameObject openedBox = null;
        if (openBoxPrefab != null)
        {
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

        // KÍCH HOẠT THOẠI SAU KHI MỞ THÙNG HÀNG (NHÌN THẤY MÁY QUAY)
        if (postOpenDialogues != null && postOpenDialogues.Length > 0 && coroutineRunner != null)
        {
            coroutineRunner.StartCoroutine(PlayBoxOpenDialoguesRoutine());
        }
    }

    IEnumerator PlayBoxOpenDialoguesRoutine()
    {
        if (postOpenDialogues == null || postOpenDialogues.Length == 0) yield break;

        TMPro.TextMeshProUGUI subtitleTextUI = null;
        BedSleepCutscene bed = Object.FindFirstObjectByType<BedSleepCutscene>(FindObjectsInactive.Include);
        if (bed != null && bed.subtitleTextUI != null) subtitleTextUI = bed.subtitleTextUI;
        if (subtitleTextUI == null)
        {
            GameIntroManager intro = Object.FindFirstObjectByType<GameIntroManager>(FindObjectsInactive.Include);
            if (intro != null && intro.subtitleTextUI != null) subtitleTextUI = intro.subtitleTextUI;
        }
        if (subtitleTextUI == null)
        {
            GameObject subObj = GameObject.Find("SubtitleText");
            if (subObj == null) subObj = GameObject.Find("Subtitle");
            if (subObj == null) subObj = GameObject.Find("DialogueText");
            if (subObj != null) subtitleTextUI = subObj.GetComponent<TMPro.TextMeshProUGUI>();
        }

        if (subtitleTextUI == null) yield break;

        SmartInteractionDialogue.isAnyDialoguePlaying = true;

        AudioSource aSource = null;
        if (coroutineRunner != null)
        {
            aSource = coroutineRunner.GetComponent<AudioSource>();
            if (aSource == null) aSource = coroutineRunner.gameObject.AddComponent<AudioSource>();
            aSource.spatialBlend = 0f;
            aSource.playOnAwake = false;
        }

        if (subtitleTextUI.transform.parent != null) subtitleTextUI.transform.parent.gameObject.SetActive(true);
        subtitleTextUI.gameObject.SetActive(true);

        // Chờ 1 frame để click tương tác ban đầu trôi qua
        yield return null;

        for (int i = 0; i < postOpenDialogues.Length; i++)
        {
            var line = postOpenDialogues[i];
            if (line == null) continue;

            string fullText = (SettingsManager.currentLanguage == "VI") ? line.vietnameseDialogue : line.englishDialogue;
            if (string.IsNullOrEmpty(fullText)) fullText = line.vietnameseDialogue;
            if (string.IsNullOrEmpty(fullText)) fullText = line.englishDialogue;
            if (string.IsNullOrEmpty(fullText)) continue;

            Color sc = subtitleTextUI.color;
            sc.a = 1f;
            subtitleTextUI.color = sc;

            if (dialogueSound != null && aSource != null)
            {
                aSource.clip = dialogueSound;
                aSource.volume = dialogueVolume;
                aSource.loop = true;
                aSource.time = 0f;
                aSource.Play();
            }

            float lineStartTime = Time.time;
            bool skip = false;
            subText:
            subtitleTextUI.text = "";
            for (int c = 1; c <= fullText.Length; c++)
            {
                if (Time.time - lineStartTime > 0.2f && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
                {
                    skip = true;
                    subtitleTextUI.text = fullText;
                    break;
                }
                subtitleTextUI.text = fullText.Substring(0, c) + "_";
                yield return new WaitForSeconds(0.03f);
            }

            if (aSource != null && aSource.isPlaying) aSource.Stop();
            subtitleTextUI.text = fullText;

            float timer = 0f;
            float hold = (line.holdDuration > 0f) ? line.holdDuration : 2.5f;
            bool blink = true;
            float blinkTimer = 0f;
            while (timer < hold)
            {
                if (timer > 0.2f && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))) break;
                timer += Time.deltaTime;
                blinkTimer += Time.deltaTime;
                if (blinkTimer >= 0.4f)
                {
                    blinkTimer = 0f;
                    blink = !blink;
                    subtitleTextUI.text = fullText + (blink ? " _" : "  ");
                }
                yield return null;
            }

            float fadeElapsed = 0f;
            while (fadeElapsed < 0.2f)
            {
                fadeElapsed += Time.deltaTime;
                sc.a = Mathf.Lerp(1f, 0f, fadeElapsed / 0.2f);
                subtitleTextUI.color = sc;
                yield return null;
            }
        }

        subtitleTextUI.text = "";
        Color finalColor = subtitleTextUI.color;
        finalColor.a = 1f;
        subtitleTextUI.color = finalColor;

        SmartInteractionDialogue.isAnyDialoguePlaying = false;
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
