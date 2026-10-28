using System;
using UnityEngine;

// 시작 화면에서 고르는 곡 하나에 필요한 것들
[Serializable]
public class Song
{
    // Masses/Cards/ 아래 카드 폴더 이름. 선택 화면에도 이 이름이 뜬다
    public string name;
    // Masses/Prompts/의 프롬프트 텍스트
    public TextAsset prompt;
    public AudioClip music;
    public float bpm = 60f;
    // 노래 시작부터 첫 박(0번 박)까지의 시간(초)
    public float firstBeatOffset;
}
