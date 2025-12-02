using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class CharacterDialogManager : MonoBehaviour
{
    public static CharacterDialogManager Instance { get; private set; }

    [Header("Dialogue UI References")]
    public Canvas screenCanvas;
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public Image portraitImage;
    public Button questButton;
    public Button closeButton;
    public Button menuButton;
    public TMP_Text npcNameText;
    [SerializeField] private NPCMenuUI menuUI;
    private GridmapLoader mapLoader;
    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    private IEnumerator Start()
    {
        dialoguePanel.SetActive(true);
        yield return null;
        dialoguePanel.SetActive(false);
        menuUI.OnOptionSelected += HandleOption;
        mapLoader = FindAnyObjectByType<GridmapLoader>();
    }

    public void OpenMenuForNPC(NPCDialogue npc)
    {
        if (npc.npcMenu != null)
            menuUI.ShowMenu(npc.npcMenu);
    }

    private void HandleOption(string actionKey)
    {
        // actionKey ví dụ: "teleport_to_Forest", "open_shop_Blacksmith", "continue_dialogue"
        if (string.IsNullOrEmpty(actionKey))
            return;

        if (actionKey.StartsWith("teleport_to_"))
        {
            string destination = actionKey.Substring("teleport_to_".Length);
            TeleportPlayer(destination);
        }
        else if (actionKey.StartsWith("open_shop_"))
        {
            string shopName = actionKey.Substring("open_shop_".Length);
            OpenShop(shopName);
        }
        else if (actionKey == "continue_dialogue")
        {
            ContinueDialogue();
        }
        else
        {
            Debug.LogWarning($"Unknown menu action: {actionKey}");
        }

        menuUI.HideMenu();
    }


    private void TeleportPlayer(string destination)
    {
        if (string.IsNullOrEmpty(destination))
        {
            Debug.LogWarning("Teleport destination is missing!");
            return;
        }
        mapLoader.LoadMapByName(destination, "Default");

    }

    private void OpenShop(string shopName)
    {
        if (string.IsNullOrEmpty(shopName))
        {
            Debug.LogWarning("Shop name is missing!");
            return;
        }
    }

    void ContinueDialogue() { /* quay lại hệ thống thoại */ }
}
