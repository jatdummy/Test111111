# 폴더 구조

```
Assets/
├── Scripts/
│   ├── Core/            # LoopManager, 공용 유틸, EnumList.cs
│   ├── Player/
│   ├── Enemy/
│   ├── UI/
│   ├── Data/            # ScriptableObject 정의
│   └── Editor/          # UnityEditor를 사용하는 에디터 전용 스크립트
├── Input/               # Input Action 에셋 (.inputactions, project-wide actions 포함)
├── Data/                # ScriptableObject 에셋(.asset)
├── Prefabs/
│   └── Player/          # 기능별 하위 폴더
├── Scenes/
│   └── Test/            # 기능 테스트용 씬
├── Materials/
├── Animations/
├── Audio/
├── Models/
├── Plugins/
├── Settings/            # 렌더 파이프라인 설정 (Unity 생성, Docs/TechSpec.md 2. Unity 환경)
├── TextMesh Pro/        # TMP Essential Resources (Unity 생성 폴더, 옮기지 않음)
└── Imports/             # 외부 에셋 스토어 에셋은 전부 여기 (직접 만든 에셋과 분리)
    └── Fonts/           # 외부 폰트 원본과 그 폰트로 만든 TMP 폰트 에셋
```

## 규칙
- 폴더 트리는 기본 구조다. 프로젝트에 필요 없는 폴더는 만들지 않아도 된다.
- 직접 만든 에셋은 `Assets/` 바로 아래의 용도별 폴더에 둔다.
- 에셋 스토어 등 외부 에셋은 모두 `Assets/Imports/` 아래에 둔다. 직접 만든 에셋과 섞이지 않게 하기 위해서다.
  - 예외 : 경로가 코드에 고정된 에셋이나 다시 임포트하면 원래 위치에 생기는 에셋(TMP Essential 등)은 옮겼을 때 깨질 수 있다. 이런 에셋은 원래 위치에 둔다.
- 새 스크립트는 기능별 폴더에 넣는다. 폴더가 애매하면 AI가 위치를 제안한다.
- `UnityEditor`를 사용하는 스크립트는 `Assets/Scripts/Editor/`에 둔다. 다른 폴더에 두면 에디터에서는 동작하지만 빌드가 실패한다. asmdef를 쓰는 경우에는 Editor 전용 asmdef로 분리한다.
- `Resources/` 폴더는 꼭 필요한 경우에만 쓰고, 우선 Inspector 참조를 사용한다.
- `StreamingAssets/`는 원본 파일 그대로 읽어야 하는 데이터에만 쓴다.
- Input Action 에셋은 `Assets/Input/`에 둔다. project-wide actions 에셋도 여기에 두고 Project Settings에서 지정한다. Unity가 다른 위치에 기본 에셋을 만들었다면 사용자가 에디터에서 옮긴다. (`Docs/TechSpec.md` `3. 유저 입력 시스템`)
- 기능 테스트는 `Assets/Scenes/Test/`의 테스트 씬에서 한다. (메인 씬 충돌·오염 방지)
- 폴더 이름은 PascalCase, 영어로 짓는다. 단, 패키지가 생성한 폴더(`TextMesh Pro` 등)는 이름을 바꾸지 않는다.
- 에셋·폴더를 옮기거나 이름을 바꾸거나 지울 때는 **Unity 에디터 안에서** 한다. 탐색기에서 `.meta` 파일 없이 옮기면 Unity가 새 GUID를 만들어 기존 참조가 끊어진다.
- Claude는 에셋·폴더의 이동, 이름 변경, 삭제를 직접 하지 않는다. 필요하면 사용자에게 요청한다.
