using UnityEngine;

// Child의 자세. CardChild.fbx의 Pose_* 애니메이션 이름과 같다
public enum ChildPose
{
    CardFace,   // 서있기
    Kneel,      // 무릎 꿇기
    LongSit,    // 앉기
}

// CardChild 하나. 자기 격자 위치의 카드 조각을 Card 메쉬에 띄우고 애니메이션을 재생한다
public class ChildBehavior : MonoBehaviour
{
    // CardChild.fbx 안의 카드 메쉬 이름
    private const string CardMeshName = "Card";

    // Animator Controller의 레이어와 상태 이름. CardChild.fbx의 애니메이션 이름과 같다
    // Body 레이어: Pose_CardFace, Pose_Kneel, Pose_LongSit (Child_Rig|Pose_*)
    // Card 레이어: Card_Pose_CardFace, Card_Pose_Kneel, Card_Pose_LongSit (Card|Card_Pose_*)
    // UpperBody 레이어: Empty, UpperBody_Clap, UpperBody_Bend (Child_Rig|UpperBody_*). 상체만 덮어쓰고, 상체 동작 중에만 Weight가 1이다
    private const int BodyLayer = 0;
    private const int CardLayer = 1;
    private const int UpperBodyLayer = 2;
    public const string ClapState = "UpperBody_Clap";
    public const string BendState = "UpperBody_Bend";
    public const string FlipUpState = "Card_Flip_Up";
    public const string FlipDownState = "Card_Flip_Down";

    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");       // URP Lit
    private static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");       // Built-in / Unlit
    private static readonly int MainTexStId = Shader.PropertyToID("_MainTex_ST");

    // 격자 위치. row 0이 맨 위, column 0이 맨 왼쪽. CardSprites.GetCard(scene, Row, Column)로 자기 조각을 찾는다
    public int Row { get; private set; }
    public int Column { get; private set; }
    public ChildPose Pose { get; private set; }
    // 지금 들고 있는 장면. 아직 없으면 -1
    public int CurrentScene { get; private set; } = -1;

    private CardSprites cardSprites;
    private Animator animator;
    private Renderer cardRenderer;
    private MaterialPropertyBlock propertyBlock;
    // 재생 중인 상체 동작 상태. 없으면 null
    private string upperBodyState;
    // Animator가 새 재생을 실제로 시작했는지(normalizedTime이 0부터 다시 흐르는 것을 봤는지)
    private bool upperBodyStarted;

    // SummonManager가 소환 직후 호출한다
    public void Initialize(int row, int column, ChildPose pose, CardSprites cardSprites)
    {
        Row = row;
        Column = column;
        this.cardSprites = cardSprites;

        animator = GetComponentInChildren<Animator>();
        propertyBlock = new MaterialPropertyBlock();
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            if (renderer.name == CardMeshName)
            {
                cardRenderer = renderer;
                break;
            }
        }
        if (cardRenderer == null)
            Debug.LogError($"[ChildBehavior] {name}: no Renderer named '{CardMeshName}'.");

        SetPose(pose);
    }

    // 몸과 카드를 pose 자세로 둔다
    public void SetPose(ChildPose pose)
    {
        Pose = pose;
        PlayState($"Pose_{pose}", BodyLayer);
        PlayState($"Card_Pose_{pose}", CardLayer);

        // 다음 프레임을 기다리지 않고 자세를 바로 적용한다. SummonManager가 곧바로 CardBounds를 읽는다
        if (animator != null && animator.runtimeAnimatorController != null)
            animator.Update(0f);
    }

    // 지금 자세에서 카드 메쉬의 월드 경계
    public Bounds CardBounds => cardRenderer != null ? cardRenderer.bounds : new Bounds(transform.position, Vector3.zero);

    // sceneNum은 CardName 값. 이미지를 먼저 바꾸고 카드를 들어 올린다
    public void OpenCard(int sceneNum)
    {
        // (접기)는 그림을 그리지 않고 카드를 보이지 않게 한다
        if (sceneNum == (int)CardName.접기)
        {
            SetCardVisible(false);
            CurrentScene = sceneNum;
            return;
        }

        if (!cardSprites.HasScene(sceneNum))
        {
            Debug.LogWarning($"[ChildBehavior] {name}: no image for CardName.{(CardName)sceneNum}.");
            return;
        }

        SetCardImage(cardSprites.GetCard(sceneNum, Row, Column));
        SetCardVisible(true);
        CurrentScene = sceneNum;

        // 같은 박에 다시 불려도 처음부터 재생한다
        // 지금 모델에는 넘기는 애니메이션이 없으므로, 상태가 없으면 건너뛴다
        PlayState(FlipUpState, CardLayer);
    }

    // 카드를 내린다
    public void CloseCard() => PlayState(FlipDownState, CardLayer);

    // 자세는 그대로 두고 상체로 박수를 한 번 친다
    public void Clap() => PlayUpperBody(ClapState);

    // 자세는 그대로 두고 상체를 한 번 꺾었다 편다
    public void Bend() => PlayUpperBody(BendState);

    // 상체 동작을 처음부터 재생한다. 다른 상체 동작 중이었으면 새 동작으로 바뀐다
    private void PlayUpperBody(string state)
    {
        if (!PlayState(state, UpperBodyLayer)) return;

        animator.SetLayerWeight(UpperBodyLayer, 1f);
        upperBodyState = state;
        upperBodyStarted = false;
    }

    // 상체 동작을 멈추고 상체를 자세로 되돌린다. 시점 이동에 쓴다
    public void StopUpperBody()
    {
        upperBodyState = null;
        if (animator != null && UpperBodyLayer < animator.layerCount)
            animator.SetLayerWeight(UpperBodyLayer, 0f);
    }

    // 상체 동작 클립은 반복하지 않으므로, 끝까지 재생되면 레이어를 꺼서 자세의 상체로 돌아간다
    private void Update()
    {
        if (upperBodyState == null) return;

        // Play는 다음 애니메이션 업데이트에서야 적용되므로, 그 전에는 지난번 재생이 끝난 상태(normalizedTime >= 1)가 읽힌다
        // 바로 끝났다고 보면 같은 동작을 두 번째로 재생할 때 레이어를 꺼 버리므로, 새 재생이 시작된 것을 본 뒤에만 끝을 판단한다
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(UpperBodyLayer);
        if (!state.IsName(upperBodyState)) return;

        if (state.normalizedTime < 1f)
            upperBodyStarted = true;
        else if (upperBodyStarted)
            StopUpperBody();
    }

    // 렌더러를 꺼서 카드를 완전히 투명하게 한다. 그림자도 함께 사라진다
    private void SetCardVisible(bool visible)
    {
        if (cardRenderer != null)
            cardRenderer.enabled = visible;
    }

    // Controller에 없는 레이어나 상태는 건너뛰고 false를 돌려준다
    private bool PlayState(string state, int layer)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        if (layer >= animator.layerCount) return false;

        int stateHash = Animator.StringToHash(state);
        if (!animator.HasState(layer, stateHash)) return false;

        // 노래 속도에 맞춰 애니메이션 속도도 바꾼다
        if (Metronome.Instance != null)
            animator.speed = Metronome.Instance.Speed;
        animator.Play(stateHash, layer, 0f);
        return true;
    }

    // Sprite는 원본 텍스처의 일부이므로, 원본 텍스처를 넣고 tiling/offset으로 조각 부분만 보이게 한다
    private void SetCardImage(Sprite card)
    {
        if (cardRenderer == null) return;

        Texture2D texture = card.texture;
        Rect rect = card.textureRect;
        Vector4 scaleOffset = new Vector4(
            rect.width / texture.width,
            rect.height / texture.height,
            rect.x / texture.width,
            rect.y / texture.height);

        // 머티리얼을 복제하지 않도록 MaterialPropertyBlock을 쓴다
        cardRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetTexture(BaseMapId, texture);
        propertyBlock.SetVector(BaseMapStId, scaleOffset);
        propertyBlock.SetTexture(MainTexId, texture);
        propertyBlock.SetVector(MainTexStId, scaleOffset);
        cardRenderer.SetPropertyBlock(propertyBlock);
    }
}
