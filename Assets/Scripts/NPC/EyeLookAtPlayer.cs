using UnityEngine;

public class EyeLookAtPlayer : MonoBehaviour
{
    [System.Serializable]
    public class EyeData
    {
        public string eyeName = "Mắt";
        public Transform eyeTransform;

        [Tooltip("Góc bù xoay riêng cho mắt này nếu bị lệch")]
        public Vector3 customOffset = Vector3.zero;

        [HideInInspector] public Vector3 origLocalPos;
        [HideInInspector] public Quaternion origLocalRot;
        [HideInInspector] public Vector3 eyeLocalCenter;
    }

    [Header("1. Xử Lý Từng Con Mắt Riêng Biệt")]
    public EyeData leftEye = new EyeData { eyeName = "Mắt Trái" };
    public EyeData rightEye = new EyeData { eyeName = "Mắt Phải" };

    [Header("2. Mục Tiêu Dõi Theo")]
    public Transform targetToLookAt;

    [Header("3. Cấu Hình Liếc Mắt")]
    public float lookSpeed = 5f;

    [Range(2f, 30f)]
    public float maxEyeAngle = 12f;

    void Start()
    {
        if (targetToLookAt == null)
        {
            Camera cam = Camera.main;
            if (cam != null) targetToLookAt = cam.transform;
        }
        InitEye(leftEye);
        InitEye(rightEye);
    }

    void InitEye(EyeData ed)
    {
        if (ed == null || ed.eyeTransform == null) return;
        ed.origLocalPos = ed.eyeTransform.localPosition;
        ed.origLocalRot = ed.eyeTransform.localRotation;

        Renderer rend = ed.eyeTransform.GetComponent<Renderer>();
        if (rend != null)
            ed.eyeLocalCenter = ed.eyeTransform.InverseTransformPoint(rend.bounds.center);
        else
            ed.eyeLocalCenter = Vector3.zero;
    }

    void LateUpdate()
    {
        if (targetToLookAt == null)
        {
            Camera cam = Camera.main;
            if (cam != null) targetToLookAt = cam.transform;
            if (targetToLookAt == null) return;
        }

        UpdateEye(leftEye);
        UpdateEye(rightEye);
    }

    void UpdateEye(EyeData ed)
    {
        if (ed == null || ed.eyeTransform == null) return;

        Transform eye = ed.eyeTransform;

        // 1. RESET VỊ TRÍ + GÓC VỀ GỐC TRƯỚC
        eye.localPosition = ed.origLocalPos;
        eye.localRotation = ed.origLocalRot;

        // 2. Lấy tâm nhãn cầu world
        Vector3 worldCenter = eye.TransformPoint(ed.eyeLocalCenter);

        // 3. Hướng tới người chơi
        Vector3 dirToPlayer = targetToLookAt.position - worldCenter;
        if (dirToPlayer.sqrMagnitude < 0.01f) return;

        // 4. Kiểm tra người chơi có ở phía trước không
        float angleToNPC = Vector3.Angle(transform.forward, dirToPlayer);
        if (angleToNPC > 90f) return;

        // 5. LOGIC XOAY GIỐNG HỆT CODE CŨ ĐANG HOẠT ĐỘNG ĐÚNG
        Quaternion lookRot = Quaternion.LookRotation(dirToPlayer) * Quaternion.Euler(ed.customOffset);
        Quaternion currentForward = Quaternion.LookRotation(transform.forward);
        Quaternion deltaRot = Quaternion.Inverse(currentForward) * lookRot;

        // 6. Giới hạn góc liếc nhẹ
        deltaRot = Quaternion.RotateTowards(Quaternion.identity, deltaRot, maxEyeAngle);

        // 7. Áp dụng xoay
        Quaternion finalLocalRot = ed.origLocalRot * deltaRot;
        eye.localRotation = Quaternion.Slerp(eye.localRotation, finalLocalRot, Time.deltaTime * lookSpeed * 3f);

        // 8. Bù vị trí để mắt xoay quanh tâm nhãn cầu
        Vector3 newWorldCenter = eye.TransformPoint(ed.eyeLocalCenter);
        eye.position += (worldCenter - newWorldCenter);
    }
}
