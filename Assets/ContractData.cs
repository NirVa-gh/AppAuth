using System;
using UnityEngine;

[Serializable]
public class ContractData
{
    public int id;
    public string title;
    public string content;
    public string status;
    public string created_at;
    public int user_id;

    public DateTime GetCreationDate()
    {
        return DateTime.Parse(created_at);
    }

    public bool IsCompleted()
    {
        return status == "completed";
    }
}