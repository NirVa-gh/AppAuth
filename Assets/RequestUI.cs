using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RequestUI : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField titleInput;
    public TMP_InputField contentInput;
    public TMP_Text statusInput;
    public TMP_Text idText;
    public TMP_Text statusText;
    public Button saveButton;
    [SerializeField] private ContractsLoader contractsLoader;
    private int currentRequestId; // ������� ID ������

    private void Start()
    {
        saveButton.onClick.AddListener(OnSaveClicked); // ������������� �� ������� ������
    }

    public void SetRequestData(RequestData requestData)
    {
        if (requestData == null) return;

        currentRequestId = requestData.id;
        titleInput.text = requestData.title;
        contentInput.text = requestData.content;
        statusInput.text = requestData.status;
        idText.text = requestData.id.ToString();
    }

    private void OnSaveClicked()
    {
        contractsLoader.LoadByUserIDContracts(AuthManager.Instance.GetUserId());
        if (currentRequestId == 0)
        {
            Debug.LogWarning("�� ������� ������ ��� ����������");
            return;
        }

        string newTitle = titleInput.text.Trim();
        string newContent = contentInput.text.Trim();

        if (string.IsNullOrEmpty(newTitle) || string.IsNullOrEmpty(newContent))
        {
            Debug.LogWarning("��������� ��� ���� ����� �����������");
            return;
        }

        RequestData updatedRequest = new RequestData
        {
            id = currentRequestId,
            title = newTitle,
            content = newContent,
            status = statusInput.text
        };

        AuthManager.Instance.UpdateRequest(updatedRequest, (success, message) =>
        {
            if (success)
            {
                Debug.Log("������ ������� ���������!");
                // ����� �������� ����������� �� �������� ����������
            }
            else
            {
                Debug.LogError($"������ ��� ����������: {message}");
            }
        });
    }

}