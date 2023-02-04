using TMPro;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class Keyboard : MonoBehaviour
{
    [SerializeField] private TMP_InputField lobbyCodeInput;

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
                    letter.AddComponent<AlphaNumInteraction>();
                    letter.GetComponent<AlphaNumInteraction>().lobbyCodeInput = lobbyCodeInput;
                }
            } else if (child.name == "Enter")
            {
                GameObject key = child.transform.gameObject;
                key.AddComponent<EnterInteraction>();
                key.GetComponent<EnterInteraction>().lobbyCodeInput = lobbyCodeInput;
            }
            else if (child.name == "Clear")
            {
                GameObject key = child.transform.gameObject;
                key.AddComponent<ClearInteraction>();
                key.GetComponent<ClearInteraction>().lobbyCodeInput = lobbyCodeInput;
            }
            else if (child.name == "Backspace")
            {
                GameObject key = child.transform.gameObject;
                key.AddComponent<BackspaceInteraction>();
                key.GetComponent<BackspaceInteraction>().lobbyCodeInput = lobbyCodeInput;
            }
        }
    }
}
