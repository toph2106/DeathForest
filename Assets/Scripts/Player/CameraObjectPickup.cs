using UnityEngine;
using System.Collections;

public class CameraObjectPickup : MonoBehaviour, IInteractable
{
    [Header("1. UI Màn Hình Máy Quay (Camera UI Canvas)")]
    [Tooltip("Kéo Canvas chứa giao diện kính ngắm & đếm giờ vào đây")]
    public GameObject cameraUICanvas;

    [Header("2. Âm Thanh Trang Bị Máy Quay (Click -> Beep -> Hiện UI)")]
    [Tooltip("Kéo âm thanh lật mở màn hình máy quay (Cạch cạch) vào đây")]
    public AudioClip camcorderClickSound;

    [Tooltip("Kéo âm thanh bíp khởi động máy quay (Beep) vào đây")]
    public AudioClip camcorderBeepSound;

    [Range(0f, 1f)]
    [Tooltip("Thanh trượt điều chỉnh âm lượng tiếng máy quay (Mặc định: 0.8)")]
    public float soundVolume = 0.8f;

    [Header("3. Khóa nhặt máy quay (Yêu cầu xem xong máy tính)")]
    public bool requireComputerCutscene = true;
    public static bool isComputerCutsceneFinished = false;

    [Header("4. Thoại Khi Nhặt Máy Quay (Pickup Dialogues)")]
    [Tooltip("Danh sách câu thoại phát ngay sau khi nhặt và khởi động máy quay")]
    public SmartInteractionDialogue.DialogueLine[] pickupDialogues = new SmartInteractionDialogue.DialogueLine[]
    {
        new SmartInteractionDialogue.DialogueLine
        {
            vietnameseDialogue = "Máy vẫn còn lên nguồn... Pin vẫn còn đầy.",
            englishDialogue = "It still powers on... Full battery.",
            holdDuration = 2.5f
        },
        new SmartInteractionDialogue.DialogueLine
        {
            vietnameseDialogue = "Để xem trong này có ghi lại cái gì...",
            englishDialogue = "Let's see if there's anything recorded in here...",
            holdDuration = 2.5f
        }
    };

    [Header("4.1. Âm Thanh Thoại Gõ Chữ (Dialogue SFX)")]
    [Tooltip("Gói âm thanh lồng tiếng / gõ chữ khi hiện phụ đề (5s blip)")]
    public AudioClip dialogueSound;
    [Range(0f, 1f)] public float dialogueVolume = 0.8f;

    private SimpleCameraOverlay overlayManager;
    private AudioSource audioSource;
    private bool isEquipping = false;

    void Start()
    {
        if (cameraUICanvas != null) cameraUICanvas.SetActive(false);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f; // Âm thanh 2D rõ nét cho người đeo tai nghe
        audioSource.playOnAwake = false;

        if (Camera.main != null)
        {
            overlayManager = Camera.main.GetComponent<SimpleCameraOverlay>();
        }
    }

    public void Interact()
    {
        Pickup();
    }

    public void Pickup()
    {
        if (isEquipping) return;

        // Nếu bật khóa và chưa dùng xong máy tính -> KHÔNG CHO NHẶT
        if (requireComputerCutscene && !isComputerCutsceneFinished)
        {
            Debug.Log("[CameraObjectPickup] ⚠️ Bạn phải sử dụng xong máy tính mới được nhặt máy quay!");
            return;
        }

        StartCoroutine(EquipSequenceRoutine());
    }

    /// <summary>
    /// Trả lại mô hình máy quay về trong chiếc hộp mở và tắt toàn bộ UI kính ngắm máy quay
    /// </summary>
    public void ResetCamcorderToBox()
    {
        isEquipping = false;

        // 1. Tắt UI Camcorder Canvas & Overlay
        if (cameraUICanvas != null) cameraUICanvas.SetActive(false);
        CamcorderUI.ResetPickedUpCameraState();

        if (overlayManager != null) overlayManager.ResetCameraView();

        if (FlashlightToggle.Instance != null)
        {
            FlashlightToggle.Instance.UnequipFlashlight();
        }

        // 2. Hiện lại toàn bộ Renderer và Collider 3D của máy quay trong hộp
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers) r.enabled = true;

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider c in colliders) c.enabled = true;

        gameObject.SetActive(true);
        Debug.Log("[CameraObjectPickup] 📦 Đã trả lại mô hình Camcorder về trong hộp và tắt toàn bộ UI Camcorder!");
    }

    IEnumerator EquipSequenceRoutine()
    {
        isEquipping = true;
        SmartInteractionDialogue.isAnyDialoguePlaying = true;

        // 1. Ẩn hiển thị hình ảnh 3D máy quay dưới đất ngay lập tức
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.enabled = false;

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders) c.enabled = false;

        TMPro.TextMeshProUGUI subtitleTextUI = FindSubtitleUI();
        AudioSource dialogueAudio = GetDialogueAudioSource();

        if (subtitleTextUI != null)
        {
            if (subtitleTextUI.transform.parent != null) subtitleTextUI.transform.parent.gameObject.SetActive(true);
            subtitleTextUI.gameObject.SetActive(true);
        }

        // Chờ 1 frame để lượt click chuột tương tác ban đầu trôi qua
        yield return null;

        // 2. BƯỚC 1: PHÁT NGAY CÂU THOẠI 1 (VD: "Thử bật lên xem nào") KHI VỪA BẤM NHẶT
        if (pickupDialogues != null && pickupDialogues.Length > 0 && pickupDialogues[0] != null)
        {
            yield return StartCoroutine(PlaySingleLine(pickupDialogues[0], subtitleTextUI, dialogueAudio));
        }

        // 3. BƯỚC 2: PHÁT TIẾNG "CẠCH CẠCH" MỞ NẮP MÀN HÌNH MÁY QUAY
        if (camcorderClickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(camcorderClickSound, soundVolume);
            yield return new WaitForSeconds(camcorderClickSound.length);
        }
        else
        {
            yield return new WaitForSeconds(0.3f);
        }

        // 4. BƯỚC 3: PHÁT TIẾNG "BÍP" KHỞI ĐỘNG MÁY QUAY
        if (camcorderBeepSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(camcorderBeepSound, soundVolume);
            yield return new WaitForSeconds(0.15f);
        }

        // 5. BƯỚC 4: KÍCH HOẠT GIAO DIỆN KÍNH NGẮM VÀ UI CAMCORDER + MẶC ĐỊNH 100% PIN
        CamcorderUI.MarkCameraPickedUp();

        if (FlashlightToggle.Instance != null)
        {
            FlashlightToggle.Instance.EquipFlashlight();
        }

        if (CamcorderUI.Instance != null)
        {
            CamcorderUI.Instance.gameObject.SetActive(true);
        }

        if (overlayManager != null)
        {
            overlayManager.TurnOnCameraView();
        }

        if (cameraUICanvas != null)
        {
            cameraUICanvas.SetActive(true);
        }

        // Kích hoạt bộ đếm sự kiện 10s mở cửa chính
        Camera10sDoorEvent doorEvent = Object.FindFirstObjectByType<Camera10sDoorEvent>();
        if (doorEvent == null)
        {
            GameObject eventObj = new GameObject("Camera10sDoorEvent");
            eventObj.AddComponent<Camera10sDoorEvent>();
        }

        // 6. BƯỚC 5: NGAY KHI UI CAMCORDER HIỆN LÊN -> PHÁT CÂU THOẠI 2 (VD: "Uầy dùng vẫn ok phết")
        if (pickupDialogues != null && pickupDialogues.Length > 1)
        {
            for (int i = 1; i < pickupDialogues.Length; i++)
            {
                if (pickupDialogues[i] != null)
                {
                    yield return StartCoroutine(PlaySingleLine(pickupDialogues[i], subtitleTextUI, dialogueAudio));
                }
            }
        }

        if (subtitleTextUI != null)
        {
            subtitleTextUI.text = "";
            Color sc = subtitleTextUI.color;
            sc.a = 1f;
            subtitleTextUI.color = sc;
            subtitleTextUI.gameObject.SetActive(false);
        }

        isEquipping = false;
        SmartInteractionDialogue.isAnyDialoguePlaying = false;
        Debug.Log("[CameraObjectPickup] ✅ Đã trang bị máy quay Camcorder thành công!");
    }

    IEnumerator PlaySingleLine(SmartInteractionDialogue.DialogueLine line, TMPro.TextMeshProUGUI subUI, AudioSource aSource)
    {
        if (subUI == null || line == null) yield break;

        string fullText = (SettingsManager.currentLanguage == "VI") ? line.vietnameseDialogue : line.englishDialogue;
        if (string.IsNullOrEmpty(fullText)) fullText = line.vietnameseDialogue;
        if (string.IsNullOrEmpty(fullText)) yield break;

        Color sc = subUI.color;
        sc.a = 1f;
        subUI.color = sc;

        if (dialogueSound != null && aSource != null)
        {
            aSource.clip = dialogueSound;
            aSource.volume = dialogueVolume;
            aSource.loop = true;
            aSource.time = 0f;
            aSource.Play();
        }

        subUI.text = "";
        for (int c = 1; c <= fullText.Length; c++)
        {
            if (c > 1 && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
            {
                subUI.text = fullText;
                break;
            }
            subUI.text = fullText.Substring(0, c) + "_";
            yield return new WaitForSeconds(0.03f);
        }

        if (aSource != null && aSource.isPlaying) aSource.Stop();
        subUI.text = fullText;

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
                subUI.text = fullText + (blink ? " _" : "  ");
            }
            yield return null;
        }

        float fadeElapsed = 0f;
        while (fadeElapsed < 0.2f)
        {
            fadeElapsed += Time.deltaTime;
            sc.a = Mathf.Lerp(1f, 0f, fadeElapsed / 0.2f);
            subUI.color = sc;
            yield return null;
        }
    }

    TMPro.TextMeshProUGUI FindSubtitleUI()
    {
        BedSleepCutscene bed = Object.FindFirstObjectByType<BedSleepCutscene>(FindObjectsInactive.Include);
        if (bed != null && bed.subtitleTextUI != null) return bed.subtitleTextUI;

        GameIntroManager intro = Object.FindFirstObjectByType<GameIntroManager>(FindObjectsInactive.Include);
        if (intro != null && intro.subtitleTextUI != null) return intro.subtitleTextUI;

        GameObject subObj = GameObject.Find("SubtitleText") ?? GameObject.Find("Subtitle") ?? GameObject.Find("DialogueText");
        if (subObj != null) return subObj.GetComponent<TMPro.TextMeshProUGUI>();

        return null;
    }

    AudioSource GetDialogueAudioSource()
    {
        MovePl player = Object.FindFirstObjectByType<MovePl>();
        if (player != null)
        {
            AudioSource aSource = player.GetComponent<AudioSource>();
            if (aSource == null) aSource = player.gameObject.AddComponent<AudioSource>();
            aSource.spatialBlend = 0f;
            aSource.playOnAwake = false;
            return aSource;
        }
        return audioSource;
    }
}