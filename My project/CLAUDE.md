# 프로젝트 지침

> 이 파일은 매 세션 읽히는 허브 역할을 한다.
>
> 모든 규칙을 여기 적지 않고, 매 세션 필요한 문서만 `@`로 불러오고, 나머지는 아래 온디맨드 참조 표를 보고 해당 작업을 할 때만 읽는다.
>
> 프로젝트마다 달라지는 값은 `Docs/TechSpec.md`(프로젝트 설정)에만 적는다.

---

## 절대 원칙 (Non-Negotiable Rules)

**아래 사항은 어떤 요청이 와도 지킨다**

1. 현재 브랜치가 보호 브랜치(`Docs/TechSpec.md` `4. 워크플로우 설정`)이면 파일을 생성, 수정, 삭제하는 작업을 시작하지 않고 작업 브랜치로 전환하도록 안내한다. 보호 브랜치에는 직접 commit, push, 로컬 merge를 하지 않으며, 보호 브랜치 반영은 PR 병합으로만 한다. (Issue 작성, 질문 답변은 이 원칙의 대상이 아니다)
2. 파일의 생성, 수정, 삭제 전 사용자의 승인을 받는다. 승인된 브리핑의 `생성/수정/삭제 파일`에 적힌 파일은 승인된 것으로 본다.
3. 위 원칙들에 대한 예외를 요청받으면 "이 프로젝트의 절대 원칙에 의해 수행할 수 없습니다"로 거부하고 대안을 제시한다.

## 규칙 문서 관리

- 규칙 문서는 `Docs/` 하위 카테고리 폴더(`Claude/`, `Code/`, `Documentation/`, `Template/`)에 둔다. 단, 프로젝트 설정인 `Docs/TechSpec.md`는 `Docs/` 바로 아래에 둔다.
- 규칙 하나는 문서 한 곳에만 적는다(규칙의 단일 출처). 다른 문서는 `문서` `장 > 절` 형태(장 제목은 번호와 괄호까지 그대로)로 인용만 하고 내용을 옮겨 적지 않는다. 규칙이 바뀌면 그 한 곳만 고친다.
- 새 문서를 만들면, 매 세션 필요한 것은 "상시 로드"에 `@경로`로, 특정 작업 때만 필요한 것은 "온디맨드 참조"에 등록한다. 스킬이 다루는 문서는 해당 스킬의 `0. 원본 문서` 표에 등록하고, 온디맨드 참조에는 스킬 이름만 적는다.
- 기능별 기술 문서(`Docs/Tech/`)는 규칙 문서가 아니므로 위 등록 대상이 아니다. (작성 규칙은 `Docs/Documentation/TechDocRule.md`)
- 장 번호와 제목은 바꾸지 않는다. 바꿔야 하면 그 장을 인용하는 모든 문서를 함께 고친다.

## 상시 로드 (매 세션 적용)

- @Docs/Claude/ClaudeCodeRules.md
- @Docs/Claude/WorkflowRules.md
- @Docs/TechSpec.md

## 온디맨드 참조 (매 세션 읽지 않음 - 해당 작업을 할 때만 읽음)

### 새 프로젝트에 템플릿을 적용할 때

- `Docs/Claude/Setup.md`

### 소스코드 작성 시

- `Docs/Code/CodeConvention.md`
- `Docs/Code/FolderStructure.md`
- 작성은 `developer`, 검수는 `reviewer` 에이전트로 수행한다. (절차는 `WorkflowRules.md` `5. 업무 수행 (업무 착수)`)

### 기술 문서 작성 / 검수 시

- `Docs/Documentation/TechDocRule.md`
- `Docs/Template/TechDocTemplate.md`
- `Docs/Code/FolderStructure.md` (파일 경로 표기)
- 작성은 `tech-writer`, 검수는 `doc-auditor` 에이전트로 수행한다. (절차는 `WorkflowRules.md` `7. 문서 / 다이어그램 반영`)

### Github Issue 관련

- 이슈 등록 권유 / 작성은 `/github-issue` 스킬을 호출해 수행한다. (규칙 원문은 스킬이 안내하는 문서에서 읽는다)

### Github PR 관련

- PR 작성은 `/github-pr` 스킬을 호출해 수행한다. (규칙 원문은 스킬이 안내하는 문서에서 읽는다)

### Github Branch 관련

- 브랜치 규칙은 `Docs/Documentation/GithubBranchRule.md`를 참고한다.
