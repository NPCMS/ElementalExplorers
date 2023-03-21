using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LevelSelectUI : MonoBehaviour
{
    [SerializeField] private TMP_Text citySelected;
    [SerializeField] private GameObject leaveLobbyBtn;
    [SerializeField] private GameObject MainMenu;
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
                    UIInteraction interaction = city.AddComponent<UIInteraction>();
                    interaction.AddCallback(() => { citySelected.text = city.GetComponentInChildren<TMP_Text>().text; });
                }
            }
            
            else if(child.name == "ConfirmBtn")
            {
                child.SetActive(false);
            }
        }

        UIInteraction leaveInteraction = leaveLobbyBtn.GetComponent<UIInteraction>();
        leaveInteraction.AddCallback(() =>
        {
            citySelected.text = "";
            MainMenu.SetActive(true);
            this.enabled = false;
        });
    }
}
