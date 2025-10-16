using System;
using System.Collections; // Добавлено для корутин
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
                Debug.LogWarning("Попытка редактирования без ID заявки");
                return;
            }
           //ACCEPT METHOD
        });

        //StartAutoUpdate();
    }


    private void OnDestroy()
    {
        // Остановка при уничтожении объекта
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
        WaitForSeconds waitTime = new WaitForSeconds(5f); // Оптимизация памяти

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
        yield return new WaitForSeconds(0.5f); // Небольшая задержка для сервера
        OnRequestUpdated?.Invoke(currentRequestId);
    }
    private void RefreshTable()
    {
        // Ваш метод для полного обновления таблицы
        Debug.Log($"Обновляем таблицу после изменения заявки {currentRequestId}");
        // Например:
        // FindObjectOfType<RequestsTableUI>()?.Refresh();
    }

    public void OnDeleteClicked()
    {
        AuthManager.Instance.DeleteRequestAdmin(currentRequestId, (success, message) =>
        {
            if (success)
            {
                Debug.Log("Заявка удалена");

                // Уничтожаем префаб заявки
                Destroy(gameObject); // Удаляем текущий ContractPrefab

                // Если нужно обновить родительский UI (например, список заявок)
                OnRequestUpdated?.Invoke(currentRequestId);
            }
            else
            {
                Debug.LogError($"Ошибка удаления: {message}");
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
