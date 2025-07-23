using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ContractUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text idText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button editButton;
    [SerializeField] private ContractEditor contractEditor;

    private int contractId;

    public void Initialize(RequestData contractData)
    {
        contractId = contractData.id;

        if (idText != null) idText.text = $"ID: {contractData.id}";
        if (titleText != null) titleText.text = contractData.title;
        if (statusText != null) statusText.text = contractData.status;

        if (editButton != null)
        {
            editButton.onClick.AddListener(() =>
            {
                contractEditor.LoadRequestData(contractId);
            });
        }
    }
}