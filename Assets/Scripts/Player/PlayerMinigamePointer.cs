using UnityEngine;

public class PlayerMinigamePointer : MonoBehaviour
{
    private SteamInputCore.SteamInput steamInput;
    [SerializeField] private SteamInputCore.Hand hand;
    [SerializeField] private AudioClip hit1;
    [SerializeField] private AudioClip hit2;
    [SerializeField] private AudioSource targetAudio;

    private void Start()
    {
        steamInput = SteamInputCore.GetInput();
    }

    private void Update()
    {
        if (!steamInput.GetInputDown(hand, SteamInputCore.Button.Trigger)) return;
        if (!Physics.Raycast(transform.position, transform.forward, out RaycastHit hit)) return;
        TargetScript target = hit.transform.gameObject.GetComponentInParent<TargetScript>();
        if (target != null)
        {
            target.TriggerTarget();
            targetAudio.PlayOneShot(Random.Range(0,2) == 0 ? hit1 : hit2);
            steamInput.Vibrate(hand, 0.1f, 120, 0.6f);
        }
    }
}
