# C# 코드 컨벤션

> 기준: Microsoft C# 공식 가이드를 바탕으로, 그동안의 코드 작성 습관을 더했다. 공식 가이드와 다른 개인 규칙에는 `(개인 규칙)`을 표시한다.
>
> 예시에 나오는 클래스, 필드 이름은 설명용이며 실제 프로젝트에 있는 클래스가 아니다.

## 1. 네이밍

| 대상 | 규칙 | 예시 |
|---|---|---|
| 클래스 / 구조체 | PascalCase | `PlayerController`, `GoldManager` |
| enum 타입 | PascalCase, 단수 명사 (플래그 enum만 복수) | `TowerType`, `TileType` |
| enum 값 | PascalCase | `TowerType.ArrowTower` |
| 인터페이스 | `I` + PascalCase | `IDamageable` |
| 메서드 | PascalCase, 동사로 시작 | `TakeDamage()`, `InstallTower()` |
| public 프로퍼티 | PascalCase | `Gold`, `IsTopView` |
| private 필드 | `_` + camelCase | `_moveSpeed`, `_selectedTile` |
| private static 필드 | `s_` + camelCase | `s_instance` |
| 지역 변수 / 매개변수 | camelCase | `targetPos`, `amount` |
| 상수 (`const`, `static readonly`) | PascalCase (private이어도 `s_` 규칙보다 우선) | `StartGold`, `MaxTowerLevel` |
| bool | `is / has / can` 접두사 | `_isTopView`, `CanJump` |
| 이벤트 | `On` + 과거형·동사 (개인 규칙) | `OnGoldChanged`, `OnTileSelected` |
| 이벤트 핸들러 메서드 | `Handle` + 이벤트 이름에서 `On`을 뺀 것 | `HandleGoldChanged()` |

- 축약어는 피한다. (`plr` ✕ → `player` ○), 단 `Hp`, `UI`, `Id` 같은 관용어는 허용.
- 스크립트 파일명 = 클래스명. **한 파일에 public 클래스 하나.**
- 식별자 철자에 주의한다. (`SkillAblity` ✕ → `SkillAbility` ○) 이름은 한 번 퍼지면 고치기 어렵다. 직렬화 필드 이름을 바꿀 때는 `3. Inspector 노출`을 따른다.
- 타입을 이름에 넣지 않는다. (`_speedFloat` ✕) 타입은 선언으로, 의미는 이름으로 나타낸다.

## 2. enum
- 모든 enum은 `Assets/Scripts/Core/EnumList.cs` **한 파일에 모은다.** 어디서 쓰든 찾는 위치가 하나로 고정된다. 이 파일은 `1. 네이밍`의 "파일명 = 클래스명" 규칙의 예외다.
- enum을 추가하는 작업은 브리핑의 수정 파일에 `EnumList.cs`를 넣는다. 파일이 없으면 생성 파일에 넣는다.
- enum 타입 위에만 주석으로 용도를 적는다. 값마다 달 필요는 없다.
- "없음"이 필요하면 맨 앞에 `None = 0`으로 둔다. 값을 넣지 않은 enum 변수와 새로 추가한 `[SerializeField]` 필드의 기본값이 0이므로 자동으로 "없음"이 된다.
- Unity는 enum을 숫자로 저장한다. 그래서 모든 값에 번호를 직접 적고, 기존 번호는 바꾸거나 재사용하지 않는다. 바꾸면 이미 저장된 씬, 프리팹의 값이 조용히 다른 값으로 바뀐다.
- `[Flags]` enum은 값을 2의 거듭제곱(1, 2, 4, 8…)으로 둔다.
- enum 값을 배열 인덱스로 쓸 때는 `None`이 0번을 차지하므로, 변환하는 코드를 한 곳(메서드 하나)에 모은다.

```csharp
// 타워 타입
public enum TowerType
{
    None = 0,
    ArrowTower = 1,
    FireTower = 2,
    IceTower = 3,
}
```

## 3. Inspector 노출
- 필드는 `public`으로 열지 않는다. Inspector 노출은 `[SerializeField] private`을 사용한다.
- 외부에서 읽어야 하면 프로퍼티로 제공한다.
- 값의 의미는 필드 **바로 위 줄**에 주석으로 적는다. (9장 규칙)
- 직렬화 필드의 이름을 바꿀 때는 `[FormerlySerializedAs("_이전이름")]`(`UnityEngine.Serialization`)을 붙인다. 붙이지 않으면 Inspector에 넣어 둔 값이 사라진다. 관련 씬, 프리팹을 다시 저장한 뒤에는 지워도 된다.

```csharp
// 기본 이동 속도(초당 거리)
[SerializeField] private float _panSpeed = 15f;

// 따라가는 정도(클수록 딱딱함)
[SerializeField] private float _smoothSpeed = 10f;

public float PanSpeed => _panSpeed;
```

- 관련 필드가 많으면 `[Header("이동")]`으로 묶는다.

## 4. 클래스 내부 순서
1. 상수 / static 필드
2. `[SerializeField]` 필드 (Header로 그룹)
3. private 필드
4. 프로퍼티
5. 이벤트
6. Unity 생명주기 메서드 (`Awake → OnEnable → Start → Update → FixedUpdate → LateUpdate → OnDisable → OnDestroy` 순. 작성 순서이며 실행 순서와 다르다. 실제로는 FixedUpdate가 Update보다 먼저 실행된다)
7. public 메서드
8. private 메서드

- 필드는 클래스 위쪽에 모은다. 메서드 사이에 필드를 선언하지 않는다.

## 5. 생명주기 메서드
- `Awake`, `Start`, `Update` 등은 **`private`**으로 선언한다. 외부에서 호출할 일이 없고, `public`이면 다른 스크립트가 실수로 부를 수 있다.
  - 예외 : 상속해서 쓰는 부모 클래스는 `protected virtual`로 선언하고, 자식 클래스는 `override`한 뒤 `base.Awake()`처럼 부모 것을 호출한다. 부모와 자식이 둘 다 `private`으로 선언하면 부모 쪽 메서드가 실행되지 않는다.
- 하는 일이 메서드 하나로 위임될 때는 `=>` 한 줄로 쓴다. 생명주기마다 무슨 일을 하는지 한눈에 보인다.

```csharp
private void Awake() => CacheComponents();
private void OnEnable() => BindInputEvents();
private void Update() => MoveToGoal();
```

## 6. 포맷
- 중괄호는 새 줄에서 시작 (Allman 스타일).
- **가독성이 유지되면 중괄호를 생략해도 된다.** 기준:
  - ○ 조기 반환·건너뛰기: `if (!_isPlaying) return;`, `if (tile == null) continue;`
  - ○ 본문이 한 줄이고 바로 아래에 다른 `if`/`else`가 이어지지 않는 경우
  - ✕ `if`–`else`가 섞이거나 본문이 두 줄 이상이면 중괄호를 쓴다.
- 들여쓰기 공백 4칸.
- 필드는 변수 타입과 생성 타입이 같으면 `new()`를 쓴다. (`private List<Monster> _monsters = new();`)
- 지역 변수는 `var`와 생성 타입을 쓴다. (`var monsters = new List<Monster>();`)
- `var`는 오른쪽에서 타입이 분명할 때(`new`, 리터럴, 캐스트)만 쓴다.
  - ✕ `var gold = GetGold();` → `int gold = GetGold();` (메서드 이름만으로는 타입을 알 수 없다)
  - `foreach`의 반복 변수는 타입을 적는다. (`foreach (Monster monster in _monsters)`)

## 7. 파일 인코딩
- 모든 `.cs` 파일은 **UTF-8(BOM 없음)**로 저장한다.
- EUC-KR(CP949)로 저장되면 GitHub와 다른 PC에서 한글 주석이 `�⺻ �̵�`처럼 깨진다.
- 프로젝트 루트의 `.editorconfig`에 `[*.cs]` `charset = utf-8`을 지정해 에디터가 이 인코딩으로 저장하게 한다.
- Visual Studio에서 직접 저장할 때는 `파일 > 다른 이름으로 저장 > 인코딩하여 저장`에서 서명(BOM) 없는 UTF-8을 고른다.
- `.gitattributes`로는 BOM 유무를 보장할 수 없다. 이 프로젝트는 `.editorconfig`로 통일한다.

## 8. Unity 작성 원칙
- 자기 오브젝트의 컴포넌트(`GetComponent`)는 `Awake`에서, 다른 오브젝트 참조(`Find`류)는 `Start`에서 한 번만 가져와 캐싱한다. `Update` 안에서 호출하지 않는다.
- `GameObject.Find("이름")`, 문자열 태그 비교는 가급적 피하고 Inspector 참조나 `CompareTag()`를 사용한다.
- 물리 처리는 `FixedUpdate`, 입력 값 읽기·일반 로직은 `Update`에서 한다.
- 매직 넘버 금지 — 수치는 `[SerializeField]` 또는 상수로 뺀다.
- 밸런스 데이터(체력, 공격력, 웨이브 정보 등)는 `ScriptableObject`로 분리하는 것을 우선 고려한다.
- 자주 생성/파괴되는 오브젝트(총알, 이펙트)는 오브젝트 풀링을 고려한다.
- 싱글톤은 꼭 필요한 매니저(GameManager 등)에만 사용한다.
- 스크립트 간 결합은 직접 참조보다 이벤트(`event Action`)나 인터페이스를 우선한다.

### 입력 (Input System)
- 패키지와 설정은 `Docs/TechSpec.md` `3. 유저 입력 시스템`을 따른다.
- 입력 액션은 `InputActionReference`로 참조한다.
- 액션(맵)의 `Enable()` / `Disable()`은 한 곳에서만 한다. 그 한 곳은 `Docs/TechSpec.md` `3. 유저 입력 시스템`의 `액션 활성화 방식`을 따른다. (project-wide actions, 또는 입력 담당 클래스 하나)
  - 같은 액션을 여러 컴포넌트가 함께 쓰기 때문에, 한 컴포넌트가 `Disable()`하면 다른 컴포넌트의 입력도 같이 꺼진다.
- 입력을 사용하는 쪽은 `Enable()` / `Disable()`을 호출하지 않는다. `Update`에서 값을 읽거나, 콜백을 구독 / 해제한다.
- `PlayerInput` 컴포넌트와 `InputActionReference`를 같은 액션에 섞어 쓰지 않는다.

### 이벤트 구독
- 이벤트와 입력 콜백은 `OnEnable`에서 구독하고 `OnDisable`에서 해제한다. 둘을 항상 쌍으로 작성한다.
- 람다로 구독하지 않고 메서드를 넘겨 구독한다. (람다는 나중에 해제할 수 없다)

### null 검사
- `UnityEngine.Object`를 상속한 타입(`MonoBehaviour`, `GameObject` 등)에는 `?.`, `??`, `??=`, `is null`을 쓰지 않고 `== null`을 쓴다. 파괴된 오브젝트는 `== null`로는 null로 판정되지만, 위 문법은 이 판정을 건너뛴다.
- 인터페이스 타입 변수는 실제 클래스가 `UnityEngine.Object`를 상속했을 수도 있으므로 `x == null || (x is UnityEngine.Object o && o == null)`로 검사한다.

### Unity 6 API
- Obsolete(사용 중단) 경고가 나는 API는 쓰지 않는다.
- `Docs/TechSpec.md` `2. Unity 환경`의 버전이 Unity 6(`6000.x`) 이상이면 아래 대응을 따른다.
  - `velocity` → `linearVelocity`
  - `drag` / `angularDrag` → `linearDamping` / `angularDamping`
  - `FindObjectOfType` → `FindFirstObjectByType` / `FindAnyObjectByType`
  - `FindObjectsOfType` → `FindObjectsByType` (정렬이 필요 없으면 `FindObjectsSortMode.None`)

## 9. 주석
- 주석은 **한국어**로 작성한다.
- `//` 일반 주석만 쓴다. XML 주석(`///`)과 `[Tooltip]`은 쓰지 않는다.
- 설명할 대상(클래스, 메서드, 필드, enum, 코드 줄)의 **바로 위 줄**에 적는다. 줄 끝에 적지 않는다.
- **한 줄, 명사형**으로 끝낸다.
- 코드만 봐서 이유를 알기 어려운 곳에만 이유를 같은 형식으로 남긴다. 코드만 봐도 아는 곳에는 남기지 않는다.

```csharp
// 게임 전체 골드 관리
public class GoldManager : MonoBehaviour
{
    // 골드 추가 및 변경 이벤트 알림
    public void AddGold(int amount) { ... }

    // 탑뷰 카메라 맵 밖 이동 방지
    private void ClampToMapBounds() { ... }
}

// 같은 프레임 중복 이동 방지
_stateMachine.ChangeState(_idleState);
return;
```

- 표시용 주석:
  - `// TODO:` 나중에 할 일
  - `// FIXME:` 알려진 문제
  - `// 테스트 로그` 지워야 할 `Debug.Log`

## 10. 불필요 코드
- 불필요한 코드, 쓰지 않는 변수, 빈 메서드는 만들지 않는다.
