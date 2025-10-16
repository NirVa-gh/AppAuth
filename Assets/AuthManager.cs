using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AuthManager : MonoBehaviour
{
    private string serverURL = "http://localhost:5000/api";
    public string baseURL = "http://127.0.0.1:5000";

    public bool IsInitialized { get; private set; }
    public static event Action<bool> OnAuthStateChanged;
    public bool IsAuthenticated => !string.IsNullOrEmpty(PlayerPrefs.GetString("auth_token"));

    public static AuthManager Instance { get; private set; }

    private void Awake()
    {
        IsInitialized = false;
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        IsInitialized = true;


    }
    private void NotifyAuthStateChanged(bool isAuthenticated)
    {
        OnAuthStateChanged?.Invoke(isAuthenticated);
    }
    public int GetUserId()
    {
        return PlayerPrefs.GetInt("user_id", 0);
    }
    public void Register(string username, string email, string password, Action<bool, string> callback)
    {
        StartCoroutine(RegisterCoroutine(username, email, password, callback));
    }
    public void Login(string username, string password, Action<bool, string, bool> callback)
    {
        StartCoroutine(LoginCoroutine(username, password, callback));
    }
    public void CreateRequest(RequestData request, Action<bool, string> callback)
    {
        StartCoroutine(CreateRequestCoroutine(request, callback));
    }
    public void DeleteRequest(int requestId, Action<bool, string> callback)
    {
        StartCoroutine(DeleteRequestCoroutine(requestId, callback));
    }
    public void DeleteRequestAdmin(int requestId, Action<bool, string> callback)
    {
        StartCoroutine(DeleteRequestAdminCoroutine(requestId, callback));
    }

    private IEnumerator DeleteRequestAdminCoroutine(int requestId, Action<bool, string> callback)
    {
        string url = $"{baseURL}/api/requestsAdmin/{requestId}";
        string authToken = PlayerPrefs.GetString("auth_token");
        using (UnityWebRequest www = UnityWebRequest.Delete(url))
        {
            www.SetRequestHeader("Authorization", $"Bearer {authToken}");
            www.downloadHandler = new DownloadHandlerBuffer();
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                callback(true, "Заявка удалена");
            }
            else
            {
                callback(false, $"Ошибка: {www.error}");
            }
        }

    }

    public void GetRequest(int requestId, Action<bool, RequestData> callback)
    {
        StartCoroutine(GetRequestCoroutine(requestId, callback));
    }
    public void UpdateRequest(RequestData request, Action<bool, string> callback)
    {
        StartCoroutine(UpdateRequestCoroutine(request, callback));
    }
    public void GetAllRequests(Action<bool, List<RequestData>> callback)
    {
        StartCoroutine(GetAllRequestsCoroutine(callback));
    }
    private string ParseErrorMessage(UnityWebRequest www)
    {
        if (www.responseCode == 400)
        {
            try
            {
                var errorResponse = JsonUtility.FromJson<ErrorResponse>(www.downloadHandler.text);
                return errorResponse?.message ?? "Неверные данные в запросе";
            }
            catch
            {
                return "Неверный формат запроса (400)";
            }
        }
        return www.error;
    }
    public void RegisterAdmin(string username, string email, string password, Action<bool, string> callback)
    {
        StartCoroutine(RegisterAdminCoroutine(username, email, password, callback));
    }

    private IEnumerator RegisterAdminCoroutine(string username, string email, string password, Action<bool, string> callback)
    {
        WWWForm form = new WWWForm();
        form.AddField("username", username);
        form.AddField("email", email);
        form.AddField("password", password);

        using (UnityWebRequest www = UnityWebRequest.Post(baseURL + "/api/register-admin", form))
        {
            www.SetRequestHeader("X-Admin-Secret", "SUPER_SECRET_KEY"); // Должен совпадать с серверным
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                callback(true, "Администратор создан");
            }
            else
            {
                callback(false, "Ошибка: " + www.error);
            }
        }
    }
    private IEnumerator RegisterCoroutine(string username, string email, string password, Action<bool, string> callback)
    {
        WWWForm form = new WWWForm();
        form.AddField("username", username);
        form.AddField("email", email);
        form.AddField("password", password);
        using (UnityWebRequest www = UnityWebRequest.Post(baseURL + "/api/register", form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                callback(true, "Connection success");
            }
            else
            {
                callback(false, "Connection error" + www.error);
            }
        }
    }
    private IEnumerator LoginCoroutine(string username, string password, Action<bool, string, bool> callback)
    {
        // Проверка входных данных
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            callback(false, "Username and password cannot be empty", false);
            yield break;
        }

        WWWForm form = new WWWForm();
        form.AddField("username", username);
        form.AddField("password", password);

        using (UnityWebRequest www = UnityWebRequest.Post(baseURL + "/api/login", form))
        {
            // Устанавливаем таймаут 10 секунд
            www.timeout = 10;

            yield return www.SendWebRequest();

            // Обработка сетевых ошибок
            if (www.result != UnityWebRequest.Result.Success)
            {
                string errorMessage = www.error;

                // Более информативные сообщения об ошибках
                if (www.responseCode == 401)
                    errorMessage = "Invalid username or password";
                else if (www.responseCode >= 500)
                    errorMessage = "Server error, please try later";

                callback(false, $"Login failed: {errorMessage}", false);
                yield break;
            }

            try
            {
                // Парсинг ответа
                var response = JsonUtility.FromJson<AuthResponse>(www.downloadHandler.text);

                if (response == null)
                {
                    callback(false, "Invalid server response", false);
                    yield break;
                }

                // Успешная авторизация
                if (response.success)
                {
                    Debug.Log($"Login successful. isPartner: {response.IsPartner}");
                    callback(response.success, response.message, response.IsPartner);

                    // Безопасное сохранение данных
                    try
                    {
                        PlayerPrefs.SetString("auth_token", response.token);
                        PlayerPrefs.SetInt("user_id", response.user_id);
                        PlayerPrefs.Save();
                        NotifyAuthStateChanged(true);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to save auth data: {e.Message}");
                    }
                }

                callback(response.success, response.message, response.IsPartner);
            }
            catch (Exception e)
            {
                Debug.LogError($"Login parsing error: {e.Message}");
                callback(false, "Failed to process login response", false);
            }
        }
    }
    private IEnumerator CreateRequestCoroutine(RequestData request, Action<bool, string> callback)
    {
        // Явно создаём объект для сериализации
        RequestPayload requestPayload = new RequestPayload
        {
            title = string.IsNullOrEmpty(request.title) ? "New Request" : request.title,
            content = string.IsNullOrEmpty(request.content) ? "Empty content" : request.content,
            status = string.IsNullOrEmpty(request.status) ? "new" : request.status
        };

        string jsonData = JsonUtility.ToJson(requestPayload);
        Debug.Log($"Sending JSON: {jsonData}"); // Важно для отладки!

        using (UnityWebRequest www = new UnityWebRequest($"{baseURL}/api/requests", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString("auth_token")}");

            yield return www.SendWebRequest();

            Debug.Log($"Response: {www.downloadHandler.text}");

            if (www.result != UnityWebRequest.Result.Success)
            {
                callback(false, $"HTTP {www.responseCode}: {www.downloadHandler.text}");
                yield break;
            }

            callback(true, www.downloadHandler.text);
        }
    }
    private IEnumerator DeleteRequestCoroutine(int requestId, Action<bool, string> callback)
    {
        string url = $"{baseURL}/api/requests/{requestId}";
        string authToken = PlayerPrefs.GetString("auth_token");

        using (UnityWebRequest www = UnityWebRequest.Delete(url))
        {
            www.SetRequestHeader("Authorization", $"Bearer {authToken}");
            www.downloadHandler = new DownloadHandlerBuffer();

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                callback(true, "Заявка удалена");
            }
            else
            {
                callback(false, $"Ошибка: {www.error}");
            }
        }
    }
    private IEnumerator GetRequestCoroutine(int requestId, Action<bool, RequestData> callback)
    {
        string url = $"{baseURL}/api/requests/{requestId}";
        string authToken = PlayerPrefs.GetString("auth_token");

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.SetRequestHeader("Authorization", $"Bearer {authToken}");
            www.downloadHandler = new DownloadHandlerBuffer();

            yield return www.SendWebRequest();

            string jsonResponse = www.downloadHandler.text;
            //Debug.Log($"Полученный JSON: {jsonResponse}");

            // Проверка на пустой ответ
            if (string.IsNullOrEmpty(jsonResponse))
            {
                Debug.LogError("JSON-ответ пустой!");
                callback(false, null);
                yield break;
            }

            //Debug.Log($"Raw JSON: {www.downloadHandler.text}"); 

            if (www.result == UnityWebRequest.Result.Success)
            {
                RequestResponse response = JsonUtility.FromJson<RequestResponse>(jsonResponse);
                if (response == null)
                {
                    Debug.LogError("Не удалось распарсить JSON");
                    callback(false, null);
                    yield break;
                }
                if (response != null && response.success && response.request != null)
                {
                    callback(true, response.request);
                }
                else
                {
                    Debug.LogError("Неверная структура ответа");
                    callback(false, null);
                }
            }
            else
            {
                Debug.LogError($"HTTP ошибка: {www.error}");
                callback(false, null);
            }
        }
    }
    private IEnumerator UpdateRequestCoroutine(RequestData request, Action<bool, string> callback)
    {
        string url = $"{baseURL}/api/requests/{request.id}";

        var requestData = new UpdateRequestDto { title = request.title, content = request.content };
        string jsonData = JsonUtility.ToJson(requestData);


        Debug.Log($"Sending JSON: {jsonData}"); // Это важно для отладки

        using (UnityWebRequest www = new UnityWebRequest(url, "PUT"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString("auth_token")}");

            yield return www.SendWebRequest();

            //Debug.Log($"Response: {www.downloadHandler.text}"); // Добавьте это
            //Debug.Log($"Status: {www.responseCode}"); // И это

            if (www.result != UnityWebRequest.Result.Success)
            {
                callback(false, www.downloadHandler.text); // Возвращаем текст ошибки от сервера
                yield break;
            }

            callback(true, "Заявка успешно обновлена");
        }
    }
    private IEnumerator GetAllRequestsCoroutine(Action<bool, List<RequestData>> callback)
    {
        string url = $"{baseURL}/api/requests";
        string authToken = PlayerPrefs.GetString("auth_token"); //??

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
          //www.SetRequestHeader("Authorization", $"Bearer {authToken}");
            www.downloadHandler = new DownloadHandlerBuffer();

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<RequestsListResponse>(www.downloadHandler.text);
                callback(response.success, response.requests);
            }
            else
            {
                Debug.LogError($"Ошибка: {www.error}");
                callback(false, null);
            }
        }
    }


    [Serializable]
    private class ErrorResponse
    {
        public string message;
    }
}

[Serializable]
public class RequestsListResponse
{
    public bool success;
    public List<RequestData> requests;
}
[Serializable]
public class AllRequestsResponse
{
    public bool success;
    public List<RequestData> requests;
}
[Serializable]
public class AuthResponse
{
    public int user_id;
    public bool success;
    public string token;
    public string message;
    public int is_partner; 
    public bool IsPartner => is_partner == 1;
}
[Serializable]
public class RequestData
{
    public int id;
    public string title;
    public string content;
    public string status;
    public string created_at;
}
[Serializable]
public class RequestResponse
{
    public bool success;
    public string message;
    public RequestData request;
}

[Serializable]
public class UpdateRequestDto
{
    public string title;
    public string content;
}
[Serializable]
public class RequestPayload
{
    public string title;
    public string content;
    public string status;
}
