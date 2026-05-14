using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class TakeKey : MonoBehaviour
{
    public GameObject keyNotification;
    public GameObject objectToHide;
    public bool hideAfterTake = true;
    public float notificationDuration = 2f;

    public void TakeKeyMethod()
    {
        if (QuestManager.Instance != null && QuestManager.Instance.hasKey)
        {
            return;
        }

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.hasKey = true;
        }

        ClearUiSelection();

        GameObject notification = keyNotification != null ? keyNotification : FindKeyNotification();
        if (notification != null)
        {
            notification.SetActive(true);
            StartCoroutine(HideNotificationAfterDelay(notification));
        }

        if (hideAfterTake)
        {
            GameObject target = objectToHide != null ? objectToHide : gameObject;
            target.SetActive(false);
        }

        Debug.Log("Key taken");
    }

    private void ClearUiSelection()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private GameObject FindKeyNotification()
    {
        if (PracticeManager.Instance == null || PracticeManager.Instance.screenContainer == null)
        {
            return null;
        }

        Transform found = PracticeManager.Instance.screenContainer.Find("KeyNotification");
        return found != null ? found.gameObject : null;
    }

    private IEnumerator HideNotificationAfterDelay(GameObject notification)
    {
        if (notificationDuration <= 0f)
        {
            yield break;
        }

        yield return new WaitForSeconds(notificationDuration);

        if (notification != null)
        {
            notification.SetActive(false);
        }
    }
}
