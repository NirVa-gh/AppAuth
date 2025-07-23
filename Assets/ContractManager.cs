using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ContractManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button createNewButton;
    [SerializeField] private GameObject contractPrefab; // Префаб с ContractEditor
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
            title = $"Новая заявка {DateTime.Now:HH:mm:ss}",
            content = $"Content_{Guid.NewGuid().ToString("N").Substring(0, 8)}",
            status = "new"
        };

        AuthManager.Instance.CreateRequest(newRequest, (success, message) =>
        {
            if (!success)
            {
                Debug.LogError($"Ошибка: {message}");
                return;
            }

            try
            {
                var response = JsonUtility.FromJson<RequestResponse>(message);

                // Детальная проверка ответа
                if (response == null || !response.success || response.request == null)
                {
                    Debug.LogError("Неверный формат ответа сервера");
                    return;
                }

                if (response.request.id <= 0)
                {
                    Debug.LogError($"Невалидный ID: {response.request.id}");
                    return;
                }

                CreateContractUIElement(response.request);
                Debug.Log($"Заявка создана. ID: {response.request.id}");
                
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка обработки: {e.Message}");
            }
        });
    }

    private void CreateContractUIElement(RequestData requestData)
    {
        // Проверяем данные на валидность
        if (requestData == null || requestData.id <= 0)
        {
            Debug.LogError($"Некорректные данные заявки: {(requestData == null ? "null" : $"ID={requestData.id}")}");
            return;
        }

        // Проверяем обязательные поля
        if (string.IsNullOrEmpty(requestData.title))
        {
            requestData.title = "Без названия";
        }

        if (string.IsNullOrEmpty(requestData.content))
        {
            requestData.content = "Содержание отсутствует";
        }

        // Создаем экземпляр префаба
        GameObject newContract = Instantiate(contractPrefab, contractsContainer);
        ContractEditor editor = newContract.GetComponent<ContractEditor>();

        if (editor == null)
        {
            Debug.LogError("Не найден компонент ContractEditor на префабе");
            Destroy(newContract);
            return;
        }

        // Инициализируем с данными
        editor.InitializeWithData(requestData);
    }


}