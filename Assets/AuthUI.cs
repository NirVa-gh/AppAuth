
using System;
using TMPro;
using UIWidgets;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UI;

public class AuthUI : MonoBehaviour
{
    [Header("Register")]
    public TMP_InputField regUserName;
    public TMP_InputField regUserPassword;
    public TMP_InputField regUserEmail;
    public Button registerButton;

    [Header("Login")]
    public TMP_InputField loginUserName;
    public TMP_InputField loginUserPassword;
    public Button loginButton;

    [Header("Message")]
    public TMP_Text messageRegPanel;
    public TMP_Text messageLoginPanel;

    [Header("Panels")]
    [SerializeField] private GameObject LoginPanel;
    [SerializeField] private GameObject CreateContractPanel;
    [SerializeField] private GameObject ViewContractPanel;
    [SerializeField] private GameObject RegisterPanel;
    private UIWidget _UIWidgetLoginPanel;
    private UIWidget _UIWidgetCreateContractPanel;
    private UIWidget _VievContractPanel;
    private UIWidget _UIRegisterPanel;

    [Header("Buttons")]
    [SerializeField] private Button logoutButton;

    [Header("Partner")]
    public bool isPartnerLocal;


    private void Start()
    {
        registerButton.onClick.AddListener(OnRegisterClicked);
        loginButton.onClick.AddListener(OnLoginClicked);
        logoutButton.onClick.AddListener(OnLogoutClicked);
        _VievContractPanel = ViewContractPanel.GetComponent<UIWidget>();
        _UIWidgetLoginPanel = LoginPanel.GetComponent<UIWidget>();
        _UIWidgetCreateContractPanel = CreateContractPanel.GetComponent<UIWidget>();
        _UIRegisterPanel = RegisterPanel.GetComponent<UIWidget>(); 
    }

    private void OnLogoutClicked()
    {
        _UIWidgetLoginPanel.Show();
        _VievContractPanel.Close();
        _UIWidgetCreateContractPanel.Close();
        ShowMessage("Logout");
    }

    private void OnLoginClicked()
    {
        if (string.IsNullOrEmpty(loginUserName.text) &&
            string.IsNullOrEmpty(loginUserPassword.text))
        {
            Debug.Log("Username & Password are requared");
            return;
        }
        AuthManager.Instance.Login(
              loginUserName.text,
              loginUserPassword.text,
              (success, message, isPartner) =>    
              {
                  ShowMessage(message);
                  if (success)
                  {
                      
                      isPartnerLocal = isPartner;
                      _UIWidgetLoginPanel.Close();
                      _UIWidgetCreateContractPanel.Show();
                  }

                  
                  if(isPartnerLocal)
                  {
                      _UIWidgetLoginPanel.Close();
                      _VievContractPanel.Show();
                      _UIWidgetCreateContractPanel.Close();
                  }
                  else
                  {

                      _UIWidgetLoginPanel.Close();
                      _UIWidgetCreateContractPanel.Show();
                      _VievContractPanel.Close();
                  }
              }
              );  
    }

    private void OnRegisterClicked()
    {
        if (string.IsNullOrEmpty(regUserName.text) && 
            string.IsNullOrEmpty(regUserPassword.text) && 
            string.IsNullOrEmpty(regUserEmail.text))
        {
            Debug.Log("Username & Password are requared");
            return;
        }

        AuthManager.Instance.Register(
              regUserName.text,
              regUserPassword.text,
              regUserEmail.text,
              (success, message) =>                          
              {
                  ShowMessage(message);
                  if (success)
                  {
                      _UIRegisterPanel.Close();
                      _UIWidgetLoginPanel.Show();
                      Debug.Log(message);
                  }
              }
            );
    }
    private void ShowMessage(string messageLocal)
    {
        messageLoginPanel.text = messageLocal;
        messageRegPanel.text = messageLocal;
    }
}
