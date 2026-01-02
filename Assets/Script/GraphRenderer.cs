using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GraphState를 받아서 노드/엣지를 씬에 렌더링하는 컴포넌트
/// - 노드: nodePrefab Instantiate
/// - 엣지: LineRenderer(edgePrefab) Instantiate
/// </summary>
public class GraphRenderer : MonoBehaviour
{
    [Header("Parents")]
    public Transform nodesParent;   // GraphRoot/Nodes
    public Transform edgesParent;   // GraphRoot/Edges

    [Header("Prefabs")]
    public GameObject nodePrefab;   // PF_Node (Cube/Sphere 등)
    public LineRenderer edgePrefab; // PF_EdgeLine (LineRenderer 컴포넌트가 있는 프리팹)

    private readonly Dictionary<string, GameObject> _nodeMap = new();
    private readonly Dictionary<string, LineRenderer> _edgeMap = new();

    /// <summary>
    /// 서버에서 받은 GraphState를 씬에 반영
    /// </summary>
    public void Render(GraphState state)
    {
        if (state == null) return;

        // null 방어(서버가 nodes/edges를 null로 줄 수 있음)
        if (state.nodes == null) state.nodes = new List<GraphNode>();
        if (state.edges == null) state.edges = new List<GraphEdge>();

        // 필수 레퍼런스 체크
        if (nodesParent == null || edgesParent == null)
        {
            Debug.LogError("[GraphRenderer] nodesParent/edgesParent가 비어있음. GraphRoot/Nodes, GraphRoot/Edges를 연결해줘.");
            return;
        }
        if (nodePrefab == null || edgePrefab == null)
        {
            Debug.LogError("[GraphRenderer] nodePrefab/edgePrefab이 비어있음. PF_Node / PF_EdgeLine 프리팹을 연결해줘.");
            return;
        }

        // --------------------
        // 1) Nodes
        // --------------------
        var aliveNodeIds = new HashSet<string>();
        foreach (var n in state.nodes)
        {
            if (n == null || string.IsNullOrEmpty(n.id)) continue;

            aliveNodeIds.Add(n.id);

            if (!_nodeMap.TryGetValue(n.id, out var go) || go == null)
            {
                go = Instantiate(nodePrefab, nodesParent);
                go.name = $"Node_{n.id}";
                _nodeMap[n.id] = go;
            }

            // 위치 적용 (로컬 좌표)
            float x = (n.pos != null) ? n.pos.x : 0f;
            float y = (n.pos != null) ? n.pos.y : 0f;
            float z = (n.pos != null) ? n.pos.z : 0f;
            go.transform.localPosition = new Vector3(x, y, z);

            // TODO: 라벨/타입 표시(텍스트/아이콘)
        }

        // 제거
        var removeNodes = new List<string>();
        foreach (var kv in _nodeMap)
        {
            if (!aliveNodeIds.Contains(kv.Key) && kv.Value != null)
            {
                Destroy(kv.Value);
                removeNodes.Add(kv.Key);
            }
        }
        foreach (var id in removeNodes) _nodeMap.Remove(id);

        // --------------------
        // 2) Edges
        // --------------------
        var aliveEdgeIds = new HashSet<string>();
        foreach (var e in state.edges)
        {
            if (e == null || string.IsNullOrEmpty(e.id)) continue;

            aliveEdgeIds.Add(e.id);

            if (!_edgeMap.TryGetValue(e.id, out var lr) || lr == null)
            {
                lr = Instantiate(edgePrefab, edgesParent);
                lr.name = $"Edge_{e.id}";
                _edgeMap[e.id] = lr;
            }

            // from/to 노드가 모두 존재할 때만 선 갱신
            if (!string.IsNullOrEmpty(e.from) &&
                !string.IsNullOrEmpty(e.to) &&
                _nodeMap.TryGetValue(e.from, out var fromGo) &&
                _nodeMap.TryGetValue(e.to, out var toGo) &&
                fromGo != null && toGo != null)
            {
                lr.positionCount = 2;
                lr.useWorldSpace = false; // 로컬 좌표 기준
                lr.SetPosition(0, fromGo.transform.localPosition);
                lr.SetPosition(1, toGo.transform.localPosition);
            }
        }

        // 제거
        var removeEdges = new List<string>();
        foreach (var kv in _edgeMap)
        {
            if (!aliveEdgeIds.Contains(kv.Key) && kv.Value != null)
            {
                Destroy(kv.Value.gameObject);
                removeEdges.Add(kv.Key);
            }
        }
        foreach (var id in removeEdges) _edgeMap.Remove(id);
    }

    /// <summary>
    /// 서버 없이도 렌더링이 되는지 확인하는 샘플 그래프 생성
    /// 버튼 OnClick에 연결해서 테스트 가능
    /// </summary>
    public void DebugSpawnSample()
    {
        var s = new GraphState
        {
            nodes = new List<GraphNode>(),
            edges = new List<GraphEdge>()
        };

        s.nodes.Add(new GraphNode { id = "A", label = "A", pos = new Vec3 { x = -0.15f, y = 0.00f, z = 0.50f } });
        s.nodes.Add(new GraphNode { id = "B", label = "B", pos = new Vec3 { x =  0.15f, y = 0.00f, z = 0.50f } });
        s.nodes.Add(new GraphNode { id = "C", label = "C", pos = new Vec3 { x =  0.00f, y = 0.15f, z = 0.50f } });

        s.edges.Add(new GraphEdge { id = "E1", from = "A", to = "B" });
        s.edges.Add(new GraphEdge { id = "E2", from = "B", to = "C" });

        Render(s);
    }

    public void ClearAll()
    {
        foreach (var kv in _nodeMap)
            if (kv.Value != null) Destroy(kv.Value);

        foreach (var kv in _edgeMap)
            if (kv.Value != null) Destroy(kv.Value.gameObject);

        _nodeMap.Clear();
        _edgeMap.Clear();
    }
}
