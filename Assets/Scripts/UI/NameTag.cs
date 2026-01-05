using TMPro;
using UnityEngine;

public class NameTag : MonoBehaviour
{
    public TMP_Text nameText;
    public Transform target;

    public void SetName(string username)
    {
        if (nameText == null)
        {
            Debug.LogError("[NameTag.SetName] nameText IS NULL !!!!!");
            return;
        }

        nameText.SetText(username);
    }

}
