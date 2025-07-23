using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ContractManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button createNewButton;
    [SerializeField] private GameObject contractPrefab; // ������ � ContractEditor
    [SerializeField] private Transform contractsContainer;
    [SerializeField] private ContractsLoader contractsLoader;
    private void Start()
    {
        createNewButton.onClick.AddListener(() => { contractsLoader.LoadByUserIDContracts(AuthManager.Instance.GetUserId()); });
        createNewButton.onClick.AddListener(CreateNewContract);
    }

    public void CreateNewContract()
    {
        RequestData newRequest = new RequestData()
        {
            title = $"����� ������ {DateTime.Now:HH:mm:ss}",
            content = $"Content_{Guid.NewGuid().ToString("N").Substring(0, 8)}",
            status = "new"
        };

        AuthManager.Instance.CreateRequest(newRequest, (success, message) =>
        {
            if (!success)
            {
                Debug.LogError($"������: {message}");
                return;
            }

            try
            {
                var response = JsonUtility.FromJson<RequestResponse>(message);

                // ��������� �������� ������
                if (response == null || !response.success || response.request == null)
                {
                    Debug.LogError("�������� ������ ������ �������");
                    return;
                }

                if (response.request.id <= 0)
                {
                    Debug.LogError($"���������� ID: {response.request.id}");
                    return;
                }

                CreateContractUIElement(response.request);
                Debug.Log($"������ �������. ID: {response.request.id}");
                
            }
            catch (Exception e)
            {
                Debug.LogError($"������ ���������: {e.Message}");
            }
        });
    }

    private void CreateContractUIElement(RequestData requestData)
    {
        // ��������� ������ �� ����������
        if (requestData == null || requestData.id <= 0)
        {
            Debug.LogError($"������������ ������ ������: {(requestData == null ? "null" : $"ID={requestData.id}")}");
            return;
        }

        // ��������� ������������ ����
        if (string.IsNullOrEmpty(requestData.title))
        {
            requestData.title = "��� ��������";
        }

        if (string.IsNullOrEmpty(requestData.content))
        {
            requestData.content = "���������� �����������";
        }

        // ������� ��������� �������
        GameObject newContract = Instantiate(contractPrefab, contractsContainer);
        ContractEditor editor = newContract.GetComponent<ContractEditor>();

        if (editor == null)
        {
            Debug.LogError("�� ������ ��������� ContractEditor �� �������");
            Destroy(newContract);
            return;
        }

        // �������������� � �������
        editor.InitializeWithData(requestData);
    }


}