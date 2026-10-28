using UnityEngine;

public class GameManager : MonoBehaviour
{// Metronome 접근해 적절한 박자, PromptManager 접근해 적절한 메서드로 CommandManager 호출, InputManager의 Input 받아 시간 조작
    [SerializeField] private Metronome metronome;
    [SerializeField] private DivisionManager divisionManager;
    [SerializeField] private SummonManager summonManager;
    [SerializeField] private PromptManager promptManager;
    [SerializeField] private CommandManager commandManager;
    [SerializeField] private InputManager inputManager;
    [SerializeField] private UIManager uiManager;
    // 시작 화면에서 고를 수 있는 곡들
    [SerializeField] private Song[] songs;

    // 고른 곡. 고르기 전에는 null
    public Song CurrentSong { get; private set; }
    // 현재 박. Metronome이 박마다 갱신한다
    public int CurrentBeat { get; private set; } = -1;

    void Start()
    {
        metronome.OnBeat += OnBeat;
        inputManager.OnLyricSelected += MoveToBeat;
        inputManager.OnSongSelected += SelectSong;

        // 곡을 고르면 SelectSong이 그 곡의 프롬프트와 카드를 읽는다
        uiManager.ShowSongSelection(songs);
    }

    private void SelectSong(int index)
    {
        if (CurrentSong != null || index < 0 || index >= songs.Length) return;

        CurrentSong = songs[index];
        inputManager.OnSongSelected -= SelectSong;
        uiManager.HideSongSelection();

        promptManager.Load(CurrentSong.prompt);
        uiManager.ShowLyrics();
        metronome.SetSong(CurrentSong);

        // 카드 조각과 Child가 모두 준비된 뒤에 노래를 시작한다
        divisionManager.OnDivided += TryPlay;
        summonManager.OnSummoned += TryPlay;
        divisionManager.Divide(CurrentSong.name, promptManager.GetUsedCardNums());
        TryPlay();
    }

    private void TryPlay()
    {
        if (metronome.IsPlaying || !divisionManager.IsDivided || !summonManager.IsSummoned) return;

        divisionManager.OnDivided -= TryPlay;
        summonManager.OnSummoned -= TryPlay;
        metronome.Play();
    }

    private void OnDestroy()
    {
        if (divisionManager != null)
            divisionManager.OnDivided -= TryPlay;
        if (summonManager != null)
            summonManager.OnSummoned -= TryPlay;
        if (metronome != null)
            metronome.OnBeat -= OnBeat;
        if (inputManager != null)
        {
            inputManager.OnLyricSelected -= MoveToBeat;
            inputManager.OnSongSelected -= SelectSong;
        }
    }

    private void OnBeat(int beat)
    {
        CurrentBeat = beat;
        uiManager.SetCurrentBeat(beat);
        OnScene(beat);
    }

    // 시점 이동. 이동한 박의 OnScene이 다음 프레임에 호출된다
    public void MoveToBeat(int beat)
    {
        // 노래를 시작하기 전(카드 조각이나 Child가 준비되지 않았을 때)에는 이동하지 않는다
        if (!divisionManager.IsDivided || !summonManager.IsSummoned) return;

        // 진행 중인 Command를 멈추고, 이동할 박 직전까지의 Prompt를 바로 적용해 카드를 그 시점의 모습으로 맞춘다
        // 그러지 않으면 () 줄로 이동했을 때 이동 전의 카드가 남는다
        commandManager.StopCommands();
        for (int b = 0; b < beat; b++)
        {
            Command command = promptManager.ReadPrompt(b);
            if (command != null)
                commandManager.FinishChildren(command);
        }

        // 노래가 끝나 멈췄으면 다시 재생한다. Play가 lastBeat를 바꾸므로 Seek보다 먼저 부른다
        if (!metronome.IsPlaying)
            metronome.Play();
        metronome.SeekToBeat(beat);
        uiManager.SetCurrentBeat(beat);
    }
    public void MoveToTime(float seconds) => metronome.SeekToTime(seconds);

    private void OnScene(int sceneNum)
    {
        Command command = promptManager.ReadPrompt(sceneNum);
        // 해당 박에 하드코딩된 Prompt가 없으면 아무것도 하지 않는다
        if (command == null) return;

        commandManager.CommandChildren(command);
    }
}
