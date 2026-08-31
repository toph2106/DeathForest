using UnityEngine;
using System.Collections;
using UnityEngine.Events;

/// <summary>
/// Script dùng cho các vật thể chắn đường (như Vỏ dao Sheath đè lên tờ giấy):
/// 1. Kế thừa IInteractable -> Tự động nhận diện chuột trái và hiện icon Hand khi nhìn vào.
/// 2. Khi click: Trượt mượt mà sang một bên theo khoảng cách (Move Offset) hoặc đến Transform chỉ định.
/// 3. Phát âm thanh cọ xát vật thể (moveSound).
/// 4. Sau khi trượt xong: Tự động kích hoạt/bật Collider cho tờ giấy Note bên dưới để người chơi đọc được.
/// </summary>
public class SlideObjectInteractable : MonoBehaviour, IInteractable
{
    [Header("1. Cài Đặt Dịch Chuyển (Slide Settings)")]
    [Tooltip("Khoảng cách trượt tương đối (Ví dụ: X: 0.15 hoặc Z: 0.15 mét sang bên cạnh)")]
    public Vector3 localMoveOffset = new Vector3(0.18f, 0f, 0f);

    [Tooltip("Điểm đích cụ thể (Nếu kéo một Empty GameObject vào đây thì vật thể sẽ trượt chính xác đến điểm đó, bỏ qua localMoveOffset)")]
    public Transform targetDestination;

    [Tooltip("Thời gian trượt (giây - Mặc định: 0.5s)")]
    public float slideDuration = 0.5f;

    [Tooltip("Đường cong gia tốc chuyển động mượt mà")]
    public AnimationCurve slideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("2. Âm Thanh Dịch Chuyển (Audio)")]
    public AudioSource audioSource;
    [Tooltip("Âm thanh cọ xát đồ vật / trượt gỗ trên chiếu")]
    public AudioClip slideSound;
    [Range(0f, 1f)] public float soundVolume = 0.9f;

    [Header("3. Vật Thể Được Giải Phóng (Unlock Target)")]
    [Tooltip("Kéo GameObject tờ giấy (Note) vào đây. Sau khi gạt vỏ dao xong, Collider của tờ giấy sẽ tự động BẬT lên để cho phép tương tác")]
    public GameObject noteToUnlock;

    [Header("4. Sự Kiện Mở Rộng (Events)")]
    public UnityEvent onSlideStart;
    public UnityEvent onSlideFinished;

    private bool hasMoved = false;
    private bool isMoving = false;
    private Collider selfCollider;

    void Awake()
    {
        selfCollider = GetComponent<Collider>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D Sound
            audioSource.playOnAwake = false;
        }

        SetNoteCollidersActive(false);
    }

    void Start()
    {
        // Khóa thêm lần nữa ở Start để đảm bảo tuyệt đối không bị script khác bật lên
        if (!hasMoved)
        {
            SetNoteCollidersActive(false);
        }
    }

    private void SetNoteCollidersActive(bool active)
    {
        if (noteToUnlock == null) return;
        Collider[] noteCols = noteToUnlock.GetComponentsInChildren<Collider>(true);
        foreach (var col in noteCols)
        {
            if (col != null) col.enabled = active;
        }
    }

    /// <summary>
    /// Được gọi tự động khi người chơi click Chuột Trái vào Vỏ Dao
    /// </summary>
    public void Interact()
    {
        if (hasMoved || isMoving) return;

        StartCoroutine(SlideRoutine());
    }

    private IEnumerator SlideRoutine()
    {
        isMoving = true;
        onSlideStart?.Invoke();

        // 1. PHÁT ÂM THANH TRƯỢT
        if (slideSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(slideSound, soundVolume);
        }

        // 2. TÍNH TOÁN VỊ TRÍ ĐÍCH
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 endPos;
        if (targetDestination != null)
        {
            endPos = targetDestination.position;
        }
        else if (transform.parent != null)
        {
            endPos = transform.parent.TransformPoint(transform.localPosition + localMoveOffset);
        }
        else
        {
            endPos = transform.position + localMoveOffset;
        }

        Quaternion endRot = (targetDestination != null) ? targetDestination.rotation : transform.rotation;

        // 3. TRƯỢT MƯỢT MÀ BẰNG COROUTINE
        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            float curveT = slideCurve.Evaluate(t);

            transform.position = Vector3.Lerp(startPos, endPos, curveT);
            transform.rotation = Quaternion.Slerp(startRot, endRot, curveT);

            yield return null;
        }

        transform.position = endPos;
        transform.rotation = endRot;

        hasMoved = true;
        isMoving = false;

        // 4. TẮT TƯƠNG TÁC VỎ DAO ĐỂ KHÔNG BỊ BẤM LẠI
        if (selfCollider != null)
        {
            selfCollider.enabled = false;
        }

        // 5. GIẢI PHÓNG & BẬT COLLIDER TỜ GIẤY ĐỂ BẮT ĐẦU TƯƠNG TÁC ĐỌC
        SetNoteCollidersActive(true);
        Debug.Log($"[SlideObjectInteractable] 📄 Đã mở khóa tương tác cho {noteToUnlock?.name}!");

        onSlideFinished?.Invoke();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Vector3 endPos;
        if (targetDestination != null) endPos = targetDestination.position;
        else if (transform.parent != null) endPos = transform.parent.TransformPoint(transform.localPosition + localMoveOffset);
        else endPos = transform.position + localMoveOffset;

        Gizmos.DrawLine(transform.position, endPos);
        Gizmos.DrawWireSphere(endPos, 0.05f);
    }
}
