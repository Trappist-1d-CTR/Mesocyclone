using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    #region Variables

    #region Menu Navigation Values
    public int MenuPosition;
    #endregion

    #region Panels
    public List<GameObject> MenuPanels;
    #endregion

    #endregion

    void Start()
    {
        SelectPanel(0);
    }

    public void ResetPanels()
    {
        for (int i = 0; i < MenuPanels.Count; i++)
        {
            MenuPanels[i].SetActive(false);
        }
    }

    public void Panel()
    {
        ResetPanels();
        if (!MenuPanels[MenuPosition].activeSelf)
        {
            MenuPanels[MenuPosition].SetActive(true);
        }
    }

    public void SelectPanel(int ID)
    {
        MenuPosition = ID;
        Panel();
    }
}
