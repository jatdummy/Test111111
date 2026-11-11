---
name: github-pr
description: Github PR 작성 절차. 사용자가 PR(풀 리퀘스트) 작성이나 생성을 요청한 경우 사용한다. PR을 만들 때는 이 스킬 없이 바로 gh pr create를 실행하지 않는다.
---

# Github PR 작성

> Github PR 작성 시 Claude가 따르는 절차와 멘트 규격
>
> 이 스킬은 규칙을 정의하지 않는다. 규칙은 아래 원본 문서에만 있으며, 스킬은 읽을 위치와 수행 순서만 안내한다.

---

## 0. 원본 문서

| 문서 | 읽을 섹션 | 용도 |
|---|---|---|
| `Docs/Claude/WorkflowRules.md` | `0. 전체 흐름`(`Git / gh 권한`), `5. 업무 수행 (업무 착수)`, `9. PR` | 사용 가능한 명령, 변경 파일 목록 기준, 생성 조건 |
| `Docs/Documentation/GithubPRRule.md` | 전체 (`1. Title` ~ `7. 특이사항`) | 섹션별 작성 규칙, DoD 판정 |
| `Docs/Documentation/GithubIssueRule.md` | `5. 완료조건 (DoD)`, `7. 기타` | DoD 항목 구분, 말머리 대응표 |
| `Docs/Template/PRTemplate.md` | 양식 코드블록 | PR 본문 양식 |
| `Docs/Documentation/GithubBranchRule.md` | `1. 기준 브랜치` | PR 대상 브랜치 |
| `Docs/TechSpec.md` | `1. 프로젝트 제원`, `4. 워크플로우 설정`, `5. 도구 연결` | 팀 구성, 기준 브랜치, Unity MCP 사용 여부 |
| `Docs/Claude/ClaudeCodeRules.md` | `Unity MCP 사용 규칙` | MCP가 켬일 때 콘솔 재조회 |

- 각 단계에서 표기된 섹션만 읽는다.
- 원본 문서와 이 스킬의 내용이 다르면, 원본 문서를 따르고 사용자에게 차이를 보고한다.

---

## 1. 정보 수집

- 연결된 Issue 번호와 Label을 확인한다. (`gh issue view`) 알 수 없으면 사용자에게 질문한다.
- 현재 브랜치(`git branch --show-current`)와 원격 반영 상태를 확인한다.
  - `git rev-parse --abbrev-ref @{u}`가 실패하면 원격에 올라가지 않은 브랜치다. `git push -u origin <작업 브랜치>`를 안내한다.
  - 업스트림이 있으면 `git status -sb`의 ahead 표시로 push하지 않은 커밋이 있는지 확인하고, 있으면 push를 안내한다.
  - push는 `WorkflowRules.md` `0. 전체 흐름 > Git / gh 권한`에 따라 대행할 수 있다.
- 변경 파일 목록은 `WorkflowRules.md`의 `9. PR`에 따라 확인하고, 검수 보고와 대조한다.
- DoD 판정을 준비한다. 판정 기준은 `GithubPRRule.md` `2. 연결 Issue`를 따른다.
  - Unity MCP가 켬이면 이 단계에서 콘솔을 다시 조회한다. `검증` 등급이면 콘솔 에러 항목은 묻지 않는다.
  - `(사용자 확인)` 항목, `미검증` 항목, PR 확인 사항의 사용자 항목(실행 테스트, `검증` 등급이 아닌 콘솔 에러)은 멘트 A에서 묻는다. `GithubPRRule.md` `4. 확인 사항`에서 `(해당 없음)`인 항목(예: 문서만 바뀐 PR의 실행 테스트)은 묻지 않는다.
- 아래 **멘트 A** 로 확인받는다.

**멘트 A : PR 작성 정보 확인**

```markdown
**PR 작성 정보 확인** : [작업 브랜치] → [기준 브랜치]

- 연결 Issue : [#번호 / 없음]
- 변경 파일 (diff 기준) :
  - 생성 : [경로]
  - 수정 : [경로]
  - 삭제 : [경로]
- 검수 보고와 다른 파일 : [경로 / 없음]
- DoD :
  - Claude 확인 : [항목 - 충족 / 미충족]
  - 사용자 확인 필요 : [항목]

사용자 확인 항목을 확인하셨나요? (확인한 항목을 알려주세요)
```

---

## 2. 초안 작성

- `PRTemplate.md`의 양식으로 본문을 작성한다.
- 각 섹션은 `GithubPRRule.md`의 해당 섹션 규칙에 따라 작성한다.
  - 연결 Issue는 DoD 판정 결과에 따라 `Closes` / `Refs`로 적는다.
- 작성 대상의 코드 / 문서를 확인해야 하는 경우, 읽기만 하며 수정하지 않는다.

---

## 3. 생성 전 승인

- 승인 요청에는 아래 **멘트 B** 의 요약만 보여준다.
- 사용자가 전문을 요청하면 본문 전체를 보여준다.

**멘트 B : 생성 승인 요청**

```markdown
**PR 생성 확인**

| 항목 | 내용 |
|---|---|
| 제목 | [말머리] [제목] |
| 브랜치 | [작업 브랜치] → [기준 브랜치] |
| 연결 Issue | [Closes #번호 / Refs #번호 / 없음] |
| 작업 내용 | [항목 n개] |
| 체크하지 않은 확인 사항 | [항목 / 없음] |

생성할까요? (전문 확인 시 "전문")
```

---

## 4. 생성

- `gh pr create --base <기준 브랜치> --head <작업 브랜치> --title "<제목>" --body-file -`로 PR을 생성한다. 본문은 표준 입력으로 넘긴다.
- 생성 후 PR 번호와 링크를 보고한다.
- 병합은 `WorkflowRules.md`의 `10. 병합, 브랜치 파기`를 따른다.

**멘트 C : 생성 완료**

```markdown
**PR 생성 완료** : #[번호] [제목]

- 링크 : [URL]
- 체크하지 않은 확인 사항 : [항목 / 없음]

다음 단계 : 병합 (사용자. 대행 여부는 `WorkflowRules.md` `0. 전체 흐름 > Git / gh 권한`)
```
