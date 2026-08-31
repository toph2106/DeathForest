using UnityEngine;
using System.Collections;
using TMPro;

/// <summary>
/// Quản lý vật phẩm có thể nhặt được trên sàn đất (Items, Battery, Keys, Gore, Quest items):
/// 1. Tự động đưa vật phẩm vào Hotbar Inventory.
/// 2. Khi túi đồ ĐẦY (5/5 ô): Tự động phát âm thanh và hiện câu thoại phụ đề nhắc nhở người chơi!
/// </summary>
public class InteractableItem : MonoBehaviour, IInteractable
{
    public enum ItemType { Consumable, Quest, Battery, Paper, Key, Backpack }

    [Header("Item Configuration")]
    public ItemType itemType; // Chọn loại Item trên Inspector
    public string itemNameOrQuestName = "Pin"; // Tên item hoặc Tên Quest / Chìa khóa

    [Header("UI Icon & 3D Model")]
    [Tooltip("Kéo Sprite hình icon của vật phẩm (nếu dùng 2D)")]
    public Sprite itemIcon;
    [Tooltip("Kéo Prefab 3D của vật phẩm (Nếu để trống sẽ tự lấy chính GameObject này)")]
    public GameObject item3DPrefab;

    [Header("Battery Settings (Nếu chọn ItemType = Battery)")]
    [Tooltip("Nếu tích chọn: Pin sẽ vào Kho đồ / Túi đồ Hotbar để dành bấm phím R nạp khi cần. Bỏ tích: Nạp thẳng vào đèn pin ngay lập tức.")]
    public bool addToInventory = true;

    [Tooltip("Lượng % Pin được nạp khi dùng trực tiếp (Mặc định: +50%)")]
    public float batteryRechargeAmount = 50f;

    [Header("Inventory Full Monologue (Thoại khi túi đồ đầy)")]
    [Tooltip("Bật hiện phụ đề thoại khi túi đồ đã đầy")]
    public bool showDialogueWhenFull = true;
    [TextArea(2, 3)]
    public string fullInventoryDialogue = "Túi đồ của mình đã đầy rồi... Không thể mang thêm được nữa.";
    public float fullDialogueDuration = 3.2f;

    [Header("Backpack Pickup Monologue (Thoại khi nhặt Balo)")]
    [Tooltip("Bật hiện phụ đề thoại khi nhặt được Balo mở rộng")]
    public bool showBackpackDialogue = true;
    [TextArea(2, 3)]
    public string backpackPickupDialogue = "Một chiếc balo cũ... có vẻ vẫn còn dùng tốt, mình có thể mang theo nhiều đồ hơn rồi.";
    public float backpackDialogueDuration = 3.5f;

    [Header("Cấu Hình Phụ Đề & Âm Thanh")]
    public TextMeshProUGUI subtitleTextUI;
    public AudioClip dialogueSound;

    private InventoryManager inventoryManager;
    private bool hasBeenPickedUp = false;
    private static Coroutine activeFullDialogueRoutine;
    private static MonoBehaviour dialogueRunner;

    void Start()
    {
        inventoryManager = Object.FindFirstObjectByType<InventoryManager>();
        FindSubtitleUI();
        FindDialogueSound();
    }

    void FindSubtitleUI()
    {
        if (subtitleTextUI != null) return;

        // Tìm từ các script thoại trong scene
        CorpseGoreHarvest corpse = Object.FindFirstObjectByType<CorpseGoreHarvest>();
        if (corpse != null && corpse.subtitleTextUI != null)
        {
            subtitleTextUI = corpse.subtitleTextUI;
            return;
        }

        SmartInteractionDialogue smart = Object.FindFirstObjectByType<SmartInteractionDialogue>();
        if (smart != null && smart.subtitleTextUI != null)
        {
            subtitleTextUI = smart.subtitleTextUI;
            return;
        }

        GameObject subObj = GameObject.Find("Subtitle Text") ?? GameObject.Find("SubtitleText") ?? GameObject.Find("DocumentSubtitleText");
        if (subObj != null) subtitleTextUI = subObj.GetComponent<TextMeshProUGUI>();
    }

    void FindDialogueSound()
    {
        if (dialogueSound != null) return;

        CorpseGoreHarvest corpse = Object.FindFirstObjectByType<CorpseGoreHarvest>();
        if (corpse != null && corpse.dialogueSound != null)
        {
            dialogueSound = corpse.dialogueSound;
            return;
        }

        SmartInteractionDialogue smart = Object.FindFirstObjectByType<SmartInteractionDialogue>();
        if (smart != null && smart.dialogueSound != null)
        {
            dialogueSound = smart.dialogueSound;
        }
    }

    public void ShowPrompt()
    {
    }

    public void HidePrompt()
    {
    }

    public void Interact()
    {
        Pickup();
    }

    public void ResetPickupState()
    {
        hasBeenPickedUp = false;
    }

    public void Pickup()
    {
        if (hasBeenPickedUp) return;

        if (inventoryManager == null)
        {
            inventoryManager = Object.FindFirstObjectByType<InventoryManager>();
            if (inventoryManager == null) return;
        }

        GameObject sourceObj = (item3DPrefab != null) ? item3DPrefab : gameObject;

        if (itemType == ItemType.Consumable || itemType == ItemType.Key || itemType == ItemType.Battery)
        {
            hasBeenPickedUp = true;
            bool isPickedUp = inventoryManager.AddConsumableItem(itemNameOrQuestName, sourceObj, itemIcon, itemType);

            if (isPickedUp)
            {
                if (item3DPrefab != null)
                {
                    gameObject.SetActive(false);
                    Destroy(gameObject);
                }
                else
                {
                    // Giữ lại mô hình 3D thật trong Scene để khi ném (Drop) ra sàn không bị biến thành khối Cube
                    transform.SetParent(inventoryManager.transform, false);
                    gameObject.SetActive(false);
                }
            }
            else
            {
                hasBeenPickedUp = false; // Trả lại nếu túi đồ đầy
                Debug.Log($"[InteractableItem] ⚠️ Túi đồ đầy (5/5 ô)! Không thể nhặt thêm '{itemNameOrQuestName}'.");

                if (showDialogueWhenFull)
                {
                    TriggerDialogue(fullInventoryDialogue, fullDialogueDuration);
                }
            }
        }
        else if (itemType == ItemType.Quest)
        {
            hasBeenPickedUp = true;
            inventoryManager.AddQuestItem(itemNameOrQuestName);
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
        else if (itemType == ItemType.Backpack)
        {
            hasBeenPickedUp = true;
            if (inventoryManager != null)
            {
                inventoryManager.UnlockBackpack();
            }

            // Hiện câu thoại khi nhặt được Balo
            if (showBackpackDialogue && !string.IsNullOrEmpty(backpackPickupDialogue))
            {
                TriggerDialogue(backpackPickupDialogue, backpackDialogueDuration);
            }

            Debug.Log($"[InteractableItem] 🎒 Đã nhặt Balo '{itemNameOrQuestName}'! Mở khóa mở rộng túi đồ.");
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
        else if (itemType == ItemType.Paper)
        {
            hasBeenPickedUp = true;
            Debug.Log("Đã đọc tài liệu: " + itemNameOrQuestName);
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }

    private void TriggerDialogue(string dialogueContent, float duration)
    {
        FindSubtitleUI();
        FindDialogueSound();

        if (subtitleTextUI == null || string.IsNullOrEmpty(dialogueContent)) return;

        // Dùng FadeCoroutineRunner hoặc InventoryManager làm runner để không bị hủy khi GameObject bị Destroy
        MonoBehaviour runner = (MonoBehaviour)inventoryManager ?? this;

        if (activeFullDialogueRoutine != null && dialogueRunner != null)
        {
            dialogueRunner.StopCoroutine(activeFullDialogueRoutine);
        }

        dialogueRunner = runner;
        activeFullDialogueRoutine = runner.StartCoroutine(PlayFullDialogueRoutine(subtitleTextUI, dialogueContent, duration, dialogueSound));
    }

    private static IEnumerator PlayFullDialogueRoutine(TextMeshProUGUI textUI, string content, float duration, AudioClip sound)
    {
        if (textUI == null) yield break;

        if (textUI.transform.parent != null) textUI.transform.parent.gameObject.SetActive(true);
        textUI.gameObject.SetActive(true);
        Color sc = textUI.color;
        sc.a = 1f;
        textUI.color = sc;
        textUI.text = "";

        if (sound == null)
        {
            sound = Resources.Load<AudioClip>("Sound/8-bit-wavering-text-scroll");
        }

        AudioSource aSrc = textUI.GetComponent<AudioSource>();
        if (aSrc == null) aSrc = textUI.gameObject.AddComponent<AudioSource>();
        aSrc.spatialBlend = 0f;

        if (sound != null)
        {
            aSrc.clip = sound;
            aSrc.volume = 0.8f;
            aSrc.loop = true;
            aSrc.time = 0f;
            aSrc.Play();
        }

        // Hiệu ứng Typewriter gõ từng chữ với con trỏ '_'
        for (int i = 1; i <= content.Length; i++)
        {
            string typed = content.Substring(0, i) + "_";
            textUI.text = typed;
            yield return new WaitForSeconds(0.03f);
        }

        textUI.text = content;

        if (aSrc != null && aSrc.isPlaying && aSrc.clip == sound)
        {
            aSrc.Stop();
        }

        float timer = 0f;
        while (timer < duration)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E))
            {
                break;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        textUI.text = "";
        textUI.gameObject.SetActive(false);
        if (textUI.transform.parent != null) textUI.transform.parent.gameObject.SetActive(false);
        activeFullDialogueRoutine = null;
    }
}