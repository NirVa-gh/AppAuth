using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;

public class ViewAcceptedContracts : MonoBehaviour
{
    [SerializeField] private GameObject contractPrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button listContractButton;

    private void Start()
    {
        refreshButton.onClick.AddListener(LoadAllAcceptedContracts);
        LoadAllAcceptedContracts();
    }

    public void LoadAllAcceptedContracts()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        AuthManager.Instance.GetAllAcceptedRequests((success, requests) =>
        {
            if (success && requests != null)
            {
                foreach (var request in requests)
                {
                    var contract = Instantiate(contractPrefab, contentParent);
                    contract.GetComponent<ContractView>().Initialize(request);
                }
            }
            else
            {
                Debug.LogError("Не удалось загрузить заявки");
            }
        });
    }
}
