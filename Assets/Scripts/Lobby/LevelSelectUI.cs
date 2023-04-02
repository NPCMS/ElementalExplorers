using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class LevelSelectUI : MonoBehaviour
{
    [SerializeField] private TMP_Text citySelected;
    [SerializeField] private GameObject leaveLobbyBtn;
    [SerializeField] private GameObject MainMenu;
    private CityOnHover previousSelection;
    void Awake()
    {
        citySelected.text = "";
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            GameObject child = gameObject.transform.GetChild(i).gameObject;

            if (child.name == "Cities")
            {
                for (int j = 0; j < child.transform.childCount; j++)
                {
                    GameObject city = child.transform.GetChild(j).gameObject;
                    CityOnHover hover = city.GetComponent<CityOnHover>();
                    UIInteraction interaction = city.AddComponent<UIInteraction>();
                    interaction.AddCallback(() =>
                    {
                        citySelected.text = city.name;
                        // hover.OnSelection();
                        // if(previousSelection != null)
                        //     previousSelection.OnDeselection();
                        // previousSelection = hover;
                    });
                    interaction.AddOnEnterCallback(() =>
                    {
                        hover.OnHoverStart();
                        Debug.Log("callback for start hover");
                    });
                    interaction.AddOnLeaveCallback(() =>
                    {
                        hover.OnHoverEnd();
                        Debug.Log("callback for end hover");
                    });
                    
                }
            }
            
            // else if(child.name == "ConfirmBtn")
            // {
            //     child.SetActive(false);
            // }
        }

        // UIInteraction leaveInteraction = leaveLobbyBtn.GetComponent<UIInteraction>();
        // leaveInteraction.AddCallback(() =>
        // {
        //     citySelected.text = "";
        //     MainMenu.SetActive(true);
        //     this.enabled = false;
        // });
    }
}
