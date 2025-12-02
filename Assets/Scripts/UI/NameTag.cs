using TMPro;
using UnityEngine;

public class NameTag : MonoBehaviour
{
    public TMP_Text nameText;
    public Transform target;

    void LateUpdate()
    {
        if (target.localScale.x < 0)
            transform.localScale = new Vector3(-1, 1, 1);
        else
            transform.localScale = new Vector3(1, 1, 1);
    }

    public void SetName(string username)
    {
        if (nameText == null)
        {
            Debug.LogError("[NameTag.SetName] nameText IS NULL !!!!!");
            return;
        }

        nameText.SetText(username);

        Debug.Log("[NameTag.SetName] Setting name to: " + nameText.text);
    }

}
