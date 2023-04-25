using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LevelSelectUI : MonoBehaviour
{
    [SerializeField] private TMP_Text citySelected;
    [SerializeField] private GameObject leaveLobbyBtn;
    [SerializeField] private GameObject MainMenu;
    private CityOnHover previousSelection;

    //private string[] tiles = { "16146_10903", "16146_10904", "16147_10903", "16147_10904" };
    private Dictionary<string, string[]> tiles = new Dictionary<string, string[]>()
    {
        { "Bristol", new string[] { "16146_10903.rfm", "16146_10904.rfm", "16147_10903.rfm", "16147_10904.rfm"} }
    };
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
                    interaction.AddCallback((RaycastHit hit, SteamInputCore.Button button) =>
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
                    });
                    interaction.AddOnLeaveCallback(() =>
                    {
                        hover.OnHoverEnd();
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

    public string[] getTiles()
    {
        if (tiles.ContainsKey(citySelected.text))
        {
            Debug.LogError("city is not precomputed");
            return null;
        }
        else
            return tiles[citySelected.text];
    }
}
