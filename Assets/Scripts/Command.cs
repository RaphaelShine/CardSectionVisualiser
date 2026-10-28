using System.Collections;
using System.Linq;
using UnityEngine;

public class Command
{
    public int row; // 0: all, 1: up, 2: midd, 3: down
    public int col; // 0: all, 1: right

    protected int cardNum;
    public Command(int s, int r, int c)
    {
        cardNum = s;
        row = r;
        col = c;
    }
    // manager: 박 단위로 기다려야 하는 Command는 manager.StartCoroutine으로 코루틴을 돌린다
    public virtual void OpenCard(CommandManager manager, ChildBehavior[] children)
    {
        foreach (var child in children)
            child.OpenCard(cardNum);
    }
    // 시점 이동용. 기다리지 않고 Command가 끝났을 때의 카드로 바로 바꾼다
    public virtual void Finish(ChildBehavior[] children)
    {
        foreach (var child in children)
            child.OpenCard(cardNum);
    }
    protected IEnumerator RestAWhile(float restInterval, ChildBehavior child)
    {
        yield return new WaitForSeconds(Metronome.Beat2Second(restInterval));
        child.OpenCard(cardNum);
    }
}
public class BlinkCommand : Command
{
    private float blinkInterval; // 단위는 한 박
    private int blinkcardNum;
    private int blinkTime;
    public BlinkCommand(int s, int bS, float bInterval, int bTime, int r, int c) : base(s,r,c)
    {
        blinkInterval = bInterval;
        blinkcardNum = bS;
        blinkTime = bTime;
    }
    public override void OpenCard(CommandManager manager, ChildBehavior[] children)
    {
        foreach (var child in children)
            manager.StartCoroutine(BlinkFor(blinkTime, blinkInterval, child));
    }
    // 마지막 점멸(i = blinkTime - 1)의 카드
    public override void Finish(ChildBehavior[] children)
    {
        foreach (var child in children)
            child.OpenCard(((blinkTime-1+child.Row+child.Column)%2==0)?cardNum:blinkcardNum);
    }
    private IEnumerator BlinkFor(int time, float restInterval, ChildBehavior child)
    {
        for (int i=0; i<time; i++)
        {
            yield return new WaitForSeconds(Metronome.Beat2Second(restInterval/time));
            // row, col은 Command가 고른 대상(모든 Child가 같은 값)이므로, 격자 무늬는 Child 자신의 위치로 만든다
            child.OpenCard(((i+child.Row+child.Column)%2==0)?cardNum:blinkcardNum);
        }
    }
}
public class LyricCommand : Command
{
    private int lyricRow;
    private int lyricScene;
    private float restInterval; // 단위는 한 박
    public LyricCommand(int s, int lS, int lR, int r, int c, float rest) : base(s,r,c)
    {
        lyricRow = lR;
        lyricScene = lS;
        restInterval = rest;
    }
    public override void OpenCard(CommandManager manager, ChildBehavior[] children)
    {
        for (int r=0; r<3; r++)
        {
            if (lyricRow == r)
            {
                for (int c=0; c<4; c++)
                {
                    float restFor = restInterval / 4 * c;
                    manager.StartCoroutine(RestAWhile(lyricScene, restFor, children[r*4+c]));
                }
            }
            else
            {
                for (int c=0; c<4; c++)
                {
                    children[r*4+c].OpenCard(cardNum);
                }
            }
        }
    }
    private IEnumerator RestAWhile(int cardNum, float restInterval, ChildBehavior child)
    {
        yield return new WaitForSeconds(Metronome.Beat2Second(restInterval));
        child.OpenCard(cardNum);
    }
}
public class OffbeatCommand : Command
{
    private float restInterval; // 단위는 한 박
    public OffbeatCommand(int s, int r, int c, float rest) : base(s,r,c)
    {
        restInterval = rest;
    }
    public override void OpenCard(CommandManager manager, ChildBehavior[] children)
    {
        foreach (var child in children)
            manager.StartCoroutine(RestAWhile(restInterval,child));
    }
}
public class ChainCommand : Command
{
    private float restInterval; // 단위는 한 박
    private bool isReverse;
    public ChainCommand(int s, int r, int c, float rest, bool reverse) : base(s,r,c)
    {
        restInterval = rest;
        isReverse = reverse;
    }
    public override void OpenCard(CommandManager manager, ChildBehavior[] children)
    {
        if (isReverse)
            children.Reverse();
        for (int r=0; r<3; r++)
        {
            float restFor = restInterval / 3 * r;
            for (int c=0; c<4; c++)
            {
                manager.StartCoroutine(RestAWhile(restFor,children[r*4+c]));
            }
        }
    }
}
public class FlipCommand : Command
{
    private float restInterval; // 단위는 한 박
    public FlipCommand(int s, int r, int c, float rest) : base(s,r,c)
    {
        restInterval = rest;
    }
    public override void OpenCard(CommandManager manager, ChildBehavior[] children)
    {
        foreach (var child in children)
            manager.StartCoroutine(Flip(restInterval,child,restInterval));
    }
    private IEnumerator Flip(float restInterval, ChildBehavior child, float rest)
    {
        float restFor = restInterval / cardNum;
        for (int i=0; i<cardNum; i++)
        {
            yield return new WaitForSeconds(Metronome.Beat2Second(restFor));
            child.OpenCard(i);
        }
    }
}
// {카드박수}. 카드를 바꾸고, 자세는 그대로 둔 채 상체로 박수를 한 번 친다
public class ClapCommand : Command
{
    public ClapCommand(int s, int r, int c) : base(s,r,c) {}
    public override void OpenCard(CommandManager manager, ChildBehavior[] children)
    {
        foreach (var child in children)
        {
            child.OpenCard(cardNum);
            child.Clap();
        }
    }
    // 시점 이동으로 지나간 박수는 다시 치지 않는다. 카드만 바꾸는 Command.Finish를 그대로 쓴다
}
// {꺾기}. 카드를 바꾸고, 자세는 그대로 둔 채 상체를 한 번 꺾었다 편다
public class BendCommand : Command
{
    public BendCommand(int s, int r, int c) : base(s,r,c) {}
    public override void OpenCard(CommandManager manager, ChildBehavior[] children)
    {
        foreach (var child in children)
        {
            child.OpenCard(cardNum);
            child.Bend();
        }
    }
    // 시점 이동으로 지나간 꺾기는 다시 하지 않는다. 카드만 바꾸는 Command.Finish를 그대로 쓴다
}
