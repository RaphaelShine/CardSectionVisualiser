using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

// Models/CardChild를 CardSprites.Rows * Columns 격자로 소환한다
// 각 Child는 자기 격자 위치(row, column)의 카드 조각을 맡는다
public class SummonManager : MonoBehaviour
{
    // Masses/Models/CardChild.fbx
    [SerializeField] private AssetReferenceGameObject cardChild;
    [SerializeField] private CardSprites cardSprites;
    // Body 레이어(Pose_*)와 Card 레이어(Card_Pose_*)를 가진 Animator Controller. 상태 이름은 ChildBehavior 참고
    [SerializeField] private RuntimeAnimatorController childAnimator;
    // row마다의 자세. 0번(맨 위)부터. 뒷줄은 서고, 가운데 줄은 무릎 꿇고, 앞줄은 앉는다
    [SerializeField] private ChildPose[] rowPoses = { ChildPose.CardFace, ChildPose.Kneel, ChildPose.LongSit };
    // 카드가 보이는 쪽(관객)에서 봤을 때 오른쪽(column), 아래(row)로 한 칸 갈 때의 거리. 격자 중심이 이 오브젝트 위치가 된다
    // CardChild는 +z를 향하므로 관객 기준 오른쪽은 -x, 앞줄은 +z다. 카드 너비가 0.4라 column은 거의 맞닿는다
    // 줄의 높이는 rowStep이 아니라 StackRows가 자세에 맞춰 정한다
    [SerializeField] private Vector3 audienceRightStep = new Vector3(-0.42f, 0f, 0f);
    [SerializeField] private Vector3 rowStep = new Vector3(0f, 0f, 0.2f);
    // 위아래 줄 카드 사이의 틈
    [SerializeField] private float rowCardGap = 0.02f;

    private AsyncOperationHandle<GameObject> loadHandle;
    // row-major. row 0이 맨 위, column 0이 맨 왼쪽
    private ChildBehavior[] children = Array.Empty<ChildBehavior>();

    public IReadOnlyList<ChildBehavior> Children => children;
    public bool IsSummoned { get; private set; }
    public event Action OnSummoned;

    public ChildBehavior GetChild(int row, int column) => children[row * CardSprites.Columns + column];

    public ChildBehavior[] GetRow(int row)
    {
        ChildBehavior[] result = new ChildBehavior[CardSprites.Columns];
        Array.Copy(children, row * CardSprites.Columns, result, 0, CardSprites.Columns);
        return result;
    }

    public ChildBehavior[] GetColumn(int column)
    {
        ChildBehavior[] result = new ChildBehavior[CardSprites.Rows];
        for (int row = 0; row < CardSprites.Rows; row++)
            result[row] = GetChild(row, column);
        return result;
    }

    private IEnumerator Start()
    {
        // 비어 있으면 Child마다 OpenCard에서 NullReferenceException이 난다
        if (cardSprites == null)
        {
            Debug.LogError("[SummonManager] CardSprites is not assigned.");
            yield break;
        }

        loadHandle = Addressables.LoadAssetAsync<GameObject>(cardChild);
        yield return loadHandle;

        if (loadHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[SummonManager] Failed to load CardChild: {loadHandle.OperationException}");
            yield break;
        }

        Vector3 center = (audienceRightStep * (CardSprites.Columns - 1) + rowStep * (CardSprites.Rows - 1)) * 0.5f;

        children = new ChildBehavior[CardSprites.Rows * CardSprites.Columns];
        for (int row = 0; row < CardSprites.Rows; row++)
        {
            for (int column = 0; column < CardSprites.Columns; column++)
            {
                GameObject instance = Instantiate(loadHandle.Result, transform);
                instance.name = $"CardChild_{row}_{column}";
                instance.transform.localPosition = audienceRightStep * column + rowStep * row - center;

                // 모델에는 ChildBehavior가 없으므로 붙여 준다
                if (!instance.TryGetComponent(out ChildBehavior child))
                    child = instance.AddComponent<ChildBehavior>();

                Animator animator = instance.GetComponentInChildren<Animator>();
                if (animator == null)
                    animator = instance.AddComponent<Animator>();
                animator.runtimeAnimatorController = childAnimator;

                ChildPose pose = row < rowPoses.Length ? rowPoses[row] : ChildPose.CardFace;
                child.Initialize(row, column, pose, cardSprites);

                children[row * CardSprites.Columns + column] = child;
            }
        }

        StackRows();

        IsSummoned = true;
        OnSummoned?.Invoke();
    }

    // 자세마다 카드 높이가 다르므로, 맨 앞줄은 그대로 두고 뒷줄을 올려 카드가 rowCardGap만큼 떨어져 쌓이게 한다
    private void StackRows()
    {
        float belowTop = 0f;
        for (int row = CardSprites.Rows - 1; row >= 0; row--)
        {
            ChildBehavior[] rowChildren = GetRow(row);
            Bounds card = rowChildren[0].CardBounds;
            float lift = row == CardSprites.Rows - 1 ? 0f : belowTop + rowCardGap - card.min.y;

            foreach (ChildBehavior child in rowChildren)
                child.transform.position += Vector3.up * lift;
            belowTop = card.max.y + lift;
        }
    }

    private void OnDestroy()
    {
        foreach (ChildBehavior child in children)
        {
            if (child != null)
                Destroy(child.gameObject);
        }
        children = Array.Empty<ChildBehavior>();

        if (loadHandle.IsValid())
            loadHandle.Release();
    }
}
