using UnityEngine;

/// <summary>
/// Central audio routing with optional clips. The prototype has no audio
/// assets yet, so every call is intentionally safe when a clip is null.
/// </summary>
public class AudioManager : MonoBehaviour
{
    [Header("Optional Clips")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip ballWallClip;
    [SerializeField] private AudioClip ballBlockClip;
    [SerializeField] private AudioClip blockDestroyClip;
    [SerializeField] private AudioClip bonusBallClip;
    [SerializeField] private AudioClip feverStartClip;
    [SerializeField] private AudioClip gameOverClip;

    [Header("Mix")]
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.25f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.7f;
    [SerializeField, Range(1, 8)] private int maxSfxPerFrame = 4;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private int lastSfxFrame = -1;
    private int sfxPlayedThisFrame;

    public void Initialize()
    {
        AudioSource[] sources = GetComponents<AudioSource>();
        musicSource = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
        sfxSource = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.clip = backgroundMusic;

        if (backgroundMusic != null && !musicSource.isPlaying)
            musicSource.Play();

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.volume = 1f;
    }

    public void PlayBallWall()
    {
        PlaySfx(ballWallClip);
    }

    public void PlayBallBlock()
    {
        PlaySfx(ballBlockClip);
    }

    public void PlayBlockDestroy()
    {
        PlaySfx(blockDestroyClip);
    }

    public void PlayBonusBall()
    {
        PlaySfx(bonusBallClip);
    }

    public void PlayFeverStart()
    {
        PlaySfx(feverStartClip);
    }

    public void PlayGameOver()
    {
        PlaySfx(gameOverClip);
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null || sfxSource == null || sfxVolume <= 0f)
            return;

        if (lastSfxFrame != Time.frameCount)
        {
            lastSfxFrame = Time.frameCount;
            sfxPlayedThisFrame = 0;
        }

        if (sfxPlayedThisFrame >= maxSfxPerFrame)
            return;

        sfxPlayedThisFrame++;
        float voiceVolume = sfxVolume / Mathf.Sqrt(maxSfxPerFrame);
        sfxSource.PlayOneShot(clip, voiceVolume);
    }
}
