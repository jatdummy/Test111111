# 새 프로젝트 셋업

> 이 템플릿을 새 Unity 프로젝트에 적용할 때의 체크리스트.
>
> 이 셋업은 사용자가 수행한다. 아직 작업 브랜치가 없으므로 Claude는 보호 브랜치에서 파일을 고치지 않는다. (`CLAUDE.md` 절대 원칙 1)
>
> 조직(회사, 교육기관 등)의 정책이 적용되는 계정에서는 조직 정책이 이 템플릿보다 우선한다.

---

## 1. 파일 배치

- [ ] `CLAUDE.md`를 프로젝트 루트(`Assets` 폴더와 같은 위치)에 둔다.
- [ ] `Docs/`, `.claude/agents/`, `.claude/skills/`를 프로젝트 루트에 복사한다.
- [ ] 기술 문서 저장 위치인 `Docs/Tech/` 폴더를 만든다.

## 2. 프로젝트 설정 채우기

- [ ] `Docs/TechSpec.md`의 `[ ]` 자리표시를 모두 채운다.
- [ ] `Docs/TechSpec.md` `4. 워크플로우 설정`을 프로젝트에 맞게 고른다. (팀 프로젝트면 `Issue 담당자 지정`, Develop 브랜치를 쓰면 `보호 브랜치`, `기준 브랜치` 확인)

## 3. Git 저장소

- [ ] Unity용 `.gitignore`(`Library/`, `Temp/`, `Logs/`, `UserSettings/` 등 제외)를 추가한다.
- [ ] 템플릿을 포함한 초기 커밋을 만들어 원격 `main`에 push한다.
- [ ] `git ls-remote origin main`으로 원격 `main`이 있는지 확인한다.
- [ ] Develop 브랜치를 쓰면 `Develop`을 만들어 push하고, GitHub 기본 브랜치를 무엇으로 할지 정한다. (기본 브랜치가 아닌 곳으로 병합하면 `Closes`로 Issue가 자동으로 닫히지 않는다)

## 4. Claude Code 확인

- [ ] 세션에서 `/memory`로 상시 로드 문서 3개(`ClaudeCodeRules.md`, `WorkflowRules.md`, `TechSpec.md`)가 로드됐는지 확인한다.
- [ ] `/agents`로 에이전트 4개(`developer`, `reviewer`, `tech-writer`, `doc-auditor`)가 보이는지 확인한다.

## 5. Unity

- [ ] (MCP를 쓸 때) Unity에 MCP 패키지를 설치하고, MCP 패널에서 Claude Code를 Configure한다. 등록 결과가 프로젝트 루트의 `.mcp.json`인지 확인하고, 아니면 `/mcp`에 표시되는 이름을 쓴다.
- [ ] (MCP를 쓸 때) `.mcp.json`으로 등록했다면 Claude Code 첫 실행 때 서버 사용을 승인한다. (또는 `.claude/settings.json`의 `enabledMcpjsonServers`에 서버 이름을 넣는다)
- [ ] (MCP를 쓸 때) `/mcp`에서 서버가 connected인지 확인한다. 상태를 바꾸지 않는 조회 도구(콘솔 로그 읽기, 씬 / 컴포넌트 조회, 에디터 상태(컴파일 중 여부) 조회)의 실제 이름과 조회 action을 확인한다. 조회 전용 도구가 따로 있으면 그것을 우선 고른다.
- [ ] (MCP를 쓸 때) `Docs/TechSpec.md` `5. 도구 연결`을 채운다.
- [ ] (MCP를 쓸 때) 조회 도구 이름(action 표기는 빼고)을 `.claude/agents/developer.md`, `.claude/agents/reviewer.md`의 `tools` 끝에 쉼표로 이어 붙인다. (예: `tools: Read, Glob, Grep, mcp__<서버>__read_console`)
- [ ] Unity가 만든 기본 Input Action 에셋이 `Assets/Input/`이 아닌 곳에 있으면 에디터에서 옮기고, project-wide actions 지정 상태를 Project Settings에서 확인한다. (`Docs/Code/FolderStructure.md` `규칙`)

## 6. GitHub

- [ ] `gh auth status`로 로그인을 확인한다.
- [ ] `git remote -v`로 원격 저장소 연결을 확인한다.
- [ ] `Docs/Documentation/GithubIssueRule.md` `7. 기타`의 Label을 등록한다. (없으면 Issue 등록 때 생성된다)
- [ ] (선택) GitHub에서 보호 브랜치 규칙을 설정한다.

## 7. 공유 설정 (선택)

- [ ] `.claude/settings.json`의 `permissions`에 `WorkflowRules.md` `0. 전체 흐름 > Git / gh 권한`을 반영한다.
  - `표준` : 상시 허용 = allow, 대행 = ask, 금지 = deny
  - `제한` : 상시 허용 = allow, 대행 = deny, 금지 = deny
  - Unity MCP를 쓰면 : 조회 action만 있는 도구만 도구 이름 단위로 allow에 넣는다. 조회와 변경 action이 섞인 도구는 넣지 않아 확인 창이 뜨게 둔다. 나머지 MCP 도구는 등록하지 않아 매번 확인 창이 뜨게 둔다. 서버 단위로 allow나 ask를 넣지 않는다. (`ClaudeCodeRules.md` `Unity MCP 사용 규칙`)
  - 패턴 문법은 Claude Code 공식 문서로 확인한다. deny 패턴은 다른 표기로 우회될 수 있으므로 규칙 문서를 대신하지 않는다.
- [ ] `.editorconfig`에 `[*.cs]` `charset = utf-8`을 지정한다. (`Docs/Code/CodeConvention.md` `7. 파일 인코딩`)

## 8. 첫 작업으로 확인

- [ ] 간단한 기능 하나로 Issue → 브랜치 생성 → 브리핑 → 업무 수행 → 사용자 검수 → PR → 병합까지 한 번 끝까지 진행해 본다.
