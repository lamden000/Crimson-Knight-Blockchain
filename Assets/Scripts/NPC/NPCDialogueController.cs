using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


[RequireComponent(typeof(NPC))]
public class NPCDialogueController : MonoBehaviour
{
    [Header("Dialogue Data")]
    public string currentQuest;
    public Sprite defaultPotrait;

    private GameObject dialoguePanel;
    private TextMeshProUGUI dialogueText;
    private Image portraitImage;
    private Button questButton;
    private Button closeButton;
    private Button menuButton;
    private TMP_Text npcNameText;

    [Header("World-Space Chat Bubble")]
    public GameObject chatBubblePrefab;      
    public float bubbleDuration = 3f;    
    public float bubbleScalePerChar = 0.02f;
    public Vector2 bubbleOffset=new Vector2(0,100);

    private AudioSource audioSource;

    private DialogueEntry currentEntry;
    private int currentLineIndex;
    private bool isTyping;
    private bool skipTyping;
    private bool isDialogueActive;
    private Canvas worldCanvas;
    private NPCDialogue dialogueData;

    private void Start()
    {     
        audioSource = GetComponent<AudioSource>();
        worldCanvas= GetComponentInChildren<Canvas>();
        dialoguePanel = CharacterDialogManager.Instance.dialoguePanel;
        dialogueText = CharacterDialogManager.Instance.dialogueText;
        portraitImage = CharacterDialogManager.Instance.portraitImage;
        questButton = CharacterDialogManager.Instance.questButton;
        closeButton = CharacterDialogManager.Instance.closeButton;
        menuButton=CharacterDialogManager.Instance.menuButton;
        npcNameText = CharacterDialogManager.Instance.npcNameText;
    }

    public void StartDialogue()
    {
        if (isDialogueActive||dialogueData == null) return;

        if (currentQuest.Length == 0)
            questButton.gameObject.SetActive(false);
        if (dialogueData.npcMenu == null)
            menuButton.gameObject.SetActive(false);
        else {
            menuButton.onClick.AddListener(OpenMenu);
            menuButton.gameObject.GetComponentInChildren<TMP_Text>().text = dialogueData.npcMenu.menuName;
        }
        dialoguePanel.SetActive(true);
        questButton.onClick.AddListener(OnQuestButton);
        closeButton.onClick.AddListener(CloseDialogue);
        npcNameText.text = dialogueData.npcName;
        portraitImage.sprite = dialogueData.npcPotrait;

        isDialogueActive = true;
        if ( GetDialogueEntry("Idle")==null)
            OpenMenu();
        else
            PlayDialogue("Idle");
    }

    public void LoadDialogueData(NPCName npcName)
    {
        int npcIndex = (int)npcName;
        string path = $"NPCs/Dialogue/{npcIndex}/NPC_{npcIndex}_Dialog";
        dialogueData = Resources.Load<NPCDialogue>(path);
        if(dialogueData == null)
        {
            Debug.LogWarning($"Dialogue data not found for {npcName} at Resources/{path}");
            return;
        }
        var sprite = Resources.Load<Sprite>($"NPCs/Avatar/{npcIndex}")?? Resources.Load<Sprite>($"NPCs/Avatar/DefaultPotrait");
        dialogueData.npcPotrait = sprite;
    }

    private void PlayDialogue(string key)
    {
        var entry = GetDialogueEntry(key);
        if (entry == null)
        {
            Debug.LogWarning($"Dialogue key not found: {key}");
            return;
        }

        currentEntry = entry;
        currentLineIndex = 0;

        StopAllCoroutines();
        StartCoroutine(PlayDialogueCoroutine());
    }

    private IEnumerator PlayDialogueCoroutine()
    {
        while (currentLineIndex < currentEntry.lines.Count)
        {
            yield return StartCoroutine(TypeLine(currentEntry.lines[currentLineIndex],
                GetAudioClip(currentLineIndex)));

            bool auto = (dialogueData.autoProgressLines.Length > currentLineIndex)
                && dialogueData.autoProgressLines[currentLineIndex];

            if (auto)
                yield return new WaitForSeconds(dialogueData.autoProgressDelay);
            else
            {
                yield return new WaitUntil(() =>
                    Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame ||
                    Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame
                );
            }

            currentLineIndex++;
        }

        questButton.gameObject.SetActive(true);
        closeButton.gameObject.SetActive(true);
    }

    private IEnumerator TypeLine(string line, AudioClip clip)
    {
        isTyping = true;
        skipTyping = false;

        dialogueText.text = "";

        if (clip != null)
        {
            audioSource.pitch = dialogueData.voicePitch;
            audioSource.clip = clip;
            audioSource.Play();
        }

        foreach (char c in line)
        {
            if (skipTyping) break;

            dialogueText.text += c;
            yield return new WaitForSeconds(dialogueData.typingSpeed);
        }

        dialogueText.text = line;
        isTyping = false;
    }

    private AudioClip GetAudioClip(int lineIndex)
    {
        if (currentEntry.audioClips != null && lineIndex < currentEntry.audioClips.Length)
            return currentEntry.audioClips[lineIndex];
        return null;
    }

    private DialogueEntry GetDialogueEntry(string key)
    {
        var field = typeof(NPCDialogue).GetField("dialogueDict",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dict = (Dictionary<string, DialogueEntry>)field.GetValue(dialogueData);

        if (dict == null)
        {
            // Build dictionary manually if needed
            var listField = typeof(NPCDialogue).GetField("dialogueList",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var list = (List<DialogueEntry>)listField.GetValue(dialogueData);
            dict = new Dictionary<string, DialogueEntry>();
            foreach (var entry in list)
                dict[entry.key] = entry;

            field.SetValue(dialogueData, dict);
        }

        dict.TryGetValue(key, out var entryFound);
        return entryFound;
    }

    private void OnQuestButton()
    {
        questButton.gameObject.SetActive(false);
        closeButton.gameObject.SetActive(false);
        PlayDialogue(currentQuest);
    }

    private void CloseDialogue()
    {
        StopAllCoroutines();
        dialogueText.text = "";
        dialoguePanel.SetActive(false);
        closeButton.onClick.RemoveAllListeners();
        questButton.onClick.RemoveAllListeners();
        menuButton.onClick.RemoveAllListeners();
        isDialogueActive = false;
    }

    public void SkipTyping()
    {
        if (isTyping)
            skipTyping = true;
    }

    // ========= Ambient talk: world-space chat bubble =========
    public void SpeakBubble(string key)
    {
        var entry = GetDialogueEntry(key);
        if (entry == null || entry.lines.Count == 0)
            return;

        string line = entry.lines[Random.Range(0, entry.lines.Count)];
        AudioClip clip = entry.audioClips != null && entry.audioClips.Length > 0 ?
            entry.audioClips[Random.Range(0, entry.audioClips.Length)] : null;

        if (clip != null)
        {
            audioSource.pitch = dialogueData.voicePitch;
            audioSource.clip = clip;
            audioSource.Play();
        }
        if (worldCanvas == null)
        {
            Debug.LogWarning("No world-space canvas found.");
            return;
        }

        Vector3 position = transform.position+new Vector3(bubbleOffset.x,bubbleOffset.y);
        GameObject bubble = Instantiate(chatBubblePrefab,position, Quaternion.identity, worldCanvas.gameObject.transform);
        var text = bubble.GetComponentInChildren<TextMeshProUGUI>();

        text.text = line;
        Destroy(bubble, bubbleDuration);
    }

    private void OpenMenu()
    {
        CloseDialogue();
        CharacterDialogManager.Instance.OpenMenuForNPC(dialogueData);
    }

}
