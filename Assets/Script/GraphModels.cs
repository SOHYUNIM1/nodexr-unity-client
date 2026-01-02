using System;
using System.Collections.Generic;

/// <summary>
/// GET /result/graph 응답 전체
/// 서버 JSON 키가 필드명과 동일해야 JsonUtility로 파싱 가능
/// </summary>
[Serializable]
public class GraphResultResponse
{
    public bool ready;
    public int version;
    public GraphState graph_state;
}

/// <summary>
/// 그래프 상태: 노드/엣지 목록
/// </summary>
[Serializable]
public class GraphState
{
    public List<GraphNode> nodes;
    public List<GraphEdge> edges;
}

/// <summary>
/// 노드 데이터
/// pos는 Unity에서 배치할 좌표(localPosition으로 사용)
/// </summary>
[Serializable]
public class GraphNode
{
    public string id;
    public string type;
    public string label;
    public Vec3 pos;
}

/// <summary>
/// 엣지 데이터(최소)
/// from -> to 연결
/// </summary>
[Serializable]
public class GraphEdge
{
    public string id;
    public string from;
    public string to;
    public string type;
}

/// <summary>
/// JsonUtility는 Vector3를 바로 못 쓰는 경우가 있어서 x,y,z 구조로 둠.
/// </summary>
[Serializable]
public class Vec3
{
    public float x;
    public float y;
    public float z;
}
