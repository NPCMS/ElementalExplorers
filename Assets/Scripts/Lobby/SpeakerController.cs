using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class SpeakerController : MonoBehaviour
{
    [SerializeField] private List<AudioNamePair> voiceLines;
    [SerializeReference] private AudioSource speaker;
    
    [Header("Music")]
    [SerializeReference] private AudioSource music;
    [SerializeField] private AudioClip raceMusic;
    [SerializeField] private AudioClip minigameMusic;
    [SerializeField] private float fadeDuration;
    [SerializeField] [Range(0f, 1f)] private float maxVolume;

    public static SpeakerController speakerController;

    private Queue<String> trackQueue = new();

    private void Start()
    {
        speakerController = this;
        DontDestroyOnLoad(this);
        music.loop = true;
    }

    public void PlayRaceMusic()
    {
        // if nothing is playing
        if (music.isPlaying != raceMusic && music.isPlaying != minigameMusic)
        {
            music.clip = raceMusic;
            music.Play();
        }
        else
            StartCoroutine(SwapTracks(raceMusic));

    }

    public void PlayMinigameMusic()
    {
        StartCoroutine(SwapTracks(minigameMusic));
    }

    private IEnumerator SwapTracks(AudioClip clip)
    {
        float time = 0;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            music.volume = Mathf.Lerp(music.volume, 0, time / fadeDuration);
            yield return null;
        }

        music.clip = minigameMusic;
        music.Play();
        time = 0;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            music.volume = Mathf.Lerp(music.volume, maxVolume, time / fadeDuration);
            yield return null;
        }
        yield break;
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
        foreach (var audioNamePair in voiceLines.Where(audioNamePair => clipName == audioNamePair.name))
        {
            speaker.PlayOneShot(audioNamePair.clip);
            return;
        }
    }
}
