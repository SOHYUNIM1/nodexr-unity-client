using System;
using UnityEngine;

// 최종 전사 결과를 받으면 이벤트
public class VoiceController : MonoBehaviour
{
    public event Action<string, float> OnFinalTranscript; // (text, confidence)

    // Meta Voice/Wit 콜백에서 호출
    public void Debug_FireFinalTranscript(string text, float confidence = 1.0f)
    {
        OnFinalTranscript?.Invoke(text, confidence);
    }

    // PTT 시작/종료 훅(Voice SDK 연결 지점)
    public void StartListening()
    {
        Debug.Log("Voice StartListening()");
        // TODO: Meta Voice SDK start
    }

    public void StopListening()
    {
        Debug.Log("Voice StopListening()");
        // TODO: Meta Voice SDK stop -> Final transcript eventually
    }
}
