using TMPro;
using UnityEngine;

public class Utility : MonoBehaviour
{
    public TMP_Text message;

    public void ShowMessage(string messageLocal)
    {
        message.text = messageLocal;
    }
}
