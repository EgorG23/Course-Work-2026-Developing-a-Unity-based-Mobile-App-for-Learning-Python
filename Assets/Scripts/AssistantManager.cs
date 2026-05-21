using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;

public class BypassCertificate : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData) => true;
}

public class AssistantManager : MonoBehaviour
{
    public static string BeforeAssistantScene = "LessonsList"; 
    public static string SavedTopic = "Введение";
    public static string SavedSectionType = "Теория";
    public static string SavedTaskDescription = "Изучение базовых понятий Python.";

    [Header("Кнопка закрытия чата")]
    public Button closeButton; 

    [Header("Элементы чата")]
    public TMP_InputField userInputField; 
    public TextMeshProUGUI chatHistoryText; 
    public Button sendButton; 
    public ScrollRect chatScrollRect; 

    [Header("Настройки ИИ API")]
    [SerializeField] private string apiKey = "КЛЮЧ_SK_OR_V1";
    [SerializeField] private string apiUrl = "https://openrouter.ai/api/v1/chat/completions";
    [SerializeField] private string modelName = "openrouter/free"; 

    void Start()
    {
        if (chatHistoryText == null)
        {
            Debug.LogError("Критическая ошибка: Поле 'Chat History Text' не привязано в Инспекторе на объекте Assistant_Canvas!");
            return;
        }

        UpdateWelcomeMessage();
        
        if (closeButton != null) closeButton.onClick.AddListener(GoBackToGameScene);
        if (sendButton != null) sendButton.onClick.AddListener(OnSendButtonClicked);

        StartCoroutine(ScrollToBottom());
    }

    public void UpdateWelcomeMessage()
    {
        string topic = string.IsNullOrEmpty(SavedTopic) ? "Изучение Python" : SavedTopic;
        string section = string.IsNullOrEmpty(SavedSectionType) ? "Теория" : SavedSectionType;

        if (chatHistoryText != null)
        {
            chatHistoryText.text = $"<b>Ворона:</b> Кар! Привет! Я твой ИИ-наставник. Помогу тебе разобраться с темой <b>{topic}</b> ({section}). В чём твой вопрос?\n";
        }
    }

    void GoBackToGameScene()
    {
        string sceneToLoad = string.IsNullOrEmpty(BeforeAssistantScene) ? "LessonsList" : BeforeAssistantScene;
        SceneManager.LoadScene(sceneToLoad);
    }

    void OnSendButtonClicked()
    {
        if (userInputField == null || string.IsNullOrWhiteSpace(userInputField.text)) return;
        if (chatHistoryText == null) return;

        string userMessage = userInputField.text;
        chatHistoryText.text += $"\n<b>Вы:</b> {userMessage}\n";
        userInputField.text = "";

        chatHistoryText.text += "<color=#888888><b>Ворона:</b> <i>Думаю... (Кар)...</i></color>\n";
        
        StartCoroutine(ScrollToBottom());
        StartCoroutine(SendRequestToAI(userMessage));
    }

    private IEnumerator SendRequestToAI(string userMessage)
    {
        string topic = string.IsNullOrEmpty(SavedTopic) ? "Изучение Python" : SavedTopic;
        string section = string.IsNullOrEmpty(SavedSectionType) ? "Теория" : SavedSectionType;
        string description = string.IsNullOrEmpty(SavedTaskDescription) ? "Базовый контекст задачи" : SavedTaskDescription;

        string systemPrompt = $"Ты — мудрая ворона-наставник по имени Кар. Твоя цель — помогать студенту изучать Python.\n" +
                              $"НИКОГДА не пиши готовый код решения задачи за студента.\n" +
                              $"Сейчас студент находится в разделе '{section}' по теме '{topic}'.\n" +
                              $"Конкетст окружения/задачи: {description}\n" +
                              $"Отвечай коротко, на русском языке, иногда вставляй 'Кар!'.";

        ChatRequest requestData = new ChatRequest();
        requestData.model = modelName;
        requestData.temperature = 0.7f;
        requestData.messages = new ChatMessage[]
        {
            new ChatMessage { role = "system", content = systemPrompt },
            new ChatMessage { role = "user", content = userMessage }
        };

        string jsonPayload = JsonUtility.ToJson(requestData);
        byte[] rawData = System.Text.Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(rawData);
            request.downloadHandler = new DownloadHandlerBuffer();
            
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey.Trim());
            request.SetRequestHeader("HTTP-Referer", "https://unity.local"); 
            request.SetRequestHeader("X-Title", "CourseWork Unity AI"); 

            request.certificateHandler = new BypassCertificate();

            yield return request.SendWebRequest();

            if (chatHistoryText != null)
            {
                string thinkingText = "<color=#888888><b>Ворона:</b> <i>Думаю... (Кар)...</i></color>\n";
                if (chatHistoryText.text.EndsWith(thinkingText))
                {
                    chatHistoryText.text = chatHistoryText.text.Substring(0, chatHistoryText.text.Length - thinkingText.Length);
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = request.downloadHandler.text;
                    ChatResponse responseData = JsonUtility.FromJson<ChatResponse>(jsonResponse);

                    if (responseData != null && responseData.choices != null && responseData.choices.Length > 0)
                    {
                        string aiReply = responseData.choices[0].message.content;
                        chatHistoryText.text += $"\n<b>Ворона:</b> {aiReply}\n";
                    }
                }
                else
                {
                    chatHistoryText.text += $"\n<b>Ворона:</b> Кар! Ошибка связи: {request.error}\n";
                }
            }

            StartCoroutine(ScrollToBottom());
        }
    }

    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        if (chatHistoryText != null) chatHistoryText.ForceMeshUpdate();

        if (chatScrollRect != null && chatScrollRect.content != null)
        {
            chatScrollRect.velocity = Vector2.zero;
            LayoutRebuilder.ForceRebuildLayoutImmediate(chatScrollRect.content);
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    [System.Serializable] public class ChatMessage { public string role; public string content; }
    [System.Serializable] public class ChatRequest { public string model; public ChatMessage[] messages; public float temperature; }
    [System.Serializable] public class ChatResponse { public Choice[] choices; }
    [System.Serializable] public class Choice { public ChatMessage message; }
}