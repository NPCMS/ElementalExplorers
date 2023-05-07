using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;

public class ThudSoundEffect : MonoBehaviour
{
    [SerializeField] private float speedThreshold = 10;
    [SerializeField] private float maxVolumeSpeed = 20;
    [SerializeField] private float minVolume = 0.1f;
    [SerializeField] private float maxVolume = 0.5f;
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;
    [SerializeField] private AudioSource source;
    [SerializeField] private int terrainLayerMask;
    [SerializeField] private AudioClip terrain;
    [SerializeField] private AudioClip other;

    private void OnCollisionEnter(Collision collision)
    {
        float speed = collision.relativeVelocity.magnitude;
        if (speed > speedThreshold)
        {
            float t = Mathf.InverseLerp(speedThreshold, maxVolumeSpeed, speed);
            float volume = Mathf.Lerp(minVolume, maxVolumeSpeed, t);
            AudioClip clip = collision.collider.gameObject.layer == terrainLayerMask ? terrain : other;
            source.pitch = Random.Range(minPitch, maxPitch);
            source.PlayOneShot(clip, volume);
        }
    }
}
