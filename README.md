## 카드섹션 가시화 프로그램
유니티 6000.6.2f1  

## features
- sprite -> 3 by 4 카드들로 변환
- 노래, 가사, 카드 동시 출력
- 가사 눌러 시점 이동
- 박자 기준으로 메서드 호출만 하드코딩할 수 있도록 설계

## file tree
```
Assets/
├─ Masses/
│  ├─ Animations/
│  ├─ Animators/
│  ├─ Cards/             // Card로 쓸 Sprites
│  ├─ Models/
│  └─ Sounds/
├─ Scenes/
│  └─ SampleScene.unity
├─ ScriptableObjects/
│  └─ CardSprites.asset  // 장면 순서대로 Sprites[3,4] 나열
├─ Scripts/
│  ├─ CardSprites.cs     // Scriptable Object Script
│  ├─ ChildBehavior.cs   // CardSprites.asset 접근하여 적절한 카드, 넘기는 동작 출력
│  ├─ CommandManager.cs  // 각각의 ChildBehavior.cs 접근하여 OpenCard(int page)나 특수 행동 호출
│  ├─ DivisionManager.cs // Masses/Cards/ 접근하여 3 by 4의 sprites로 변환, 조각을 CardSprites.asset에 저장
│  ├─ GameManager.cs     // Metronome 접근해 적절한 박자, PromptManager 접근해 적절한 메서드로 CommandManager 호출, InputManager의 Input 받아 시간 조작
│  ├─ InputManager.cs    // UI 누르면 시간 바뀌도록, 키보드로 속도 조절
│  ├─ Metronome.cs       // 박자 맞춰 GameManager 시간 field 조절
│  ├─ Prompt.cs
│  ├─ PromptManager.cs   // 텍스트 읽고 시간 값에 맞춰 Prompt 전달
│  ├─ SummonManager.cs   // Models/CardChild를 3 by 4 격자로 소환, ChildBehavior 목록 제공
│  └─ UIManager.cs       // 화면 좌측에 가사 띄우고 해당 시간 저장, 누르면 시점 이동
├─ Settings/
│  └─ etc...
└─ InputSystem_Actions.inputactions
```