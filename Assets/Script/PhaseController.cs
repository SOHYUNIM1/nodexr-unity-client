using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// 앱 상태(Phase 01 MVP) 정의
public enum AppPhase
{
    Boot,
    Idle,
    Listening,
    SendingUtterance,
    WaitingGraph,
    GraphReady,
    ConfirmingGraph,
    TransitionNext,
    Error
}

/// Phase 01(말하기 → 그래프 → Confirm) 흐름을 관리하는 컨트롤러
/// - 토글 방식 PTT 지원 (Start/Stop 버튼 1개)
/// - Final transcript를 받으면 /event(utterance_end) 전송
/// - /result/graph를 폴링해서 ready=true면 GraphRenderer로 렌더
/// - Confirm 누르면 /event(graph_confirm) 전송 후 다음 단계로 전환
///
/// Quest 없이 에디터 테스트용:
/// - UI_DebugSendTranscriptFromInput(): 입력창 텍스트를 Final transcript처럼 처리
/// - UI_SpawnDummyGraph(): 서버 없이 그래프 렌더링만 검증
public class PhaseController : MonoBehaviour
{
    [Header("State")]
    public AppPhase phase = AppPhase.Boot;

    [Header("Refs")]
    public NetworkClient network;          // FastAPI 통신 담당
    public VoiceController voice;          // 음성(STT) 담당 (Quest 없으면 미연결 가능)
    public GraphRenderer graphRenderer;    // 그래프 렌더 담당

    [Header("UI (Optional)")]
    public TMP_Text statusText;            // 상태 표시 텍스트
    public TMP_Text pttButtonText;         // 토글 버튼 글자(Start/Stop)
    public Button confirmButton;           // Confirm 버튼(없어도 됨)
    public TMP_InputField debugInput;      // 에디터 테스트용 입력창(없어도 됨)

    [Header("Session")]
    public string sessionId = "s_local";
    public int lastGraphVersion = 0;

    [Header("Polling")]
    public float pollInterval = 0.5f;      // 그래프 폴링 주기(초)
    public float pollTimeoutSec = 20f;     // 폴링 최대 대기(초)

    [Header("Safety")]
    public float waitFinalTranscriptTimeout = 15f; // Stop 누른 뒤 Final transcript 안 오면 타임아웃(초)

    private bool _isListening = false;     // 토글 상태(현재 듣는 중인지)
    private Coroutine _pollCoroutine;
    private Coroutine _finalWaitCoroutine;

    private void Awake()
    {
        // sessionId가 비어있으면 임의 생성
        if (string.IsNullOrEmpty(sessionId))
            sessionId = $"s_{Guid.NewGuid():N}".Substring(0, 10);

        // VoiceController에서 최종 전사 결과를 받는 이벤트 구독
        if (voice != null)
            voice.OnFinalTranscript += HandleFinalTranscript;

        SetPhase(AppPhase.Idle);
        UpdatePTTLabel();
    }

    private void OnDestroy()
    {
        if (voice != null)
            voice.OnFinalTranscript -= HandleFinalTranscript;
    }

    // =========================
    // UI에서 연결할 함수들
    // =========================

    /// PTT 토글 버튼(한 번 누르면 Start, 다시 누르면 Stop)
    /// Unity UI Button.onClick에 이 함수 연결
    public void UI_TogglePTT()
    {
        // 전송/대기/그래프 준비 이후에는 다시 듣기 방지(원하면 정책 변경 가능)
        if (phase != AppPhase.Idle && phase != AppPhase.Listening)
            return;

        // VoiceController가 아예 없으면(Quest 없이) 토글만 하고 Debug 입력으로 테스트하도록 유도
        if (voice == null)
        {
            Debug.LogWarning("VoiceController is not assigned. Use Debug input (UI_DebugSendTranscriptFromInput) to test in Editor.");
            // 그래도 UI 토글감만 보이게 상태는 바꿔줌
        }

        if (!_isListening)
        {
            // ----- Start Listening -----
            _isListening = true;
            SetPhase(AppPhase.Listening);

            // 이전 Final 대기 타임아웃 코루틴이 돌고 있으면 정리
            if (_finalWaitCoroutine != null)
            {
                StopCoroutine(_finalWaitCoroutine);
                _finalWaitCoroutine = null;
            }

            if (voice != null)
                voice.StartListening();
        }
        else
        {
            // ----- Stop Listening -----
            _isListening = false;

            // Stop을 눌러도 Final transcript는 약간 늦게 올 수 있음.
            // 여기서 바로 전송하지 않고, Final이 오면 HandleFinalTranscript에서 전송한다.
            if (voice != null)
                voice.StopListening();

            // Final이 안 오면 멈춰버릴 수 있으니 타임아웃 안전장치
            if (_finalWaitCoroutine != null) StopCoroutine(_finalWaitCoroutine);
            _finalWaitCoroutine = StartCoroutine(WaitFinalTranscriptTimeout());
        }

        UpdatePTTLabel();
    }

    /// 그래프를 확인(Confirm)하는 버튼
    /// Button.onClick에 연결
    public void UI_ConfirmGraph()
    {
        if (phase != AppPhase.GraphReady) return;
        _ = ConfirmGraphAsync();
    }

    // =========================
    // Debug (Editor)
    // =========================

    /// TMP_InputField에 적은 텍스트를 "Final transcript"처럼 처리
    /// Quest 없이도 전체 흐름(전송/폴링/상태전환)을 에디터에서 검증 가능
    public void UI_DebugSendTranscriptFromInput()
    {
        if (debugInput == null)
        {
            Debug.LogWarning("debugInput is not assigned.");
            return;
        }

        string text = debugInput.text;
        if (string.IsNullOrWhiteSpace(text)) return;

        Debug_FakeFinalTranscript(text, 1.0f);
    }

    /// 서버 없이 그래프 렌더러만 검증하고 싶을 때(노드/엣지 프리팹 연결 확인)
    public void UI_SpawnDummyGraph()
    {
        if (graphRenderer == null)
        {
            Debug.LogWarning("graphRenderer is not assigned.");
            return;
        }

        var st = new GraphState
        {
            nodes = new List<GraphNode>
            {
                new GraphNode { id="A", type="intent", label="A", pos=new Vec3{ x=-0.2f, y=0.0f, z=0.6f } },
                new GraphNode { id="B", type="tool",   label="B", pos=new Vec3{ x= 0.2f, y=0.0f, z=0.6f } },
                new GraphNode { id="C", type="param",  label="C", pos=new Vec3{ x= 0.0f, y=0.2f, z=0.6f } },
            },
            edges = new List<GraphEdge>
            {
                new GraphEdge { id="E1", from="A", to="C", type="rel" },
                new GraphEdge { id="E2", from="C", to="B", type="rel" },
            }
        };

        graphRenderer.Render(st);
        lastGraphVersion = 1;
        SetPhase(AppPhase.GraphReady);
    }

    /// 외부에서 "Final transcript"가 온 것처럼 강제 호출하는 함수
    public void Debug_FakeFinalTranscript(string text, float confidence)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        // 이미 전송/폴링 중이면 중복 전송 방지
        if (phase == AppPhase.SendingUtterance || phase == AppPhase.WaitingGraph)
            return;

        // Final이 왔으니 타임아웃 코루틴 정리
        if (_finalWaitCoroutine != null)
        {
            StopCoroutine(_finalWaitCoroutine);
            _finalWaitCoroutine = null;
        }

        // 토글 UI도 "듣기 종료" 상태로 정리
        _isListening = false;
        UpdatePTTLabel();

        _ = SendUtteranceEndAsync(text, confidence);
    }

    // =========================
    // Voice 이벤트 처리
    // =========================

    /// VoiceController에서 최종 전사(text) 결과가 도착하면 호출된다.
    private void HandleFinalTranscript(string text, float confidence)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        // 이미 전송/폴링 중이면 중복 전송 방지
        if (phase == AppPhase.SendingUtterance || phase == AppPhase.WaitingGraph)
            return;

        // Final이 왔으니 타임아웃 코루틴 정리
        if (_finalWaitCoroutine != null)
        {
            StopCoroutine(_finalWaitCoroutine);
            _finalWaitCoroutine = null;
        }

        // 토글 UI도 "듣기 종료" 상태로 정리
        _isListening = false;
        UpdatePTTLabel();

        // 전송 시작
        _ = SendUtteranceEndAsync(text, confidence);
    }

    private IEnumerator WaitFinalTranscriptTimeout()
    {
        float t = 0f;
        while (t < waitFinalTranscriptTimeout)
        {
            t += 0.25f;
            yield return new WaitForSeconds(0.25f);

            // Final이 오면 이 코루틴은 HandleFinalTranscript에서 StopCoroutine으로 정리됨
            if (phase == AppPhase.SendingUtterance || phase == AppPhase.WaitingGraph)
                yield break;
        }

        Debug.LogError("Final transcript timeout. Back to Idle.");
        SetPhase(AppPhase.Idle);

        // 토글 UI도 Idle로 맞춰줌
        _isListening = false;
        UpdatePTTLabel();
    }

    // =========================
    // 네트워크: utterance_end 전송
    // =========================

    private async System.Threading.Tasks.Task SendUtteranceEndAsync(string text, float confidence)
    {
        if (network == null)
        {
            Debug.LogError("NetworkClient is not assigned.");
            SetPhase(AppPhase.Error);
            return;
        }

        SetPhase(AppPhase.SendingUtterance);

        var envelope = new UtteranceEndEnvelope
        {
            session_id = sessionId,
            event_id = $"e_{Guid.NewGuid():N}".Substring(0, 8),
            type = "utterance_end",
            ts = DateTimeOffset.Now.ToString("o"),
            payload = new UtteranceEndPayload
            {
                utterance_id = $"u_{Guid.NewGuid():N}".Substring(0, 8),
                text = text,
                asr_confidence = confidence
            }
        };

        // NetworkClient.PostEventAsync<T> (제네릭) 호출
        bool ok = await network.PostEventAsync(envelope);
        if (!ok)
        {
            SetPhase(AppPhase.Error);
            return;
        }

        StartPollingGraph();
    }

    // =========================
    // 네트워크: 그래프 폴링
    // =========================

    private void StartPollingGraph()
    {
        SetPhase(AppPhase.WaitingGraph);

        if (_pollCoroutine != null) StopCoroutine(_pollCoroutine);
        _pollCoroutine = StartCoroutine(PollGraphLoop());
    }

    private IEnumerator PollGraphLoop()
    {
        float t = 0f;

        while (t < pollTimeoutSec)
        {
            t += pollInterval;

            var task = network.GetGraphResultAsync(sessionId, lastGraphVersion);
            while (!task.IsCompleted) yield return null;

            var res = task.Result;

            // ready=true면 그래프 상태가 도착
            if (res != null && res.ready && res.graph_state != null)
            {
                lastGraphVersion = res.version;

                if (graphRenderer == null)
                {
                    Debug.LogError("GraphRenderer is not assigned.");
                    SetPhase(AppPhase.Error);
                    yield break;
                }

                graphRenderer.Render(res.graph_state);

                SetPhase(AppPhase.GraphReady);
                yield break;
            }

            yield return new WaitForSeconds(pollInterval);
        }

        Debug.LogError("Graph polling timeout");
        SetPhase(AppPhase.Error);
    }

    // =========================
    // 네트워크: graph_confirm 전송
    // =========================

    private async System.Threading.Tasks.Task ConfirmGraphAsync()
    {
        if (network == null)
        {
            Debug.LogError("NetworkClient is not assigned.");
            SetPhase(AppPhase.Error);
            return;
        }

        SetPhase(AppPhase.ConfirmingGraph);

        var envelope = new GraphConfirmEnvelope
        {
            session_id = sessionId,
            event_id = $"e_{Guid.NewGuid():N}".Substring(0, 8),
            type = "graph_confirm",
            ts = DateTimeOffset.Now.ToString("o"),
            payload = new GraphConfirmPayload
            {
                graph_version = lastGraphVersion
            }
        };

        bool ok = await network.PostEventAsync(envelope);
        if (!ok)
        {
            SetPhase(AppPhase.Error);
            return;
        }

        SetPhase(AppPhase.TransitionNext);
        // TODO: Panel 전환 or Scene 로드
    }

    // =========================
    // 유틸
    // =========================

    private void SetPhase(AppPhase next)
    {
        phase = next;

        if (statusText != null)
            statusText.text = phase.ToString();

        // Confirm 버튼은 GraphReady 상태에서만 활성화되게(원하면 조건 수정)
        if (confirmButton != null)
            confirmButton.interactable = (phase == AppPhase.GraphReady);

        Debug.Log($"Phase => {phase}");
    }

    private void UpdatePTTLabel()
    {
        if (pttButtonText == null) return;
        pttButtonText.text = _isListening ? "Stop" : "Start Talking";
    }

    // =========================
    // DTO (JsonUtility 안정 직렬화용)
    // =========================

    [Serializable]
    public class UtteranceEndEnvelope
    {
        public string session_id;
        public string event_id;
        public string type;
        public string ts;
        public UtteranceEndPayload payload;
    }

    [Serializable]
    public class UtteranceEndPayload
    {
        public string utterance_id;
        public string text;
        public float asr_confidence;
    }

    [Serializable]
    public class GraphConfirmEnvelope
    {
        public string session_id;
        public string event_id;
        public string type;
        public string ts;
        public GraphConfirmPayload payload;
    }

    [Serializable]
    public class GraphConfirmPayload
    {
        public int graph_version;
    }
}
