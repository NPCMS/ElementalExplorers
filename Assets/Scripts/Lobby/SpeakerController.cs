using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public IEnumerator PlayAudio(string clipName)
    {
        // If the first speaker is playing audio then wait until it has finished
        yield return new WaitWhile(() => speakers[0].isPlaying);
        
        foreach (AudioNamePair audioNamePair in voiceLines)
        {
            if (clipName == audioNamePair.name)
            {
                foreach (AudioSource speaker in speakers)
                {
                    speaker.PlayOneShot(audioNamePair.clip);
                }
            }
        }
    }
}
