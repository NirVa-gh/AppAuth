using TMPro;
using UnityEngine;

public class ContractView : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text contentText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text idText;


    public void Initialize(RequestData request)
    {
        titleText.text = request.title;
        //contentText.text = request.content;
        statusText.text = request.status;
        idText.text = $"{request.id}";
    }
}