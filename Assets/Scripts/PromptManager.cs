using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class PromptManager : MonoBehaviour
{
    // 한 줄이 차지하는 박 수. 1이면 n번째 줄이 n번째 박에 실행된다
    [SerializeField, Min(1)] private int beatsPerLine = 1;

    // (창문/눈) [흰 눈이 내린다] {격자로 점멸}
    // ( )와 [ ]는 비어 있을 수 있고, { }는 생략할 수 있다
    private static readonly Regex LinePattern = new Regex(
        @"^\s*\((?<cards>[^)]*)\)\s*\[(?<lyric>[^\]]*)\]\s*(?:\{(?<command>[^}]*)\})?\s*$");
    private static readonly Regex Whitespace = new Regex(@"\s+");

    // 줄 순서대로. 해석에 실패한 줄은 null로 자리만 차지한다
    private readonly List<Prompt> prompts = new List<Prompt>();

    public IReadOnlyList<Prompt> Prompts => prompts;

    // 한 줄마다 (cardNum) [가사] {Command}. GameManager가 고른 곡의 텍스트를 넘긴다
    public void Load(TextAsset file)
    {
        prompts.Clear();
        if (file == null)
        {
            Debug.LogError("[PromptManager] Prompt file is not assigned.");
            return;
        }

        string[] lines = file.text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            // 빈 줄은 박으로 세지 않는다
            if (line.Length == 0) continue;

            Prompt prompt = Parse(line, i + 1);
            if (prompt != null)
                prompt.beat = prompts.Count * beatsPerLine;
            prompts.Add(prompt);
        }
    }

    private static Prompt Parse(string line, int lineNumber)
    {
        Match match = LinePattern.Match(line);
        if (!match.Success)
        {
            Debug.LogError($"[PromptManager] Line {lineNumber}: expected \"(장면/장면) [가사] {{Command}}\" but got \"{line}\"");
            return null;
        }

        // 등록되지 않은 이름은 에러를 출력하고 건너뛴다. 가사는 남겨야 하므로 줄 자체는 버리지 않는다
        List<int> cardNums = new List<int>();
        foreach (string cardToken in match.Groups["cards"].Value.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (TryParseWord(cardToken, out CardName cardName))
                cardNums.Add((int)cardName);
            else
                Debug.LogError($"[PromptManager] Line {lineNumber}: \"{cardToken.Trim()}\" is not in CardName.");
        }

        CommandType command = CommandType.None;
        string commandToken = match.Groups["command"].Value;
        if (Whitespace.Replace(commandToken, "").Length > 0 && !TryParseWord(commandToken, out command))
            Debug.LogError($"[PromptManager] Line {lineNumber}: \"{commandToken.Trim()}\" is not in CommandType.");

        return new Prompt
        {
            lineNumber = lineNumber,
            cardNums = cardNums.ToArray(),
            lyric = match.Groups["lyric"].Value,
            command = (int)command,
        };
    }

    // 띄어쓰기를 무시하고 enum 이름과 비교한다. "3" 같은 숫자는 받지 않는다
    private static bool TryParseWord<T>(string word, out T value) where T : struct, Enum
    {
        string name = Whitespace.Replace(word, "");
        if (name.Length == 0 || char.IsDigit(name[0]) || name[0] == '-')
        {
            value = default;
            return false;
        }
        return Enum.TryParse(name, out value) && Enum.IsDefined(typeof(T), value);
    }

    // ( )에 한 번이라도 나온 장면들
    public HashSet<int> GetUsedCardNums()
    {
        HashSet<int> cardNums = new HashSet<int>();
        foreach (Prompt prompt in prompts)
        {
            if (prompt != null)
                cardNums.UnionWith(prompt.cardNums);
        }
        return cardNums;
    }

    // 박에 해당하는 Prompt. 해당 박에 시작하는 줄이 없으면 null
    public Prompt GetPrompt(int beat)
    {
        if (beat < 0 || beat % beatsPerLine != 0) return null;

        int index = beat / beatsPerLine;
        return index < prompts.Count ? prompts[index] : null;
    }

    public Command ReadPrompt(int sceneNum)
    {
        Prompt prompt = GetPrompt(sceneNum);
        return prompt == null ? null : CreateCommand(prompt);
    }

    // CommandType -> Command
    // 새 Command는 여기에 case를 추가해 연결한다
    private Command CreateCommand(Prompt prompt)
    {
        CommandType commandType = (CommandType)prompt.command;

        // () 이면 바꿀 장면이 없으므로 이전 장면을 유지한다
        if (prompt.cardNums.Length == 0)
        {
            if (commandType != CommandType.None)
                Debug.LogWarning($"[PromptManager] Line {prompt.lineNumber}: {{{commandType}}} has no card in ( ). Ignored.");
            return null;
        }

        switch (commandType)
        {
            case CommandType.None:
                return new Command(prompt.cardNums[0], 0, 0);

            case CommandType.격자로점멸:
                return new BlinkCommand(prompt.cardNums[0], prompt.cardNums[1], 0.9f, 4, 0, 0);

            case CommandType.첫줄:
                return new Command(prompt.cardNums[0], 1, 0);
            case CommandType.둘쨋줄:
                return new Command(prompt.cardNums[0], 2, 0);
            case CommandType.셋쨋줄:
                return new Command(prompt.cardNums[0], 3, 0);;
            case CommandType.위에서밑으로쫘르륵:
                return new ChainCommand(prompt.cardNums[0], 0, 0, 1, false);
            case CommandType.위에서밑으로천천히쫘르륵:
                return new ChainCommand(prompt.cardNums[0], 0, 0, 2, false);
            case CommandType.밑에서위로쫘르륵:
                return new ChainCommand(prompt.cardNums[0], 0, 0, 1, true);
            case CommandType.밑에서위로천천히쫘르륵:
                return new ChainCommand(prompt.cardNums[0], 0, 0, 2, true);
            case CommandType.카드박수:
                return new ClapCommand(prompt.cardNums[0], 0, 0);
            case CommandType.꺾기:
                return new BendCommand(prompt.cardNums[0], 0, 0);
            case CommandType.셋쨋줄가사:
                return new LyricCommand(prompt.cardNums[0],prompt.cardNums[1],2,0,0,1);
            case CommandType.둘쨋줄가사:
                return new LyricCommand(prompt.cardNums[0],prompt.cardNums[1],1,0,0,1);
            case CommandType.두박에:
                return new OffbeatCommand(prompt.cardNums[0], 0, 0, 0.25f);
            case CommandType.카드전체쫘라락넘기기:
                return new FlipCommand(prompt.cardNums[0],0,0,2);

            default:
                Debug.LogWarning($"[PromptManager] Line {prompt.lineNumber}: {{{commandType}}} is not connected to a Command yet. Falling back to a plain Command.");
                return new Command(prompt.cardNums[0], 0, 0);
        }
    }
}
