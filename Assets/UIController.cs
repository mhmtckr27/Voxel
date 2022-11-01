using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [SerializeField] public GameObject cursor;
    [SerializeField] private GameObject loadingBar;
    [SerializeField] public GameObject menu;
    [SerializeField] private GameObject menuButtons;
    [SerializeField] private TMP_InputField saveFileName;
    [SerializeField] private GameObject saveList;
    [SerializeField] private GameObject saveListContent;
    [SerializeField] private GameObject saveListEntryPrefab;
    [SerializeField] private Button saveConfirmButton;
    [SerializeField] private Button loadConfirmButton;
    [SerializeField] private GameObject saveParent;
    [SerializeField] private GameObject loadParent;
    [SerializeField] private List<Image> blockBorders;
    
    List<string> saveFiles;

    public static bool IsGamePaused = false;

    public List<BlockType> blockDatas;

    public BlockType selectedBlock;

    public void OnContinueButton()
    {
        OpenMenu();
    }

    public void OnNewGameButton()
    {
        OpenMenu();
        loadingBar.SetActive(true);
        FindObjectOfType<World>().loadFromFile = false;
        FindObjectOfType<World>().GenerateWorld();
    }

    public void OnSaveGameButton()
    {
        saveConfirmButton.interactable = false;
        ResetSaveListEntryButtonColors();
        saveFileName.text = "";
        saveFiles = WorldSaver.GetAllSaveFiles();
        for (int i = 0; i < saveListContent.transform.childCount; i++)
        {
            Transform saveListEntry = saveListContent.transform.GetChild(i);
            if (i < saveFiles.Count)
            {
                saveListEntry.GetComponentInChildren<TMP_Text>(true).text = saveFiles[i];
                saveListEntry.gameObject.SetActive(true);
            }
            else
            {
                saveListEntry.gameObject.SetActive(false);
            }
        }
        
        saveFileName.gameObject.SetActive(true);
        saveList.SetActive(true);
        saveParent.SetActive(true);
        loadParent.SetActive(false);
        menuButtons.SetActive(false);
    }

    public void OnLoadGameButton()
    {
        ResetSaveListEntryButtonColors();
        saveFileName.text = "";
        saveFiles = WorldSaver.GetAllSaveFiles();
        for (int i = 0; i < saveListContent.transform.childCount; i++)
        {
            Transform saveListEntry = saveListContent.transform.GetChild(i);
            if (i < saveFiles.Count)
            {
                saveListEntry.GetComponentInChildren<TMP_Text>(true).text = saveFiles[i];
                saveListEntry.gameObject.SetActive(true);
            }
            else
            {
                saveListEntry.gameObject.SetActive(false);
            }
        }
        
        saveList.SetActive(true);
        loadParent.SetActive(true);
        loadingBar.SetActive(true);
        menuButtons.SetActive(false);
    }

    public void OnQuitButton()
    {
        Application.Quit();
    }

    public void OnSaveConfirmButton()
    {
        WorldSaver.Save(FindObjectOfType<World>(), saveFileName.text);
        OnCancelSaveButton();
    }
    
    public void OnLoadConfirmButton()
    {        
        OnCancelLoadButton();
        OpenMenu();
        loadingBar.SetActive(true);
        FindObjectOfType<World>().loadFromFile = true;
        FindObjectOfType<World>().GenerateWorld(saveFileName.text);
    }

    public void OnCancelSaveButton()
    {
        saveList.SetActive(false);
        saveParent.SetActive(false);
        menuButtons.SetActive(true);
    }

    public void OnCancelLoadButton()
    {
        saveList.SetActive(false);
        loadParent.SetActive(false);
        menuButtons.SetActive(true);
    }

    public void OnSaveListEntryButton(Image clickedButtonBG, string saveFileName)
    {
        ResetSaveListEntryButtonColors();
        clickedButtonBG.color = Color.green;
        this.saveFileName.text = saveFileName;
    }

    private void ResetSaveListEntryButtonColors()
    {
        for (int i = 0; i < saveListContent.transform.childCount; i++)
        {
            saveListContent.transform.GetChild(i).GetComponent<Image>().color = Color.white;
        }
    }

    public void OnSaveFileNameTextChanged()
    {
        saveConfirmButton.interactable = !string.IsNullOrEmpty(saveFileName.text);
        loadConfirmButton.interactable = !string.IsNullOrEmpty(saveFileName.text);
    }

    public void OpenMenu()
    {
        menuButtons.SetActive(!menuButtons.activeSelf);
        menu.SetActive(!menu.activeSelf);
        Cursor.visible = menu.activeSelf;
        Cursor.lockState = menu.activeSelf ? CursorLockMode.None : CursorLockMode.Locked;
        IsGamePaused = menu.activeSelf;
    }

    public void SelectBlock(int selectedBlock)
    {
        foreach (Image blockBorder in blockBorders)
        {
            blockBorder.color = Color.black;
        }
        
        blockBorders[selectedBlock - 1].color = Color.white;
        this.selectedBlock = blockDatas[selectedBlock - 1];
    }
}
