using UnityEngine;
using TMPro;
using Fusion;
using System.Text;

public class PlayerListUI : MonoBehaviour
{
    public TextMeshProUGUI playerListText;
    private NetworkRunner _runner;

    void Update()
    {
        // 1. 현재 잡고 있는 러너가 없거나, 연결이 끊겼다면 다시 찾습니다.
        if (_runner == null || !_runner.IsRunning)
        {
            // 씬에 있는 모든 NetworkRunner를 배열로 가져옵니다.
            NetworkRunner[] allRunners = FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None);

            foreach (var r in allRunners)
            {
                if (r.IsRunning) // 실제로 세션에 접속 중인 러너만 선택
                {
                    _runner = r;
                    break;
                }
            }
        }

        // 2. 여전히 못 찾았다면 화면에 상태 표시
        if (_runner == null || !_runner.IsRunning)
        {
            if (playerListText != null)
                playerListText.text = "<color=red>실행 중인 네트워크 세션을 찾을 수 없습니다.</color>";
            return;
        }

        UpdateList();
    }

    void UpdateList()
    {
        if (playerListText == null) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<color=yellow><b>[ 현재 참여자 명단 ]</b></color>");
        sb.AppendLine($"<size=80%>세션명: {_runner.SessionInfo.Name}</size>"); // 세션 확인용 추가
        sb.AppendLine("---------");

        // 3. 현재 러너의 ActivePlayers 정보를 가져옵니다.
        foreach (var playerRef in _runner.ActivePlayers)
        {
            string nameDisplay = $"ID: {playerRef.PlayerId}";

            if (_runner.TryGetPlayerObject(playerRef, out var playerObj))
            {
                var info = playerObj.GetComponent<PlayerInfo>();
                if (info != null)
                {
                    nameDisplay = info.playerName.ToString();
                    if (string.IsNullOrEmpty(nameDisplay)) nameDisplay = $"Player {playerRef.PlayerId}";
                }

                if (playerRef == _runner.LocalPlayer) nameDisplay += " <color=green>(나)</color>";
            }
            else
            {
                nameDisplay += " (오브젝트 로딩 중...)";
            }
            sb.AppendLine("- " + nameDisplay);
        }
        playerListText.text = sb.ToString();
    }
}