using UnityEngine;

public class DialogFactory : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas mainCanvas;

    [Header("Prefabs")]
    [SerializeField] private DialogYesNo dialogYesNoPrefab;
    [SerializeField] private DialogOK dialogOKPrefab;
    [SerializeField] private DialogDropdown dialogDropdownPrefab;

    private void Awake()
    {
        if (mainCanvas == null)
            mainCanvas = FindAnyObjectByType<Canvas>();
    }

    public DialogYesNo CreateYesNo()
    {
        return Instantiate(dialogYesNoPrefab, mainCanvas.transform, false);
    }

    public DialogOK CreateOK()
    {
        return Instantiate(dialogOKPrefab, mainCanvas.transform, false);
    }

    public DialogDropdown CreateDropdown()
    {
        return Instantiate(dialogDropdownPrefab, mainCanvas.transform, false);
    }
}
