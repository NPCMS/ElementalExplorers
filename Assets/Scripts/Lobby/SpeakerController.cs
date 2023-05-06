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

    private Queue<String> trackQueue = new();

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
        trackQueue.Enqueue(clipName);
        yield break;
    }

    private void Update()
    {
        if (speaker.isPlaying || trackQueue.Count <= 0) return;
        
        var clipName = trackQueue.Dequeue();
        Debug.Log("Speaker playing: " + speaker.isPlaying);
        foreach (var audioNamePair in voiceLines.Where(audioNamePair => clipName == audioNamePair.name))
        {
            speaker.PlayOneShot(audioNamePair.clip);
            return;
        }
    }
}
