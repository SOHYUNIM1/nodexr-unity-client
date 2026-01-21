using Fusion;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class XRPlayerBinder : NetworkBehaviour
{
    private Transform xrOrigin;

    public override void Spawned()
    {
        // 로컬 플레이어만 XR Origin을 가진다
        if (!Object.HasInputAuthority)
            return;

        // 씬에 있는 XR Origin 찾기
        var xrOriginComponent = FindObjectOfType<XROrigin>();

        if (xrOriginComponent == null)
        {
            Debug.LogError("XR Origin not found in scene!");
            return;
        }

        xrOrigin = xrOriginComponent.transform;

        // 최초 위치 동기화
        transform.position = xrOrigin.position;
        transform.rotation = xrOrigin.rotation;
    }

    private void LateUpdate()
    {
        if (!Object.HasInputAuthority)
            return;

        if (xrOrigin == null)
            return;

        // XR 이동 결과를 네트워크 오브젝트에 반영
        transform.position = xrOrigin.position;
        transform.rotation = xrOrigin.rotation;
    }
}
