## 카드섹션 가시화 프로그램
유니티 6000.6.2f1  

## features
- 곡 선택 화면에서 곡을 고르면 노래, 가사, 카드섹션을 함께 재생
- 장면 이미지를 3 by 4 카드 조각으로 나누어 Child 12명이 하나씩 들고 표현
- 줄마다 다른 자세 (윗줄 서기, 가운데 줄 무릎 꿇기, 아랫줄 앉기)
- 노래 재생 위치를 기준으로 박을 세어, 박마다 프롬프트 텍스트의 한 줄을 실행
- 화면 왼쪽 가사를 눌러 그 시점으로 이동, 휠로 가사 목록 스크롤
- 카드 동작은 텍스트파일에 `(장면) [가사] {Command}`로 적기만 하면 되도록 설계

## 실행 흐름
1. `GameManager`가 곡 선택 화면을 띄운다
2. 곡을 고르면 `PromptManager`가 프롬프트 텍스트를 읽고, `Metronome`에 노래와 bpm을 넣는다
3. `DivisionManager`가 그 곡의 카드 이미지를 3 by 4로 나누고, `SummonManager`가 Child 12명을 소환한다
4. 둘 다 준비되면 노래를 시작한다
5. `Metronome`이 새 박마다 `OnBeat`를 부르면 `GameManager.OnScene`이 그 박의 `Prompt`를 `Command`로 바꿔 `CommandManager`에 넘긴다
6. `CommandManager`가 대상 Child를 골라 `Command`를 실행하고, 각 `ChildBehavior`가 자기 조각을 카드에 그린다

## 곡 추가하기
1. `Masses/Sounds/`에 노래를 넣는다
2. `Masses/Cards/<곡 이름>/`에 장면 이미지를 넣는다
   - 파일 이름은 `CardName` enum 이름과 같아야 한다 (예: `창문.png`)
   - Addressable로 등록하고 기존 카드와 같은 label을 붙인다. Sprite Mode는 Single
3. `Masses/Prompts/<곡 이름>.txt`에 프롬프트를 쓴다
4. 새 장면, Command 이름은 `Prompt.cs`의 `CardName`, `CommandType`에 추가한다
5. 씬의 `GameManager`의 `songs`에 곡을 추가한다 (`name`은 카드 폴더 이름, `prompt`, `music`, `bpm`, `firstBeatOffset`)

## 프롬프트 형식
한 줄이 한 박이다. 빈 줄은 박으로 세지 않는다.
```
(장면/장면) [가사] {Command}

(꽃봉오리) [나의 마음을] {}
(접기/주님말씀) [주님의 말씀을] {셋쨋줄가사}
() [] {}
```
- `( )` : 장면 이름. `/`로 여러 개. 비어 있으면 카드를 바꾸지 않는다
- `[ ]` : 가사. 그대로 화면에 표시. 비어 있으면 가사 목록에 넣지 않는다
- `{ }` : Command 이름. 띄어쓰기는 무시한다. 비우거나 생략하면 모든 카드를 첫 장면으로 바꾼다
- `(접기)`는 그림 대신 카드를 보이지 않게 한다

| Command | 동작 |
|---|---|
| (없음) | 모든 카드를 첫 장면으로 |
| 격자로점멸 | 두 장면을 체크무늬로 번갈아 점멸 |
| 첫줄 / 둘쨋줄 / 셋쨋줄 | 해당 줄만 바꾸기 |
| 위에서밑으로(천천히)쫘르륵 | 윗줄부터 차례로 바꾸기 |
| 밑에서위로(천천히)쫘르륵 | 아랫줄부터 차례로 바꾸기 |
| 둘쨋줄가사 / 셋쨋줄가사 | 해당 줄은 두 번째 장면을 왼쪽부터 차례로, 나머지 줄은 첫 장면 |
| 두박에 | 0.25박 늦게 바꾸기 |
| 카드전체쫘라락넘기기 | 2박 동안 0번 장면부터 첫 장면 직전까지 차례로 넘기기 |
| 카드박수, 수어 | 아직 전용 동작 없음 (첫 장면으로 바꾸기) |

새 Command는 `Command.cs`에 클래스를 만들고 `PromptManager.CreateCommand`의 `switch`에 연결한다.

## file tree
```
Assets/
├─ AddressableAssetsData/
├─ Masses/
│  ├─ Animations/
│  ├─ Animators/
│  │  └─ ChildAnimator.controller // Body 레이어(Pose_*), Card 레이어(Card_Pose_*)
│  ├─ Cards/
│  │  └─ <곡 이름>/            // 곡마다 Card로 쓸 Sprites. 파일 이름 = CardName
│  ├─ Models/
│  │  └─ CardChild.fbx         // Child와 Card 메쉬, 자세 애니메이션(CardFace, Kneel, LongSit)
│  ├─ Prompts/
│  │  └─ <곡 이름>.txt         // (장면) [가사] {Command}
│  └─ Sounds/
│     └─ <곡 이름>.mp3
├─ Scenes/
│  └─ SampleScene.unity
├─ ScriptableObjects/
│  └─ CardSprites.asset        // CardName 순서대로 장면마다 Sprites[3,4]
├─ Scripts/
│  ├─ Editor/
│  │  └─ ChildAnimatorBuilder.cs // CardChild.fbx 클립으로 ChildAnimator.controller 생성 (Tools > Build Child Animator)
│  ├─ CardSprites.cs           // Scriptable Object Script. 장면별 3 by 4 조각 저장
│  ├─ ChildBehavior.cs         // 격자 위치의 조각을 Card 메쉬에 그리기, row별 자세, (접기) 숨기기
│  ├─ Command.cs               // Command와 BlinkCommand, LyricCommand, OffbeatCommand, ChainCommand, FlipCommand
│  ├─ CommandManager.cs        // Command의 row, col로 대상 ChildBehavior를 골라 실행, 코루틴 실행·정지
│  ├─ DivisionManager.cs       // 고른 곡의 Masses/Cards/ 이미지를 Addressables로 불러와 3 by 4로 나누어 CardSprites.asset에 저장
│  ├─ GameManager.cs           // 곡 선택, 준비되면 재생, 박마다 OnScene으로 Prompt -> Command 실행, 시점 이동
│  ├─ InputManager.cs          // 마우스·터치로 곡 버튼, 가사 누르기. 휠로 가사 스크롤
│  ├─ Metronome.cs             // 노래 재생 위치로 박을 세어 OnBeat 발생. 시점 이동, 재생 속도
│  ├─ Prompt.cs                // 프롬프트 한 줄. CardName, CommandType enum
│  ├─ PromptManager.cs         // 텍스트 읽고 박에 맞는 Prompt를 Command로 변환
│  ├─ Song.cs                  // 곡 하나의 이름, 프롬프트, 노래, bpm, 첫 박 위치
│  ├─ SummonManager.cs         // Models/CardChild를 3 by 4 격자로 소환, 자세에 맞춰 줄 높이 쌓기, ChildBehavior 목록 제공
│  └─ UIManager.cs             // 곡 선택 화면, 화면 왼쪽 가사 목록과 현재 박 강조
├─ Settings/
│  └─ etc...
└─ InputSystem_Actions.inputactions
```
