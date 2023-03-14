using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SpeakerController : MonoBehaviour
{
    [SerializeField] private List<AudioNamePair> voiceLines;
    [SerializeReference] private List<AudioSource> speakers;
    
    [Serializable]
    private struct AudioNamePair
    {
        public string name;
        public AudioClip clip;
    }

    public bool PlayAudio(string clipName)
    {
        foreach (AudioNamePair audioNamePair in voiceLines)
        {
            if (clipName == audioNamePair.name)
            {
                foreach (AudioSource speaker in speakers)
                {
                    speaker.PlayOneShot(audioNamePair.clip);
                }
                return true;
            }
        }

        return false;
    }
}
