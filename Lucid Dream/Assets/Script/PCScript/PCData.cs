using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

[Serializable]
public class NoteData
{
    [Header("📅 Day Settings")]
    public int dayNumber = 1;

    [Header("📝 Note Info")]
    public LocalizedString noteTitle;   // ✨ ช่องสำหรับใส่ไตเติ้ลหัวข้อ เช่น "Note", "Day 1 Entry"
    public LocalizedString noteContent; // ✨ ช่องใส่เนื้อหาบันทึก
}

[Serializable]
public class ChatMessageData
{
    [Header("📅 Day Settings")]
    public int dayNumber = 1;

    [Header("💬 Chat Info")]
    public string senderID = "User01";   // ID สั้นๆ ไว้จัดกลุ่มคนส่ง
    public LocalizedString senderName;  // ✨ ช่องใส่ชื่อคนส่ง เช่น "Name01", "Doctor B"
    public LocalizedString messageText; // ✨ ช่องใส่เนื้อหาแชต
}

[CreateAssetMenu(fileName = "PC_Database", menuName = "Analog PC/Database")]
public class PCData : ScriptableObject
{
    [Header("📝 Note List")]
    public List<NoteData> notes = new List<NoteData>();

    [Header("💬 Chat List")]
    public List<ChatMessageData> chatMessages = new List<ChatMessageData>();
}