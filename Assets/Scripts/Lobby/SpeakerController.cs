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
            music.Play();
        }
        else
            StartCoroutine(SwapTracks(raceMusic));

    }

    public void PlaySpaceshipMusic()
    {
        if (music.isPlaying != raceMusic && music.isPlaying != minigameMusic && music.isPlaying != spaceshipMusic)
        {
            music.clip = spaceshipMusic;
            music.Play();
        }
        else
            StartCoroutine(SwapTracks(spaceshipMusic));
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

        music.clip = clip;
        music.Play();
        time = 0;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            music.volume = Mathf.Lerp(music.volume, 1, time / fadeDuration);
            yield return null;
        }
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

    public IEnumerator PlayAudioNow(string clipName)
    {
        trackQueue.Clear();
        if (speaker.isPlaying) speaker.Stop();
        foreach (var audioNamePair in voiceLines.Where(audioNamePair => clipName == audioNamePair.name))
        {
            speaker.PlayOneShot(audioNamePair.clip);
            music.volume = dimmerMultiplier;
            dimmed = true;
            yield break;
        }
    }

    private void Update()
    {
        if (dimmed && !speaker.isPlaying)
        {
            music.volume = 1;
            dimmed = false;
        }
        if (speaker.isPlaying || trackQueue.Count <= 0) return;

        var clipName = trackQueue.Dequeue();
        foreach (var audioNamePair in voiceLines.Where(audioNamePair => clipName == audioNamePair.name))
        {
            speaker.PlayOneShot(audioNamePair.clip);
            music.volume = dimmerMultiplier;
            dimmed = true;
            return;
        }
    }
}
