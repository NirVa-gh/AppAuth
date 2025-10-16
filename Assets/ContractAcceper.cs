using System;
using System.Collections; // ��������� ��� �������
using TMPro;
using UIWidgets;
using UnityEngine;
using UnityEngine.UI;

public class ContractAcceper : Utility
{
    [Header("Text Fields")]
    [SerializeField] private TMP_Text IDText;

    [Header("Buttons")]
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;

    [Header("References")]
    [SerializeField] private ContractsLoader contractsLoader;

    public static event Action<int> OnRequestUpdated;
    private RequestUI requestUI;
    private int currentRequestId;
    private Coroutine updateRoutine;
    private bool isUpdating;
    private UIWidget uIWidget;

    private void Start()
    {
        OnRequestUpdated += (id) => RefreshTable();
        declineButton.onClick.AddListener(OnDeleteClicked);

        string rawText = IDText.text.Trim();
        if (!int.TryParse(rawText, out currentRequestId)) // ID:20
        {
            Debug.LogWarning("Failed to parse request ID");
            currentRequestId = 0;
        }///!!!!!!!!!!!!!!!!!!!!!!!!!!!
        Debug.LogWarning(currentRequestId);
        acceptButton.onClick.AddListener(() =>
        {
            if (currentRequestId <= 0)
            {
                Debug.LogWarning("������� �������������� ��� ID ������");
                return;
            }
           //ACCEPT METHOD
        });

        //StartAutoUpdate();
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
            }
        }
    }
    #endregion
    private IEnumerator RefreshAfterUpdate()
    {
        yield return new WaitForSeconds(0.5f); // ��������� �������� ��� �������
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
        AuthManager.Instance.DeleteRequestAdmin(currentRequestId, (success, message) =>
        {
            if (success)
            {
                Debug.Log("������ �������");

                // ���������� ������ ������
                Destroy(gameObject); // ������� ������� ContractPrefab

                // ���� ����� �������� ������������ UI (��������, ������ ������)
                OnRequestUpdated?.Invoke(currentRequestId);
            }
            else
            {
                Debug.LogError($"������ ��������: {message}");
            }
        });
    }

    public void SetContractId(int newId)
    {
        currentRequestId = newId;
        if (IDText != null)
        {
            IDText.text = newId.ToString();
        }
    }
}
