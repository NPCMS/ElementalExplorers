using System;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class Keyboard : MonoBehaviour
{
    [SerializeField] private TMP_Text lobbyCodeInput;

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < gameObject.transform.childCount; i++) {
            GameObject child = gameObject.transform.GetChild(i).gameObject;
            
            if (child.name == "AlphaNum")
            {
                for (int j = 0; j < child.transform.childCount; j++)
                {
                    GameObject letter = child.transform.GetChild(j).gameObject;
                    UIInteraction interaction = letter.AddComponent<UIInteraction>();
                    interaction.AddCallback(() =>
                    {
                        lobbyCodeInput.text += letter.GetComponentInChildren<TMP_Text>().text;
                    });
                }
            } else if (child.name == "Enter")
            {
                GameObject key = child.transform.gameObject;
                UIInteraction interaction = key.AddComponent<UIInteraction>();
                interaction.AddCallback(() =>
                {
                    //Debug.Log("Enter");
                });
            }
            else if (child.name == "Clear")
            {
                GameObject key = child.transform.gameObject;
                UIInteraction interaction = key.AddComponent<UIInteraction>();
                interaction.AddCallback(() =>
                {
                    lobbyCodeInput.text = "";
                });
            }
            else if (child.name == "Backspace")
            {
                GameObject key = child.transform.gameObject;
                key.AddComponent<UIInteraction>();
                UIInteraction interaction = key.GetComponent<UIInteraction>();
                interaction.AddCallback(() => {
                    if (lobbyCodeInput.text.Length > 0)
                    {
                        lobbyCodeInput.text = lobbyCodeInput.text.Substring(0, lobbyCodeInput.text.Length - 1);
                    }
                });
            }
        }
    }
}
