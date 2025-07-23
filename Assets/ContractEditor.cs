using System;
using System.Collections; // ��������� ��� �������
using TMPro;
using UIWidgets;
using UnityEngine;
using UnityEngine.UI;

public class ContractEditor : Utility
{
    [Header("Text Fields")]
    [SerializeField] private TMP_Text IDText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text statusText;

    [Header("Buttons")]
    [SerializeField] private Button editButton;
    [SerializeField] private Button deleteButton;

    [Header("References")]
    [SerializeField] private GameObject editContractPanel;
    [SerializeField] private ContractsLoader contractsLoader;
    public static event Action<int> OnRequestUpdated;
    private RequestUI requestUI;
    private int currentRequestId;
    private Coroutine updateRoutine;
    private bool isUpdating;
    private UIWidget uIWidget;

    private void Start()
    {
        editButton.onClick.AddListener(OnEditButtonClicked);
        OnRequestUpdated += (id) => RefreshTable();
        deleteButton.onClick.AddListener(OnDeleteClicked);

        if (!int.TryParse(IDText.text, out currentRequestId))
        {
            Debug.LogWarning("Failed to parse request ID");
            currentRequestId = 0;
        }

        editButton.onClick.AddListener(() =>
        {
            if (currentRequestId <= 0)
            {
                Debug.LogWarning("������� �������������� ��� ID ������");
                ShowMessage("������� �������� ��� �������� ������");
                return;
            }
            LoadRequestData(currentRequestId);
        });

        //StartAutoUpdate();
    }

    private void OnEditButtonClicked()
    {
        contractsLoader.LoadByUserIDContracts(AuthManager.Instance.GetUserId());


        if (currentRequestId <= 0)
        {
            Debug.LogWarning("������� �������������� ��� ID ������");
            ShowMessage("������� �������� ��� �������� ������");
            return;
        }

        // ���������� ������ ��������������
        if (editContractPanel != null)
        {
            editContractPanel.GetComponent<UIWidget>().Show();
            LoadRequestData(currentRequestId); // ��������� ������ ������
        }
        else
        {
            Debug.LogError("EditContractPanel �� �������� � ����������");
        }
    }

    private void OnDestroy()
    {
        // ��������� ��� ����������� �������
        StopAutoUpdate();
    }

    #region Auto-Update Logic
    public void StartAutoUpdate()
    {
        if (!isUpdating)
        {
            isUpdating = true;
            updateRoutine = StartCoroutine(UpdateFieldsPeriodically());
            Debug.Log("Auto-update started");
        }
    }

    public void StopAutoUpdate()
    {
        if (isUpdating && updateRoutine != null)
        {
            StopCoroutine(updateRoutine);
            isUpdating = false;
            Debug.Log("Auto-update stopped");
        }
    }

    private IEnumerator UpdateFieldsPeriodically()
    {
        WaitForSeconds waitTime = new WaitForSeconds(5f); // ����������� ������

        while (isUpdating)
        {
            yield return waitTime;

            if (currentRequestId != 0 && gameObject.activeInHierarchy)
            {
                Debug.Log($"Auto-updating request {currentRequestId}");
                LoadRequestData(currentRequestId);
            }
        }
    }
    #endregion

    public void OnEditButtonClicked(int requestId)
    {
        RequestUI requestUI = FindObjectOfType<RequestUI>();
        LoadRequestData(requestId);
    }

    public void LoadRequestData(int requestId)
    {
        AuthManager.Instance.GetRequest(requestId, (success, requestData) =>
        {
            if (success && requestData != null)
            {
                // �������� ��������� RequestUI �� ������ ��������������
                RequestUI requestUI = editContractPanel.GetComponent<RequestUI>();

                if (requestUI != null)
                {
                    requestUI.SetRequestData(requestData); // �������� ������ � UI
                }
                else
                {
                    Debug.LogError("RequestUI �� ������ �� ������ ��������������");
                }

                // ������������� ��������� ��������� ���� � ContractEditor (���� �����)
                IDText.text = requestData.id.ToString();
                titleText.text = requestData.title;
                statusText.text = requestData.status;

                currentRequestId = requestData.id;
            }
            else
            {
                Debug.LogError("�� ������� ��������� ������ ������");
            }
        });
    }

    private IEnumerator RefreshAfterUpdate()
    {
        yield return new WaitForSeconds(0.5f); // ��������� �������� ��� �������
        LoadRequestData(currentRequestId);
        OnRequestUpdated?.Invoke(currentRequestId);
    }

    private void RefreshTable()
    {
        // ��� ����� ��� ������� ���������� �������
        Debug.Log($"��������� ������� ����� ��������� ������ {currentRequestId}");
        // ��������:
        // FindObjectOfType<RequestsTableUI>()?.Refresh();
    }

    public void OnDeleteClicked()
    {
        contractsLoader.LoadByUserIDContracts(AuthManager.Instance.GetUserId());
        if (currentRequestId == 0)
        {
            Debug.LogWarning("�� ������� ������ ��� ��������");
            ShowMessage("�������� ������ ��� ��������");
            return;
        }

        // ������������� �������� (�����������)
        if (!ShowConfirmationDialog("�� �������, ��� ������ ������� ��� ������?"))
            return;

        AuthManager.Instance.DeleteRequest(currentRequestId, (success, message) =>
        {
            if (success)
            {
                Debug.Log("������ �������");
                ShowMessage("������ �������");

                // ���������� ������ ������
                Destroy(gameObject); // ������� ������� ContractPrefab

                // ���� ����� �������� ������������ UI (��������, ������ ������)
                OnRequestUpdated?.Invoke(currentRequestId);
            }
            else
            {
                Debug.LogError($"������ ��������: {message}");
                ShowMessage("������ ��� ��������");
            }
        });
    }

    private bool ShowConfirmationDialog(string message)
    {
        // ���������� ������ ������������� ����� ��� UI
        Debug.Log(message);
        return true; // ��� ������� ������ ���������� true
    }

    private void ResetUI()
    {
        IDText.text = "";
        titleText.text = "";
        statusText.text = "";
        requestUI.titleInput.text = "";
        requestUI.contentInput.text = "";
        requestUI.statusInput.text = "new";
    }

    public void SetContractId(int newId)
    {
        currentRequestId = newId;
        if (IDText != null)
        {
            IDText.text = newId.ToString();
        }
    }
    public void InitializeWithData(RequestData requestData)
    {
        if (requestData == null)
        {
            Debug.LogError("�������� null-������ ������");
            return;
        }

        // ������������� ID
        currentRequestId = requestData.id > 0 ? requestData.id : 0;

        // ��������� UI � ��������� �� null
        if (IDText != null)
            IDText.text = currentRequestId.ToString();

        if (titleText != null)
            titleText.text = string.IsNullOrEmpty(requestData.title) ? "����� ������" : requestData.title;

        if (statusText != null)
            statusText.text = string.IsNullOrEmpty(requestData.status) ? "new" : requestData.status;

        // ������������� ����� ��������������
        if (requestUI != null)
        {
            requestUI.titleInput.text = string.IsNullOrEmpty(requestData.title) ? "" : requestData.title;
            requestUI.contentInput.text = string.IsNullOrEmpty(requestData.content) ? "" : requestData.content;
            requestUI.statusInput.text = string.IsNullOrEmpty(requestData.status) ? "new" : requestData.status;
            requestUI.idText.text = currentRequestId.ToString();
        }
    }

}