using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class SpeakerController : MonoBehaviour
{
    [SerializeField] private List<AudioNamePair> voiceLines;
    [SerializeReference] private AudioSource speaker;
    
    [Header("Music")]
    [SerializeReference] private AudioSource music;
    [SerializeField] private AudioClip raceMusic;
    [SerializeField] private AudioClip minigameMusic;
    [SerializeField] private AudioClip spaceshipMusic;
    [SerializeField] private float fadeDuration;
    [SerializeField] [Range(0f, 1f)] private float raceVolume;
    [SerializeField] [Range(0f, 1f)] private float minigameVolume;
    [SerializeField] [Range(0f, 1f)] private float spaceshipVolume;
    [SerializeField] [Range(0f, 1f)] private float dimmerMultiplier;

    public static SpeakerController speakerController;

    private Queue<String> trackQueue = new();

    private bool dimmed = false;

    private void Start()
    {
        speakerController = this;
        DontDestroyOnLoad(this);
        music.loop = true;
    }

    public void PlayRaceMusic()
    {
        // if nothing is playing
        if (music.isPlaying != raceMusic && music.isPlaying != minigameMusic && music.isPlaying != spaceshipMusic)
        {
            music.clip = raceMusic;
            music.volume = raceVolume;
            music.Play();
        }
        else
            StartCoroutine(SwapTracks(raceMusic, raceVolume));

    }

    public void PlaySpaceshipMusic()
    {
        if (music.isPlaying != raceMusic && music.isPlaying != minigameMusic && music.isPlaying != spaceshipMusic)
        {
            music.clip = spaceshipMusic;
            music.volume = spaceshipVolume;
            music.Play();
        }
        else
            StartCoroutine(SwapTracks(spaceshipMusic, spaceshipVolume));
    }

    public void PlayMinigameMusic()
    {
        StartCoroutine(SwapTracks(minigameMusic, minigameVolume));
    }

    private IEnumerator SwapTracks(AudioClip clip, float clipVolume)
    {
        float time = 0;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            music.volume = Mathf.Lerp(music.volume, 0, time / fadeDuration);
            yield return null;
        }

        music.clip = clip;
        music.Play();
        time = 0;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            music.volume = Mathf.Lerp(music.volume, clipVolume, time / fadeDuration);
            yield return null;
        }
    }

    [Serializable]
    private struct AudioNamePair
    {
        public string name;
        public AudioClip clip;
    }

    private IEnumerator PlayAudio(string clipName)
    {
        trackQueue.Enqueue(clipName);
        yield break;
    }

    private void Update()
    {
        if (dimmed && trackQueue.Count <= 0)
            music.volume /= dimmerMultiplier;
        if (speaker.isPlaying || trackQueue.Count <= 0) return;

        var clipName = trackQueue.Dequeue();
        foreach (var audioNamePair in voiceLines.Where(audioNamePair => clipName == audioNamePair.name))
        {
            speaker.PlayOneShot(audioNamePair.clip);
            music.volume *= dimmerMultiplier;
            dimmed = true;
            return;
        }
    }
}
