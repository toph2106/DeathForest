using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class NPCDialogue : MonoBehaviour
{
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public GameObject interactHint; // Chữ "Press F"

    [TextArea(3, 10)]
    public string[] lines; // Danh sách các câu thoại
    private int index;
    private bool isPlayerInRange;
    private bool isTalking;

    void Update()
    {
        // Kiểm tra nếu người chơi nhấn F khi đang ở gần
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.F) && !isTalking)
        {
            StartDialogue();
        }

        // Chuyển câu thoại bằng chuột trái
        if (isTalking && Input.GetMouseButtonDown(0))
        {
            NextLine();
        }
    }

    void StartDialogue()
    {
        isTalking = true;
        index = 0;
        dialoguePanel.SetActive(true);
        interactHint.SetActive(false);
        dialogueText.text = lines[index];
    }

    void NextLine()
    {
        if (index < lines.Length - 1)
        {
            index++;
            dialogueText.text = lines[index];
        }
        else
        {
            // Kết thúc hội thoại
            dialoguePanel.SetActive(false);
            isTalking = false;
            if (isPlayerInRange) interactHint.SetActive(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            interactHint.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            interactHint.SetActive(false);
            dialoguePanel.SetActive(false);
            isTalking = false;
        }
    }
}