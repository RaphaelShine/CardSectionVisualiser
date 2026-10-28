// ( ) 안의 장면 이름. 값이 CardSprites의 장면 번호가 된다
// 텍스트에 새 장면 이름을 쓰면 여기에도 추가한다
public enum CardName
{
    // LookOutTheWindow
    창문,
    눈창문,
    숲,
    눈숲,
    썰매,
    노을얼굴,
    밤,
    암흑,
    트리,
    별들과구유,
    눈싸움,
    노을새싹,
    // NewMiracle
    꽃봉오리,
    접기,
    주님말씀,
    하늘,
    꽃여러개,
    신랑신부,
    술통,
    마리아예수,
    아직,
    그러나,
    포도주,
    고민,
    왕관,
    예수님,
    판단,
    하트,
    기도손,
    애들얼굴,
    마지막장,
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
    위에서밑으로쫘르륵,
    위에서밑으로천천히쫘르륵,
    카드박수,
    셋쨋줄가사,
    둘쨋줄가사,
    밑에서위로쫘르륵,
    밑에서위로천천히쫘르륵,
    두박에,
    카드전체쫘라락넘기기,
    수어,
    꺾기,
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
