using UnityEngine;
using TMPro;
using System;

public class LobbyMeetingUI : MonoBehaviour
{
    [Header("Top Clock UI (Left)")]
    [SerializeField] private TextMeshProUGUI timeText;     // 시간 (예: 13:27)
    [SerializeField] private TextMeshProUGUI dateFullText; // 년 월 일 (예: 2026년 2월 12일)
    [SerializeField] private TextMeshProUGUI dayText;      // 요일 (예: 목요일)

    [Header("Lobby Meeting Header (Center)")]
    [SerializeField] private TextMeshProUGUI meetingDateText; // 2026.02.12 (오늘)

    void Update()
    {
        UpdateLobbyUI();
    }

    void UpdateLobbyUI()
    {
        DateTime now = DateTime.Now;

        // 1. 시간 업데이트
        if (timeText != null) 
            timeText.text = now.ToString("HH:mm");

        // 2. 년 월 일 업데이트
        if (dateFullText != null) 
            dateFullText.text = now.ToString("yyyy년 M월 d일");

        // 3. 요일 업데이트 ("x요일" 형식 보장)
        if (dayText != null) 
        {
            // "dddd" 포맷 자체가 '월요일', '화요일' 등 전체 요일 명칭을 반환합니다.
            dayText.text = now.ToString("dddd");
        }

        // 4. 중앙 회의 목록 헤더 업데이트
        if (meetingDateText != null)
        {
            string formattedDate = now.ToString("yyyy.MM.dd");
            meetingDateText.text = $"{formattedDate} (오늘)";
        }
    }
}