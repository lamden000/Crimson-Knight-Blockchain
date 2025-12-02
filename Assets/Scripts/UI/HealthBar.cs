using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Image fillImage; // kéo ?nh fill vào ?ây

    public Transform target;

    void LateUpdate()
    {
        if (target.localScale.x < 0)
            transform.localScale = new Vector3(-1, 1, 1);
        else
            transform.localScale = new Vector3(1, 1, 1);
    }


    public void SetHealth(int current, int max)
    {
        float amount = (float)current / max;
        fillImage.fillAmount = amount;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }
}
