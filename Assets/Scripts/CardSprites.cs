using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CardSprites", menuName = "Scriptable Objects/CardSprites")]
public class CardSprites : ScriptableObject
{
    public const int Rows = 3;
    public const int Columns = 4;

    // 한 장면(원본 Sprite 하나)을 Rows * Columns 조각으로 나눈 결과
    // Unity는 2차원 배열을 직렬화하지 못하므로 row-major 1차원 배열로 저장한다
    [Serializable]
    public class CardScene
    {
        public string name;
        public Sprite[] cards = new Sprite[Rows * Columns];

        public Sprite this[int row, int column] => cards[row * Columns + column];
    }

    // CardName 순서대로 나열. 이미지가 없는 장면은 cards가 비어 있다
    [SerializeField] private List<CardScene> scenes = new List<CardScene>();

    public static int SceneCount => Enum.GetValues(typeof(CardName)).Length;

    public bool HasScene(int scene) =>
        scene >= 0 && scene < scenes.Count && scenes[scene].cards != null && scenes[scene].cards.Length == Rows * Columns;

    public CardScene GetScene(int scene) => scenes[scene];
    public CardScene GetScene(CardName scene) => GetScene((int)scene);

    // scene은 CardName 값. row 0이 맨 위, column 0이 맨 왼쪽
    public Sprite GetCard(int scene, int row, int column) => scenes[scene][row, column];
    public Sprite GetCard(CardName scene, int row, int column) => GetCard((int)scene, row, column);

    // 모든 CardName 자리를 빈 장면으로 채운다
    public void Clear()
    {
        scenes.Clear();
        for (int i = 0; i < SceneCount; i++)
            scenes.Add(new CardScene { name = ((CardName)i).ToString(), cards = Array.Empty<Sprite>() });
    }

    public void SetScene(CardName scene, Sprite[] cards)
    {
        if (cards.Length != Rows * Columns)
            throw new ArgumentException($"cards must contain {Rows * Columns} sprites.", nameof(cards));
        if (scenes.Count != SceneCount)
            Clear();

        scenes[(int)scene].cards = cards;
    }
}
