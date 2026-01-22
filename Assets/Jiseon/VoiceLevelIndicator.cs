using UnityEngine;
using UnityEngine.UI;

public class VoiceLevelIndicator : MonoBehaviour
{
    [Header("이미지 3개 (낮음 -> 중간 -> 높음 순서)")]
    public Image bar1;
    public Image bar2;
    public Image bar3;

    [Header("마이크 상태 아이콘")]
    public GameObject micOnIcon;  // 소리가 날 때 보일 아이콘 (예: 초록색 마이크)
    public GameObject micOffIcon; // 소리가 없을 때 보일 아이콘 (예: 회색 마이크)

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

        // 1. 음향 크기에 따른 3단계 바 표시
        SetActive(bar1, v >= th1);
        SetActive(bar2, v >= th2);
        SetActive(bar3, v >= th3);

        // 2. 마이크 온/오프 아이콘 제어 (th1 기준)
        bool isSpeaking = v >= th1;
        if (micOnIcon != null) micOnIcon.SetActive(isSpeaking);
        if (micOffIcon != null) micOffIcon.SetActive(!isSpeaking);
    }

    void SetActive(Graphic g, bool on)
    {
        if (!g) return;
        
        // 이미지를 완전히 끄면 레이아웃이 틀어질 수 있으므로 enabled만 제어하거나
        // 투명도(Alpha)를 함께 조절합니다.
        g.enabled = on; 
        var c = g.color; 
        c.a = on ? 1f : 0.2f; 
        g.color = c;
    }
}