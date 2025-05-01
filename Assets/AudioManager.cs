using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    [Header("Music")]
    public AudioClip[] backgroundMusic; // Assign 3 music tracks in inspector
    public AudioSource musicSource;
    private int lastMusicIndex = -1;

    [Header("SFX")]
    public AudioClip attackSFX;
    public AudioClip dashSFX;
    public AudioClip hitSFX;
    public AudioClip defendSFX_Knight;
    public AudioClip defendSFX_Samurai;
    public AudioSource sfxSource;

    void Start()
    {
        PlayRandomMusic();
    }

    void PlayRandomMusic()
    {
        if (backgroundMusic == null || backgroundMusic.Length == 0 || musicSource == null)
            return;
        int newIndex;
        do {
            newIndex = Random.Range(0, backgroundMusic.Length);
        } while (backgroundMusic.Length > 1 && newIndex == lastMusicIndex);
        lastMusicIndex = newIndex;
        musicSource.clip = backgroundMusic[newIndex];
        musicSource.Play();
        Invoke(nameof(PlayRandomMusic), backgroundMusic[newIndex].length);
    }

    // SFX Methods
    public void PlayAttackSFX() {
        PlaySFX(attackSFX);
    }
    public void PlayDashSFX() {
        PlaySFX(dashSFX);
    }
    public void PlayHitSFX() {
        PlaySFX(hitSFX);
    }
    public void PlayDefendSFX(string characterType) {
        if (characterType == "Knight")
            PlaySFX(defendSFX_Knight);
        else if (characterType == "Samurai")
            PlaySFX(defendSFX_Samurai);
    }
    private void PlaySFX(AudioClip clip) {
        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip);
    }
}
