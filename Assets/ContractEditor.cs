using System;
using System.Collections; // Добавлено для корутин
using TMPro;
using UIWidgets;
using UnityEngine;
using UnityEngine.Networking;
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
                Debug.LogWarning("Попытка редактирования без ID заявки");
                ShowMessage("Сначала создайте или выберите заявку");
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
            Debug.LogWarning("Попытка редактирования без ID заявки");
            ShowMessage("Сначала создайте или выберите заявку");
            return;
        }

        // Активируем панель редактирования
        if (editContractPanel != null)
        {
            editContractPanel.GetComponent<UIWidget>().Show();
            LoadRequestData(currentRequestId); // Загружаем данные заявки
        }
        else
        {
            Debug.LogError("EditContractPanel не назначен в инспекторе");
        }
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
                // Получаем компонент RequestUI из панели редактирования
                RequestUI requestUI = editContractPanel.GetComponent<RequestUI>();

                if (requestUI != null)
                {
                    requestUI.SetRequestData(requestData); // Передаем данные в UI
                }
                else
                {
                    Debug.LogError("RequestUI не найден на панели редактирования");
                }

                // Дополнительно обновляем текстовые поля в ContractEditor (если нужно)
                IDText.text = requestData.id.ToString();
                titleText.text = requestData.title;
                statusText.text = requestData.status;

                currentRequestId = requestData.id;
            }
            else
            {
                Debug.LogError("Не удалось загрузить данные заявки");
            }
        });
    }

    private IEnumerator RefreshAfterUpdate()
    {
        yield return new WaitForSeconds(0.5f); // Небольшая задержка для сервера
        LoadRequestData(currentRequestId);
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
        contractsLoader.LoadByUserIDContracts(AuthManager.Instance.GetUserId());
        if (currentRequestId == 0)
        {
            Debug.LogWarning("Не выбрана заявка для удаления");
            ShowMessage("Выберите заявку для удаления");
            return;
        }

        // Подтверждение удаления (опционально)
        if (!ShowConfirmationDialog("Вы уверены, что хотите удалить эту заявку?"))
            return;

        AuthManager.Instance.DeleteRequest(currentRequestId, (success, message) =>
        {
            if (success)
            {
                Debug.Log("Заявка удалена");
                //ShowMessage("Заявка удалена");

                // Уничтожаем префаб заявки
                Destroy(gameObject); // Удаляем текущий ContractPrefab

                // Если нужно обновить родительский UI (например, список заявок)
                OnRequestUpdated?.Invoke(currentRequestId);
            }
            else
            {
                Debug.LogError($"Ошибка удаления: {message}");
                //ShowMessage("Ошибка при удалении");
            }
        });
    }

    private bool ShowConfirmationDialog(string message)
    {
        // Реализуйте диалог подтверждения через ваш UI
        Debug.Log(message);
        return true; // Для примера всегда возвращает true
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
            Debug.LogError("Переданы null-данные заявки");
            return;
        }

        // Устанавливаем ID
        currentRequestId = requestData.id > 0 ? requestData.id : 0;

        // Обновляем UI с проверкой на null
        if (IDText != null)
            IDText.text = currentRequestId.ToString();

        if (titleText != null)
            titleText.text = string.IsNullOrEmpty(requestData.title) ? "Новая заявка" : requestData.title;

        if (statusText != null)
            statusText.text = string.IsNullOrEmpty(requestData.status) ? "new" : requestData.status;

        // Инициализация формы редактирования
        if (requestUI != null)
        {
            requestUI.titleInput.text = string.IsNullOrEmpty(requestData.title) ? "" : requestData.title;
            requestUI.contentInput.text = string.IsNullOrEmpty(requestData.content) ? "" : requestData.content;
            requestUI.statusInput.text = string.IsNullOrEmpty(requestData.status) ? "new" : requestData.status;
            requestUI.idText.text = currentRequestId.ToString();
        }
    }

}
