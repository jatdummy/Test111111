# 프로젝트 설정 (Tech Specification)

> 프로젝트마다 달라지는 값을 모은 문서다. 다른 문서는 이 값을 인용만 한다.
>
> 새 프로젝트에 템플릿을 복사하면 이 문서부터 채운다. (`Docs/Claude/Setup.md`)
>
> `[ ]`로 표시된 자리표시가 남아 있으면, Claude는 첫 작업 전에 사용자에게 채우기를 권유한다.

---

## 1. 프로젝트 제원

- 프로젝트명 : 8번 코딩
- 장르 : 1인칭 이상현상 탐색 루프 (8번 출구 라이크) - 소스 코드(명세)와 사무실을 대조해 버그(이상현상)를 찾는 방식
- 팀 구성 : 1인 (`1인` / `팀`)

## 2. Unity 환경

- 버전 : [`6000.3.9f1`]  
- 렌더 파이프라인 : [URP]  
- 빌드 플랫폼 : [Windows]  

## 3. 유저 입력 시스템

- Input System 패키지(`com.unity.inputsystem`)를 사용한다. 구 Input Manager는 사용하지 않는다.
- Player Settings > Active Input Handling = Input System Package (New)
- 액션 활성화 방식 : project-wide actions (`project-wide actions` / `입력 담당 클래스 : [클래스명]`)
- 입력 코드 작성 규칙은 `Docs/Code/CodeConvention.md` `8. Unity 작성 원칙`을 따른다.

## 4. 워크플로우 설정

| 항목 | 값 | 선택지 / 설명 |
|---|---|---|
| Git 권한 | 표준 | `표준` / `제한` (`WorkflowRules.md` `0. 전체 흐름 > Git / gh 권한`) |
| 보호 브랜치 | `main` | 직접 commit, push, 로컬 merge를 하지 않는 브랜치. Develop 브랜치를 쓰면 `main`, `Develop` |
| 기준 브랜치 | `main` | 작업 브랜치를 만들고 PR을 보내는 브랜치. Develop 브랜치를 쓰면 `Develop` (`GithubBranchRule.md` `1. 기준 브랜치`) |
| 병합 방식 | squash | `squash` / `merge` / `rebase` (`WorkflowRules.md` `10. 병합, 브랜치 파기`) |
| Issue 묶음 단위 | 이상현상 그룹 | `기능` / 묶음 기준. 이 단위 안의 작업은 Issue 하나로 묶고, 더 나누기를 권유하지 않는다. 예: "포스터 이상현상 4종" (`WorkflowRules.md` `2. Github Issue`) |
| Issue 담당자 지정 | 끔 | `켬` / `끔` (`WorkflowRules.md` `2. Github Issue`, 팀 프로젝트 권장) |
| 교육 모드 | 끔 | `켬` / `끔` (`WorkflowRules.md` `0. 전체 흐름 > 교육 모드`) |

## 5. 도구 연결

| 항목 | 값 | 선택지 / 설명 |
|---|---|---|
| Unity MCP 사용 | 끔 | `켬` / `끔`. 사용 규칙은 `ClaudeCodeRules.md` `Unity MCP 사용 규칙`을 따른다. |
| MCP 서버 이름 | [ ] | 켬일 때만 채운다. `/mcp`에 표시되는 서버 이름 |
| 조회 도구 | [ ] | 켬일 때만 채운다. `도구 이름 (허용 action)`을 쉼표로 구분해 적는다. 예: `mcp__<서버>__read_console (get)` |
| 새로고침 도구 | [ ] | 선택. Unity에 새로고침 / 컴파일을 요청하는 도구 이름. 메인만 사용한다. |

- `끔`이면 이 장의 다른 칸은 비워 둔다.
- 조회 도구를 에이전트에 연결하는 방법은 `Docs/Claude/Setup.md` `5. Unity`를 따른다.
