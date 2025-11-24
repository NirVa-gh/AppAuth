using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

public class ContractsLoader : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private GameObject contractPrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField] private Button refreshButtonID;
    [SerializeField] private Button createButton;

    private void Start()
    {
        //refreshButton.onClick.AddListener(LoadUserContracts);
        createButton.onClick.AddListener(() => {LoadByUserIDContracts(AuthManager.Instance.GetUserId());});
        refreshButtonID.onClick.AddListener(() => { LoadByUserIDContracts(AuthManager.Instance.GetUserId()); });
    }

    public void LoadByUserIDContracts(int targetUserID)
    {
        StartCoroutine(LoadUserIDRequestsCoroutine(targetUserID));
    }

    private IEnumerator LoadUserIDRequestsCoroutine(int targetUserID)
    {
        string url = $"{AuthManager.Instance.baseURL}/api/requestsByUserID?user_id = {targetUserID}";
        string authToken = PlayerPrefs.GetString("auth_token");
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.SetRequestHeader("Authorization", $"Bearer {authToken}");
            www.downloadHandler = new DownloadHandlerBuffer();

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                ProcessContractsResponse(www.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"Failed to load contracts: {www.error} (Status: {www.responseCode})");
            }
        }
    }

    public void LoadUserContracts()
    {
        if (!AuthManager.Instance.IsAuthenticated)
        {
            Debug.LogWarning("User not authenticated");
            return;
        }

        StartCoroutine(LoadAllRequestsCoroutine());
    }

    private IEnumerator LoadAllRequestsCoroutine()
    {
        string url = $"{AuthManager.Instance.baseURL}/api/requests";
        string authToken = PlayerPrefs.GetString("auth_token");

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.SetRequestHeader("Authorization", $"Bearer {authToken}");
            www.downloadHandler = new DownloadHandlerBuffer();

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                ProcessContractsResponse(www.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"Failed to load contracts: {www.error} (Status: {www.responseCode})");
            }
        }
    }

    private void ProcessContractsResponse(string jsonResponse)
    {
        try
        {
            // Clear existing contracts
            foreach (Transform child in contentParent)
            {
                Destroy(child.gameObject);
            }

            // Parse response
            var response = JsonUtility.FromJson<RequestsListResponse>(jsonResponse);

            if (response != null && response.success && response.requests != null)
            {
                foreach (RequestData contract in response.requests)
                {
                    CreateContractUIItem(contract);
                }
            }
            else
            {
                Debug.LogError("Invalid contracts response format");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error processing contracts: {e.Message}");
        }
    }

    private void CreateContractUIItem(RequestData contract)
    {
        if (contractPrefab == null || contentParent == null)
        {
            Debug.LogError("Contract prefab or content parent not assigned");
            return;
        }

        // Создаем новый экземпляр префаба с активными компонентами
        GameObject contractItem = Instantiate(contractPrefab, contentParent);
        contractItem.SetActive(true); // Активируем весь объект

        // Получаем и активируем все компоненты
        MonoBehaviour[] components = contractItem.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var component in components)
        {
            if (component != null)
            {
                component.enabled = true;
            }
        }

        ContractEditor editor = contractItem.GetComponent<ContractEditor>();

        if (editor != null)
        {
            editor.enabled = true; // Явно включаем компонент
            editor.InitializeWithData(contract);
        }
        else
        {
            Debug.LogWarning("Contract prefab doesn't have ContractEditor component");
        }
    }

    [System.Serializable]
    private class RequestsListResponse
    {
        public bool success;
        public string message;
        public List<RequestData> requests;
    }
}