using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpeakerController : MonoBehaviour
{
    [SerializeField] private List<AudioNamePair> voiceLines;
    [SerializeReference] private AudioSource speaker;

    public static SpeakerController speakerController;

    private void Start()
    {
        speakerController = this;
        DontDestroyOnLoad(this);
    }

    [Serializable]
    private struct AudioNamePair
    {
        public string name;
        public AudioClip clip;
    }

    public IEnumerator PlayAudio(string clipName)
    {
        // If the first speaker is playing audio then wait until it has finished
        yield return new WaitWhile(() => speaker.isPlaying);

        foreach (var audioNamePair in voiceLines.Where(audioNamePair => clipName == audioNamePair.name))
        {
            speaker.PlayOneShot(audioNamePair.clip);
            yield break;
        }
    }
}
