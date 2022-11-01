using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveListEntryButton : MonoBehaviour
{
    public void OnButtonClicked()
    {
        FindObjectOfType<UIController>(true).OnSaveListEntryButton(GetComponent<Image>(), GetComponentInChildren<TMP_Text>().text);
    }
}
