using System;
using UnityEngine;

// 노래(AudioSource)의 재생 위치를 기준으로 박자를 센다
// 프레임 시간이 아니라 오디오 샘플 위치를 쓰므로 프레임 드랍, 시점 이동, 속도 변경에도 노래와 어긋나지 않는다
[RequireComponent(typeof(AudioSource))]
public class Metronome : MonoBehaviour
{
    #region Singleton
    public static Metronome Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }
    #endregion

    // 곡을 고르면 SetSong이 채운다
    private float bpm = 60f;
    // 노래 시작부터 첫 박(0번 박)까지의 시간(초)
    private float firstBeatOffset;

    private AudioSource audioSource;
    private int lastBeat = -1;

    // 새 박에 진입할 때마다 그 박의 번호(0부터)로 호출된다
    public event Action<int> OnBeat;

    public float SecondsPerBeat => 60f / bpm;
    public bool IsPlaying => audioSource.isPlaying;

    // 노래 재생 위치(초)
    public float SongTime => audioSource.clip == null ? 0f : (float)audioSource.timeSamples / audioSource.clip.frequency;
    // 현재 박 위치. 소수부는 박 사이의 진행도
    public float BeatPosition => (SongTime - firstBeatOffset) / SecondsPerBeat;
    public int CurrentBeat => Mathf.FloorToInt(BeatPosition);

    // 재생 속도. 노래와 박자가 함께 빨라지거나 느려진다
    public float Speed
    {
        get => audioSource.pitch;
        set => audioSource.pitch = Mathf.Max(0.01f, value);
    }

    // 박 수 -> 실제 경과 시간(초). Command의 박 단위 대기에 쓴다
    public static float Beat2Second(float beat) => beat * Instance.SecondsPerBeat / Instance.Speed;

    public void SetSong(Song song)
    {
        Stop();
        audioSource.clip = song.music;
        bpm = song.bpm;
        firstBeatOffset = song.firstBeatOffset;

        if (song.music == null)
            Debug.LogError($"[Metronome] Song '{song.name}' has no music.");
        if (bpm <= 0f)
            Debug.LogError($"[Metronome] Song '{song.name}' has invalid bpm {bpm}.");
    }

    public void Play()
    {
        lastBeat = CurrentBeat - 1;
        audioSource.Play();
    }

    public void Pause() => audioSource.Pause();

    public void Stop()
    {
        audioSource.Stop();
        lastBeat = -1;
    }

    public void SeekToTime(float seconds)
    {
        if (audioSource.clip == null) return;

        int sample = Mathf.RoundToInt(seconds * audioSource.clip.frequency);
        audioSource.timeSamples = Mathf.Clamp(sample, 0, audioSource.clip.samples - 1);
        // 이동한 지점의 박을 다음 Update에서 바로 발생시킨다
        lastBeat = CurrentBeat - 1;
    }

    public void SeekToBeat(int beat)
    {
        SeekToTime(firstBeatOffset + beat * SecondsPerBeat);
        // 샘플 반올림으로 박 직전에 놓여도 이전 박이 다시 발생하지 않게 한다
        lastBeat = beat - 1;
    }

    private void Update()
    {
        if (!audioSource.isPlaying) return;

        int currentBeat = CurrentBeat;
        // 프레임이 길어져 여러 박을 건너뛰었어도 빠짐없이 순서대로 발생시킨다
        while (lastBeat < currentBeat)
        {
            lastBeat++;
            if (lastBeat >= 0)
                OnBeat?.Invoke(lastBeat);
        }
    }
}
