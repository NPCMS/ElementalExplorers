using UnityEngine;

public class PlayerMinigamePointer : MonoBehaviour
{
    private SteamInputCore.SteamInput steamInput;
    [SerializeField] private SteamInputCore.Hand hand; 

    private void Start()
    {
        steamInput = SteamInputCore.GetInput();
    }

    private void Update()
    {
        if (!steamInput.GetInputDown(hand, SteamInputCore.Button.Trigger)) return;
        if (!Physics.Raycast(transform.position, transform.forward, out RaycastHit hit)) return;
        if (hit.transform.gameObject.TryGetComponent<TargetScript>(out var target))
        {
            target.TriggerTarget();
            steamInput.Vibrate(hand, 0.1f, 120, 0.6f);
        }
    }
}
