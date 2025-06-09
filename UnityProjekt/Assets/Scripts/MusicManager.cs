using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Audio Settings")]
    public List<AudioClip> musicClips;
    public AudioSource audioSource;

    [Range(0f, 1f)]
    public float volume = 1f;
    public bool isMuted = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.volume = volume;
    }

    private void Start()
    {
        PlayRandomTrack();
    }

    private void Update()
    {
        if (!audioSource.isPlaying && musicClips.Count > 0)
        {
            PlayRandomTrack();
        }

        audioSource.volume = isMuted ? 0f : volume;
    }

    public void PlayRandomTrack()
    {
        if (musicClips.Count == 0)
            return;

        int index = Random.Range(0, musicClips.Count);
        audioSource.clip = musicClips[index];
        audioSource.Play();
    }

    public void SetVolume(float value)
    {
        volume = Mathf.Clamp01(value);
    }

    public void ToggleMute(bool mute)
    {
        isMuted = mute;
    }
}
