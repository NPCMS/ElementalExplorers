using UnityEngine;
using UnityEngine.Rendering;

public class CheckpointController : MonoBehaviour
{
    [SerializeField] public int checkpoint;
    [SerializeField] private Shader shader;
    [SerializeField] private AudioSource successSound;
    public RaceController raceController;
    public bool passed;
    // reference to shader property

    private static readonly int H = Shader.PropertyToID("_Y_height");
    private static readonly int HoloHeight = Shader.PropertyToID("_Hologrm_Height");

    public void Start()
    {
        float yOffset = gameObject.transform.position.y - gameObject.GetComponent<MeshCollider>().bounds.size[0];
        Material instanceOfCheckpointShader = new Material(shader);
        instanceOfCheckpointShader.SetFloat(H, yOffset);
        instanceOfCheckpointShader.SetFloat(HoloHeight, gameObject.transform.localScale.y * 2);
        var render = gameObject.GetComponent<MeshRenderer>();
        render.material = instanceOfCheckpointShader;
        render.shadowCastingMode = ShadowCastingMode.Off;
    }

    public void PassCheckpoint()
    {
        if (passed) return;
        if (raceController != null)
        {
            successSound.Play();
            raceController.PassCheckpoint(checkpoint);
        }
        else
        {
            Debug.LogWarning("Checkpoint not connected to race controller");
            gameObject.SetActive(false);
        }
    }
}
