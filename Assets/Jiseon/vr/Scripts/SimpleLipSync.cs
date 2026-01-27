using UnityEngine;
using Fusion;

public class SimpleLipSync : NetworkBehaviour
{
    public VoiceLevelSource_V2 source;
    public SkinnedMeshRenderer headMesh;
    public int mouthBlendShapeIndex = 0; // 아바타 머리의 입 벌리기 인덱스
    public float maxMouthOpen = 100f;    // 입이 최대로 벌어질 값

    void Update()
    {
        if (source == null || headMesh == null) return;

        // level01(0~1) 값에 따라 입을 벌림
        float weight = source.level01 * maxMouthOpen;
        headMesh.SetBlendShapeWeight(mouthBlendShapeIndex, weight);
    }
}