using UnityEngine;

public class DayNightSystem : MonoBehaviour
{
    [Header("Cài đặt thời gian")]
    [Tooltip("Thời gian hoàn thành 1 ngày (tính bằng giây)")]
    [SerializeField] private float dayDuration = 120f;
    [SerializeField] private float dayRatio = 0.7f; // 70% ngày, 30% đêm

    [SerializeField] private Light sunLight; // Kéo Directional Light vào đây

    private float _currentTime = 0f;

    void Update()
    {
        // 1. Tính toán thời gian trôi qua
        _currentTime += Time.deltaTime / dayDuration;
        if (_currentTime >= 1f) _currentTime = 0f;

        // 2. Xoay mặt trời (Xoay quanh trục X từ 0 đến 360 độ)
        // Góc 0: Bình minh, 90: Giữa trưa, 180: Hoàng hôn, 270: Nửa đêm
        float sunRotation;

        if (_currentTime <= dayRatio)
        {
            // BAN NGÀY (0 → 180 độ)
            float t = _currentTime / dayRatio; // chuẩn hóa về 0 → 1
            sunRotation = Mathf.Lerp(0f, 180f, t);

            sunLight.intensity = Mathf.Clamp01(Mathf.Sin(t * Mathf.PI));
            sunLight.color = Color.white;
        }
        else
        {
            // BAN ĐÊM (180 → 360 độ)
            float t = (_currentTime - dayRatio) / (1f - dayRatio);
            sunRotation = Mathf.Lerp(180f, 360f, t);

            sunLight.intensity = 0.2f;
            sunLight.color = new Color(0.5f, 0.7f, 1f);
        }

        sunLight.transform.localRotation = Quaternion.Euler(sunRotation, -90f, 0f);
    }
}


