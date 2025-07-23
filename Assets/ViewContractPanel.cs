using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class ViewContractPanel : MonoBehaviour
{
    [SerializeField] private GameObject contractPrefab; 
    [SerializeField] private Transform contentParent;
    [SerializeField] private Button refreshButton;

    private void Start()
    {
        refreshButton.onClick.AddListener(LoadAllContracts);
        LoadAllContracts();
    }

    public void LoadAllContracts()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        AuthManager.Instance.GetAllRequests((success, requests) =>
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