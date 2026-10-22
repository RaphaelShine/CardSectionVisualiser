using System.Collections.Generic;
using UnityEngine;

public class CommandManager : MonoBehaviour
{
    [SerializeField] private SummonManager summonManager;

    public void CommandChildren(Command command)
    {
        if (!summonManager.IsSummoned) return;

        command.OpenCard(this, SelectChildren(command));
    }

    // 시점 이동용. 애니메이션 없이 Command가 끝난 상태로 바로 만든다
    public void FinishChildren(Command command)
    {
        if (!summonManager.IsSummoned) return;

        command.Finish(SelectChildren(command));
    }

    // 박 단위로 기다리는 중인 Command(점멸, 쫘르륵)를 모두 멈춘다
    public void StopCommands() => StopAllCoroutines();

    // Command.row: 0 all, 1 up, 2 midd, 3 down
    // Command.col: 0 all, n 오른쪽에서 n번째 열
    private ChildBehavior[] SelectChildren(Command command)
    {
        List<ChildBehavior> selected = new List<ChildBehavior>();
        foreach (ChildBehavior child in summonManager.Children)
        {
            // Command는 위에서부터 1, ChildBehavior는 위에서부터 0
            bool rowMatch = command.row == 0 || command.row == child.Row + 1;
            bool columnMatch = command.col == 0 || command.col == CardSprites.Columns - child.Column;

            if (rowMatch && columnMatch)
                selected.Add(child);
        }
        return selected.ToArray();
    }
}
