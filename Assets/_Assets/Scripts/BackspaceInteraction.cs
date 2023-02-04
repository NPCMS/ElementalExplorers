using TMPro;
using UnityEngine;

public class BackspaceInteraction : UIInteraction
{
    public TMP_Text lobbyCodeInput;

    public override void Interact()
    {
        if (lobbyCodeInput.text.Length > 0)
        {
            lobbyCodeInput.text = lobbyCodeInput.text.Substring(0, lobbyCodeInput.text.Length - 1);
            Debug.Log(gameObject.GetComponentInChildren<TMP_Text>().text);
        }
    }
}
