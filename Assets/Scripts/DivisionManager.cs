using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class DivisionManager : MonoBehaviour
{
    [SerializeField] private CardSprites cardSprites;
    // Masses/Cards/의 Sprite들에 붙인 Addressables label
    [SerializeField] private AssetLabelReference cardLabel;

    private AsyncOperationHandle<IList<Sprite>> loadHandle;
    private readonly List<Sprite> createdSprites = new List<Sprite>();

    public bool IsDivided { get; private set; }
    public event Action OnDivided;

    private IEnumerator Start()
    {
        loadHandle = Addressables.LoadAssetsAsync<Sprite>(cardLabel, null);
        yield return loadHandle;

        if (loadHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[DivisionManager] Failed to load sprites with label '{cardLabel.labelString}': {loadHandle.OperationException}");
            yield break;
        }

        // 파일 이름 = CardName 이름. 그 CardName 값이 장면 번호가 된다
        cardSprites.Clear();
        foreach (Sprite source in loadHandle.Result)
        {
            // macOS에서 만든 한글 파일 이름은 자모가 분리된(NFD) 형태일 수 있다
            string name = source.name.Normalize(NormalizationForm.FormC);
            if (!Enum.TryParse(name, out CardName cardName) || !Enum.IsDefined(typeof(CardName), cardName))
            {
                Debug.LogWarning($"[DivisionManager] Sprite '{source.name}' does not match any CardName. Skipped.");
                continue;
            }
            cardSprites.SetScene(cardName, Divide(source));
        }

        for (int i = 0; i < CardSprites.SceneCount; i++)
        {
            if (!cardSprites.HasScene(i))
                Debug.LogWarning($"[DivisionManager] No image for CardName.{(CardName)i}.");
        }

        IsDivided = true;
        OnDivided?.Invoke();
    }

    // source를 세로 Rows * 가로 Columns 조각으로 나눈다. row 0이 맨 위
    private Sprite[] Divide(Sprite source)
    {
        Rect rect = source.textureRect;

        Sprite[] cards = new Sprite[CardSprites.Rows * CardSprites.Columns];
        for (int row = 0; row < CardSprites.Rows; row++)
        {
            for (int column = 0; column < CardSprites.Columns; column++)
            {
                // 1024 / 3처럼 나누어떨어지지 않으면 조각 경계가 소수가 되고, Sprite.Create가 0 크기 Sprite를 만든다
                // 경계를 정수 픽셀로 반올림해 조각들이 틈 없이 원본을 덮게 한다
                // 텍스처 좌표는 아래에서 위로 증가하므로 row를 위에서부터 센다
                float left = rect.x + Mathf.Round(rect.width * column / CardSprites.Columns);
                float right = rect.x + Mathf.Round(rect.width * (column + 1) / CardSprites.Columns);
                float top = rect.yMax - Mathf.Round(rect.height * row / CardSprites.Rows);
                float bottom = rect.yMax - Mathf.Round(rect.height * (row + 1) / CardSprites.Rows);
                Rect pieceRect = Rect.MinMaxRect(left, bottom, right, top);

                // 카드 메쉬에는 textureRect만 쓰므로 Tight 메쉬를 만들 필요가 없다
                Sprite card = Sprite.Create(source.texture, pieceRect, new Vector2(0.5f, 0.5f), source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                card.name = $"{source.name}_{row}_{column}";

                cards[row * CardSprites.Columns + column] = card;
                createdSprites.Add(card);
            }
        }
        return cards;
    }

    private void OnDestroy()
    {
        foreach (Sprite sprite in createdSprites)
            Destroy(sprite);
        createdSprites.Clear();

        if (cardSprites != null)
            cardSprites.Clear();

        if (loadHandle.IsValid())
            loadHandle.Release();
    }
}
