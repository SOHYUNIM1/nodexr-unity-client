using UnityEngine;
using UnityEngine.UI;

public class VoiceLevelIndicator : MonoBehaviour
{
    [Header("이미지 3개 (낮음 -> 중간 -> 높음 순서)")]
    public Image bar1;
    public Image bar2;
    public Image bar3;

    [Header("음성 소스")]
    public VoiceLevelSource source;

    [Header("임계값 (0~1)")]
    public float th1 = 0.10f; // 1단계 활성화 기준
    public float th2 = 0.40f; // 2단계 활성화 기준
    public float th3 = 0.75f; // 3단계 활성화 기준

    void Reset() { source = GetComponentInParent<VoiceLevelSource>(); }

    void Update()
    {
        if (!source) return;
        float v = source.level01;

        // 음향 크기에 따라 3단계 표시
        SetActive(bar1, v >= th1);
        SetActive(bar2, v >= th2);
        SetActive(bar3, v >= th3);
    }

    void SetActive(Graphic g, bool on)
    {
        if (!g) return;
        
        // g.enabled를 끄면 이미지가 완전히 사라지고, 
        // 켜져 있을 때는 알파값(투명도)으로 강조/비강조를 표현합니다.
        g.enabled = on; 
        var c = g.color; 
        c.a = on ? 1f : 0.2f; 
        g.color = c;
    }
}