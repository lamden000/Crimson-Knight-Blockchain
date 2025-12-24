using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private DialogFactory dialogFactory;
    public DialogFactory DialogFactory => dialogFactory;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ShowYesNo(string message, int idDialog, System.Action<bool, int> callback)
    {
        var dialog = dialogFactory.CreateYesNo();
        dialog.Setup(message, idDialog, callback);
        dialog.Show();
    }


    public void ShowDropdown(string title, string[] options, System.Action<int> callback)
    {
        var dialog = dialogFactory.CreateDropdown();
        dialog.Setup(title, options, callback);
        dialog.Show();
    }
    public void ShowOK(string message, System.Action onOkClicked = null)
    {
        var dialog = dialogFactory.CreateOK();
        dialog.Setup(message, onOkClicked);
        dialog.Show();
    }

}
