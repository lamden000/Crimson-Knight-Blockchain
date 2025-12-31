using UnityEngine;

public class DialogFactory : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas mainCanvas;

    [Header("Prefabs")]
    [SerializeField] private DialogYesNo dialogYesNoPrefab;
    [SerializeField] private DialogOK dialogOKPrefab;
    [SerializeField] private DialogDropdown dialogDropdownPrefab;
    [SerializeField] private WithdrawDialog withdrawDialogPrefab;
    [SerializeField] private SellItemDialog sellItemDialogPrefab;

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

    public WithdrawDialog CreateWithdrawDialog()
    {
        if (withdrawDialogPrefab == null)
        {
            Debug.LogError("[DialogFactory] WithdrawDialog prefab is not assigned!");
            return null;
        }
        return Instantiate(withdrawDialogPrefab, mainCanvas.transform, false);
    }

    public SellItemDialog CreateSellItemDialog()
    {
        if (sellItemDialogPrefab == null)
        {
            Debug.LogError("[DialogFactory] SellItemDialog prefab is not assigned!");
            return null;
        }
        return Instantiate(sellItemDialogPrefab, mainCanvas.transform, false);
    }
}
