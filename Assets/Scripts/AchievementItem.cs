using UnityEngine;

public class AchievementItem : MonoBehaviour
{
    public enum AchievementType
    {
        TheEnd,
        FastAsWind,
        Money
    }

    public AchievementType achievementType; 
    public GameObject lockIcon;            

    void Start()
    {
        UpdateAchievementStatus();
    }

    public void UpdateAchievementStatus()
    {
        if (GameManager.Instance == null || lockIcon == null) return;

        bool isUnlocked = false;

        switch (achievementType)
        {
            case AchievementType.TheEnd:
                isUnlocked = GameManager.Instance.achievementTheEnd;
                break;
            case AchievementType.FastAsWind:
                isUnlocked = GameManager.Instance.achievementFastAsWind;
                break;
            case AchievementType.Money:
                isUnlocked = GameManager.Instance.achievementMoney;
                break;
        }

        lockIcon.SetActive(!isUnlocked);
    }
}