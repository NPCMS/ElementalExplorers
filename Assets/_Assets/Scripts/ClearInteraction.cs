using TMPro;
using UnityEngine;

public class ClearInteraction : UIInteraction
{
    public TMP_InputField lobbyCodeInput;

    public override void Interact()
    {
        lobbyCodeInput.text = "";
        Debug.Log(gameObject.GetComponentInChildren<TMP_Text>().text);
    }
}
