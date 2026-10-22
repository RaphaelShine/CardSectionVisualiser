// ( ) 안의 장면 이름. 값이 CardSprites의 장면 번호가 된다
// 텍스트에 새 장면 이름을 쓰면 여기에도 추가한다
public enum CardName
{
    창문,
    눈,
    숲,
    썰매,
    노을얼굴,
    밤,
    암흑,
    무지개,
    별들과구유,
    눈싸움,
    노을새싹,
}

// { } 안의 Command 이름. 텍스트의 띄어쓰기는 무시한다. 예) {격자로 점멸} -> 격자로점멸
// 텍스트에 새 Command 이름을 쓰면 여기에도 추가한다
public enum CommandType
{
    None,               // {} 또는 { } 생략
    격자로점멸,
    첫줄,
    둘쨋줄,
    셋쨋줄,
    위에서아래로쫘르륵,
    위에서아래로천천히쫘르륵,
}

// 프롬프트 텍스트파일의 한 줄
// (장면/장면) [가사] {Command}
public class Prompt
{
    public int lineNumber;  // 텍스트파일에서의 줄 번호(1부터). 에러 메시지용
    public int beat;        // 이 줄이 실행되는 박
    public int[] cardNums;  // ( ) 안의 장면들을 CardName으로 바꾼 값. 예) (창문/눈) -> {0, 1}, () -> {}
    public string lyric;    // [ ] 안의 가사. 텍스트 그대로
    public int command;     // { } 안의 글을 CommandType으로 바꾼 값

    public override string ToString()
    {
        string[] cardNames = new string[cardNums.Length];
        for (int i = 0; i < cardNums.Length; i++)
            cardNames[i] = ((CardName)cardNums[i]).ToString();

        return $"({string.Join("/", cardNames)}) [{lyric}] {{{(CommandType)command}}}";
    }
}
