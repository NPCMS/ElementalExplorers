using TMPro;
using UnityEngine;

public class EnterInteraction : UIInteraction
{
    public TMP_InputField lobbyCodeInput;

    public override void Interact()
    {
        Debug.Log(gameObject.GetComponentInChildren<TMP_Text>().text);
    }
}
