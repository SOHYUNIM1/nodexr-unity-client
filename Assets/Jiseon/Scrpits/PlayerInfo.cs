using Fusion;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInfo : NetworkBehaviour
{
    public enum Role : byte
    {
        Cleaner = 0,
        Saboteur = 1
    }

    [Header("Render / UI")]
    public MeshRenderer MeshRenderer;
    public TMP_Text nameDisplayTMP;
    //public HealthBar healthBar;
    public Button readyButton;

    [Networked, OnChangedRender(nameof(ColorChanged))]
    public Color NetworkedColor { get; set; }

    [Networked, OnChangedRender(nameof(OnNameChanged))]
    public NetworkString<_64> playerName { get; set; }

    [Networked, OnChangedRender(nameof(OnReadyStateChanged))]
    public NetworkBool IsReady { get; private set; }

    // ���� ��Ʈ��ũ ����
    [Networked, OnChangedRender(nameof(OnRoleChangedRender))]
    public Role PlayerRole { get; private set; }

    [Header("Leader UI")]
    public GameObject leaderIcon; // 인스펙터에서 왕관(호스트) 아이콘 연결

    // 네트워크를 통해 모든 유저에게 동기화될 호스트 상태 값
    [Networked, OnChangedRender(nameof(UpdateLeaderIcon))]
    public NetworkBool IsLeader { get; set; }


    bool _uiWired;

    public string cachedName = "(unnamed)";

    private bool IsHost => Runner != null && (Runner.IsSharedModeMasterClient || Runner.IsServer);

    void Start()
    {
        if (HasInputAuthority && readyButton != null && !_uiWired)
        {
            if (!IsHost)
            {
                
            }
            else
            {
                
            }
        }
    }

    public override void Spawned()
    {

        cachedName = playerName.ToString();

        if (nameDisplayTMP != null)
        {
            nameDisplayTMP.text = cachedName;
            transform.gameObject.name = cachedName;
        }

        // 1. 객체가 생성될 때, 해당 객체의 주인(StateAuthority)이 호스트 여부를 판단해서 기록합니다.
        if (Object.HasStateAuthority)
        {
            IsLeader = IsHost;
        }

        // 2. 스폰 즉시 아이콘 상태를 반영합니다.
        UpdateLeaderIcon();
    }

    // IsLeader 값이 바뀌거나 Spawn 시 호출되어 실제 아이콘 오브젝트를 끄고 켭니다.
    void UpdateLeaderIcon()
    {
        if (leaderIcon != null)
        {
            leaderIcon.SetActive(IsLeader);
        }
    }


    
    void OnDestroy()
    {
    }

    public void SetPlayerName(string name)
    {
        if (HasStateAuthority)
        {
            playerName = name;
            cachedName = name;
            if (nameDisplayTMP) nameDisplayTMP.text = name;
        }
        else
        {
            RPC_SetPlayerName(name);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetPlayerName(string name)
    {
        playerName = name;
        cachedName = name;
        if (nameDisplayTMP) nameDisplayTMP.text = name;
    }

    public void ToggleReady()
    {
        if (!HasInputAuthority) return;
        RPC_ToggleReady(!IsReady);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_ToggleReady(bool newState)
    {
        IsReady = newState;
    }

    // Host�� ���� ������ �õ��� ��: ���� StateAuthority�� ���� ����, �ƴϸ� StateAuthority���� RPC ��û
    public void SetRoleServer(Role role)
    {
        if (HasStateAuthority)
        {
            PlayerRole = role;
            ApplyRoleToEquip(role);
        }
        else
        {
            RpcSetRole(role);
        }
    }

    private void ApplyRoleToEquip(Role role)
    {
        throw new NotImplementedException();
    }

    // �� ������Ʈ�� StateAuthority�� ������ ���
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RpcSetRole(Role role)
    {
        PlayerRole = role;
        ApplyRoleToEquip(role);
    }

    // ���� �ȳ� �޽���: �� ������Ʈ�� InputAuthority���Ը� ����
    [Rpc(RpcSources.All, RpcTargets.InputAuthority)]
    public void RpcShowRoleMessage(Role role, float seconds)
    {
        
    }

    public void ColorChanged()
    {
        if (MeshRenderer && MeshRenderer.material)
            MeshRenderer.material.color = NetworkedColor;
    }

    public void OnNameChanged()
    {
        cachedName = playerName.ToString();
        if (nameDisplayTMP)
            nameDisplayTMP.text = cachedName;
    }


    void OnReadyStateChanged()
    {
        if (HasInputAuthority && readyButton != null)
        {
            var tmp = readyButton.GetComponentInChildren<TMP_Text>();
            if (tmp) tmp.text = IsReady ? "Wait..." : "Ready";
        }

        if (nameDisplayTMP)
            nameDisplayTMP.color = IsReady ? Color.green : Color.white;
    }

    void OnRoleChangedRender()
    {
        // �ʿ� �� ���Һ� ���� ������ ���⼭
        // if (nameDisplayTMP) nameDisplayTMP.color = PlayerRole == Role.Saboteur ? new Color(1f,0.5f,0.5f) : Color.white;
    }


    private void ApplyRoleToEquip(bool role)
    {
        
    }

    void OnDisable()
    {
        if (_uiWired && readyButton != null)
        {
            readyButton.onClick.RemoveListener(ToggleReady);
            _uiWired = false;
        }
    }
}