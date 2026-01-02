using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// 서버(FastAPI)와 통신하는 클래스
/// - POST /event
/// - GET /result/graph
public class NetworkClient : MonoBehaviour
{
    [Header("Server")]
    public string baseUrl = "http://127.0.0.1:8000";

    /// POST /event 로 이벤트(envelope)를 보냄
    /// T는 구체 타입 envelope여야 JsonUtility가 payload까지 잘 직렬화한다.
    public async Task<bool> PostEventAsync<T>(T envelope)
    {
        string url = $"{baseUrl}/event";
        string json = JsonUtility.ToJson(envelope);

        using var req = new UnityWebRequest(url, "POST");
        byte[] body = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        // Unity 6(6000) 기준: SendWebRequest()는 await 가능(AsyncOperation await 지원)
        await req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"POST /event failed: {req.error}\n{req.downloadHandler.text}");
            Debug.LogError($"Sent JSON: {json}");
            return false;
        }

        return true;
    }

    /// GET /result/graph 를 폴링해서 그래프 결과를 받음.
    /// GraphModels.cs에 GraphResultResponse/GraphState가 있어야 파싱 가능.
    public async Task<GraphResultResponse> GetGraphResultAsync(string sessionId, int sinceVersion)
    {
        string url =
            $"{baseUrl}/result/graph" +
            $"?session_id={UnityWebRequest.EscapeURL(sessionId)}" +
            $"&since_version={sinceVersion}";

        using var req = UnityWebRequest.Get(url);

        await req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"GET /result/graph failed: {req.error}\n{req.downloadHandler.text}");
            return null;
        }

        // 서버 JSON 키가 GraphResultResponse의 필드명과 정확히 일치해야 함
        // {"ready":true,"version":3,"graph_state":{"nodes":[...],"edges":[...]}}
        return JsonUtility.FromJson<GraphResultResponse>(req.downloadHandler.text);
    }
}