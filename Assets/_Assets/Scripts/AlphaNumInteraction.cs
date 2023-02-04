using TMPro;
using UnityEngine;

public class AlphaNumInteraction : UIInteraction
{
    public TMP_Text lobbyCodeInput;

    public override void Interact()
    {
        lobbyCodeInput.text += gameObject.GetComponentInChildren<TMP_Text>().text;
        Debug.Log(gameObject.GetComponentInChildren<TMP_Text>().text);
    }
}
