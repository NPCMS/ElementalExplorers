using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class TeleportEffect : MonoBehaviour
{
    [SerializeField] private UniversalRendererData data;
    [SerializeField] private float disableTime = 2.0f;
    [SerializeField] private Material teleportMaterial;

    private void Start()
    {
        Disable();
    }

    private void OnEnable()
    {
        Enable();
    }

    private void OnDisable()
    {
        Disable();
    }

    public void Enable()
    {
        teleportMaterial.SetFloat("_StartTime", Time.time);
        data.rendererFeatures[^1].SetActive(true);
        StartCoroutine(DisableRoutine());
    }

    private IEnumerator DisableRoutine()
    {
        yield return new WaitForSeconds(disableTime);
        Disable();
    }

    public void Disable()
    {
        data.rendererFeatures[^1].SetActive(false);
    }
}