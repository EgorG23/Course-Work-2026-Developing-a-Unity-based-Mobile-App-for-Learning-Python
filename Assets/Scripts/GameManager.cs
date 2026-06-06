using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int currentLessonIndex = 0;

    public bool[] theoryCompleted = new bool[4];
    public bool[] practiceCompleted = new bool[4];

    public bool achievementTheEnd = false;
    public bool achievementFastAsWind = false;
    public bool achievementMoney = false;

    private float practiceStartTime;
    private bool isPracticeTimerRunning = false;

    public int coins = 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        GetOrCreate();
    }

    public static GameManager GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameManager existing = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            return existing;
        }

        GameObject gameManagerObject = new GameObject(nameof(GameManager));
        return gameManagerObject.AddComponent<GameManager>();
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadData(); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void StartPracticeTimer()
    {
        practiceStartTime = Time.time;
        isPracticeTimerRunning = true;
        Debug.Log("Таймер практики запущен!");
    }

    public void CompleteTheory()
    {
        if (currentLessonIndex >= 0 && currentLessonIndex < theoryCompleted.Length)
        {
            if (!theoryCompleted[currentLessonIndex])
            {
                theoryCompleted[currentLessonIndex] = true;
                AddCoins(5, $"Лекция №{currentLessonIndex + 1} пройдена впервые!");
                CheckAchievements();
            }
        }
    }

    public void CompletePractice(int lessonIndex)
    {
        if (lessonIndex < 0 || lessonIndex >= practiceCompleted.Length)
        {
            Debug.LogError($"Неверный индекс практики: {lessonIndex}");
            return;
        }

        currentLessonIndex = lessonIndex;
        if (practiceCompleted[lessonIndex])
        {
            Debug.LogWarning($"Практика {lessonIndex} уже была пройдена.");
            return;
        }

        practiceCompleted[lessonIndex] = true;
        AddCoins(10, $"Практика №{lessonIndex + 1} успешно решена!");

        if (lessonIndex == 0 && isPracticeTimerRunning)
        {
            float duration = Time.time - practiceStartTime;
            isPracticeTimerRunning = false;
            Debug.Log($"Практика 0 пройдена за {duration:F1} сек.");

            if (duration <= 180f)
            {
                achievementFastAsWind = true;
                Debug.Log("Достижение 'БЫСТРЕЕ ВЕТРА' разблокировано!");
            }
        }

        CheckAchievements();
    }
    public void CheckAchievements()
{
    bool allTheoriesCompleted = theoryCompleted[0] && theoryCompleted[1] && theoryCompleted[2] && theoryCompleted[3];
    bool requiredPracticesCompleted = practiceCompleted[0] && practiceCompleted[1] && practiceCompleted[2];

    if (allTheoriesCompleted && requiredPracticesCompleted && !achievementTheEnd)
    {
        achievementTheEnd = true;
        Debug.Log("Достижение 'THE END' разблокировано!");
    }

    if (coins >= 50 && !achievementMoney)
    {
        achievementMoney = true;
        Debug.Log("Достижение 'МАНИ' разблокировано!");
    }

    SaveData();
}

    private void AddCoins(int amount, string contextMessage)
    {
        coins += amount;
        SaveData();
        Debug.Log($"{contextMessage} Начислено: +{amount} монет.  Текущий баланс: {coins}");
    }

    private void SaveData()
    {
        PlayerPrefs.SetInt("PlayerCoins", coins);
        PlayerPrefs.SetInt("Ach_TheEnd", achievementTheEnd ? 1 : 0);
        PlayerPrefs.SetInt("Ach_FastAsWind", achievementFastAsWind ? 1 : 0);
        PlayerPrefs.SetInt("Ach_Money", achievementMoney ? 1 : 0);

        for (int i = 0; i < theoryCompleted.Length; i++)
        {
            PlayerPrefs.SetInt("Theory_" + i, theoryCompleted[i] ? 1 : 0);
            PlayerPrefs.SetInt("Practice_" + i, practiceCompleted[i] ? 1 : 0);
        }
        PlayerPrefs.Save();
    }

    private void LoadData()
    {
        coins = PlayerPrefs.GetInt("PlayerCoins", 0);
        achievementTheEnd = PlayerPrefs.GetInt("Ach_TheEnd", 0) == 1;
        achievementFastAsWind = PlayerPrefs.GetInt("Ach_FastAsWind", 0) == 1;
        achievementMoney = PlayerPrefs.GetInt("Ach_Money", 0) == 1;

        for (int i = 0; i < theoryCompleted.Length; i++)
        {
            theoryCompleted[i] = PlayerPrefs.GetInt("Theory_" + i, 0) == 1;
            practiceCompleted[i] = PlayerPrefs.GetInt("Practice_" + i, 0) == 1;
        }
    }
}
