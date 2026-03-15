---
title: Git & GitHub 협업 가이드
date: 2026-03-15
tags:
  - type/reference
  - tool/git
  - tool/github
  - subject/협업
aliases: [깃 협업, 깃허브 가이드]
---

# Git & GitHub 협업 가이드

---

## 1. 핵심 개념

### Git이란?

코드의 변경 이력을 기록하는 **버전 관리 도구**. 로컬(내 컴퓨터)에서 동작한다.

**왜 필요한가:**
- 코드를 이전 상태로 되돌릴 수 있음
- 누가 언제 뭘 바꿨는지 추적 가능
- 여러 명이 동시에 작업해도 코드를 안전하게 합칠 수 있음

### GitHub이란?

Git 저장소를 인터넷에 올려두는 **원격 저장소 서비스**. 팀원 간 코드 공유와 협업의 허브 역할.

> 내 컴퓨터 (로컬) ←── push/pull ──→ GitHub (원격) ←── push/pull ──→ 팀원 컴퓨터 (로컬)

### 핵심 용어 정리

| 용어 | 의미 | 비유 |
|------|------|------|
| **Repository (레포)** | 프로젝트 저장소 | 프로젝트 폴더 전체 |
| **Commit** | 코드 변경사항의 스냅샷 | 세이브 포인트 |
| **Branch** | 독립된 작업 공간 | 평행 세계 |
| **main** | 기본 브랜치, 항상 안정된 코드 | 정식 출시 버전 |
| **Push** | 로컬 → GitHub으로 업로드 | 클라우드에 백업 |
| **Pull** | GitHub → 로컬로 다운로드 | 최신 버전 동기화 |
| **Clone** | GitHub 레포를 로컬에 복사 | 프로젝트 첫 다운로드 |
| **PR (Pull Request)** | 브랜치를 main에 합쳐달라는 요청 | 코드 리뷰 + 머지 요청 |
| **Merge** | 브랜치를 합치는 것 | 평행 세계를 하나로 합침 |
| **Conflict** | 같은 부분을 다르게 수정한 충돌 | 두 사람이 같은 줄을 고침 |

---

## 2. 최초 프로젝트 셋업

### 2-1. 로컬 프로젝트 생성 후 Git 초기화

```bash
# 프로젝트 폴더에서
git init                              # Git 추적 시작 (.git 폴더 생성)
git add .                             # 모든 파일을 스냅샷 대상에 추가
git commit -m "chore: 프로젝트 초기 세팅"  # 첫 번째 스냅샷 저장
```

**왜 이 순서인가:**
- `git init` → "이 폴더를 Git으로 관리하겠다" 선언
- `git add .` → "이 파일들을 다음 커밋에 포함시켜" (`.gitignore`에 적힌 파일은 자동 제외)
- `git commit` → 현재 상태를 하나의 스냅샷으로 저장

### 2-2. GitHub 빈 레포 생성

1. GitHub → 오른쪽 위 `+` → `New repository`
2. Repository name 입력
3. **README 체크 해제** ⚠️
4. **.gitignore 체크 해제** ⚠️
5. Create repository

> **왜 빈 레포로 만드는가:**
> GitHub에서 README를 만들면 GitHub에 커밋 1개가 생기고, 로컬에도 커밋 1개가 있어서 서로 다른 이력이 된다. 합치려면 충돌 해결이 필요해져서 번거로움. 빈 레포면 이 문제가 없다.

### 2-3. 로컬과 GitHub 연결

```bash
git branch -M main                    # 기본 브랜치 이름을 main으로 통일
git remote add origin [GitHub URL]    # 로컬 ↔ GitHub 연결
git push -u origin main               # 로컬 코드를 GitHub에 업로드
```

**각 명령어 의미:**
- `git branch -M main` → Git 기본값이 `master`일 수 있어서 `main`으로 통일
- `git remote add origin [URL]` → "내 로컬 Git을 이 GitHub 주소와 연결해". `origin`은 원격 저장소의 별명 (관례)
- `git push -u origin main` → 로컬의 main을 GitHub에 업로드. `-u`는 "앞으로 `git push`만 쳐도 이 주소로 보내"라는 기본값 설정

### 2-4. 팀원 초대 + 클론

**레포 주인:**
GitHub 레포 → `Settings` → `Collaborators` → `Add people` → 팀원 GitHub 아이디 입력

**팀원:**
```bash
git clone [GitHub URL]      # 레포 전체를 내 컴퓨터에 복사
cd 프로젝트폴더              # 복사된 폴더로 이동
```

---

## 3. 브랜치 전략

### 3-1. 왜 브랜치를 쓰는가?

`main`에서 직접 작업하면:
- 두 사람이 동시에 같은 파일을 수정하면 충돌이 계속 남
- 버그 있는 코드가 바로 main에 반영됨
- 누가 뭘 작업하는지 구분이 안 됨

브랜치를 쓰면:
- 각자 독립된 공간에서 작업 → 서로 안 건드림
- 완성된 기능만 PR로 main에 합침 → main은 항상 안정
- 브랜치 이름으로 "누가 뭘 하는지" 바로 파악

### 3-2. 브랜치 네이밍 규칙

```
feat/기능명       → 새 기능 개발
fix/버그명        → 버그 수정
docs/문서명       → 문서 작업
refactor/대상    → 리팩토링
chore/작업명      → 기타 잡일 (설정 변경 등)
```

**예시:**
```
feat/room-create       → 방 생성 기능
feat/game-loop         → 게임 루프 구현
fix/unit-transfer-bug  → 유닛 전송 버그 수정
docs/api-spec          → API 명세 문서 작성
```

### 3-3. 브랜치 흐름

> main ─────────────────────────────── 항상 배포 가능 상태
>   │                         ↑
>   ├── feat/room-create ─────┘ (PR → 리뷰 → 머지)
>   │                              ↑
>   ├── feat/game-loop ────────────┘ (PR → 리뷰 → 머지)

---

## 4. 일상적인 협업 흐름

### 4-1. 작업 시작 전 (매번)

```bash
git checkout main          # main 브랜치로 이동
git pull origin main       # GitHub에서 최신 코드 가져오기
```

> ⚠️ 이걸 안 하면 팀원이 머지한 코드를 모른 채 옛날 코드 위에서 작업하게 됨 → 나중에 충돌 지옥

### 4-2. 기능 개발

```bash
# 1. 브랜치 생성 + 이동
git checkout -b feat/room-create

# 2. 코드 작업 ... (파일 수정, 생성, 삭제)

# 3. 변경사항 확인
git status                 # 뭐가 바뀌었는지 확인
git diff                   # 구체적으로 어떤 줄이 바뀌었는지

# 4. 커밋
git add .                  # 모든 변경사항 스테이징
git commit -m "feat: 방 생성 로직 구현"

# 5. GitHub에 업로드
git push origin feat/room-create
```

### 4-3. PR(Pull Request) 생성 + 머지

**GitHub 웹사이트에서:**
1. 브랜치를 push하면 `Compare & pull request` 버튼이 뜸
2. PR 제목 + 설명 작성 (뭘 구현했는지)
3. 팀원이 코드를 훑어보고 코멘트 or 승인
4. `Merge pull request` 클릭 → main에 합쳐짐
5. 머지된 브랜치 삭제 (`Delete branch` 버튼)

**왜 PR을 쓰는가:**
- main에 바로 push하면 버그 있는 코드가 즉시 반영됨
- PR을 거치면 최소 한 명이 확인 → 실수를 잡을 수 있음
- 나중에 "이 기능이 언제 어떻게 들어왔지?" 추적 가능

### 4-4. 머지 후 정리

```bash
git checkout main                       # main으로 돌아오기
git pull origin main                    # 머지된 최신 코드 가져오기
git branch -d feat/room-create          # 로컬 브랜치 삭제 (이미 머지됐으니 불필요)
```

---

## 5. 커밋 컨벤션

### 5-1. 커밋 메시지 형식

```
타입: 설명 (한글 OK)
```

### 5-2. 타입 종류

| 타입 | 용도 | 예시 |
|------|------|------|
| `feat` | 새 기능 | `feat: 방 생성 로직 구현` |
| `fix` | 버그 수정 | `fix: 유닛 전송 시 음수 버그 수정` |
| `docs` | 문서 | `docs: API 명세 업데이트` |
| `refactor` | 리팩토링 | `refactor: GameService 책임 분리` |
| `style` | 포맷팅 | `style: 코드 들여쓰기 정리` |
| `chore` | 기타 잡일 | `chore: .gitignore 업데이트` |
| `test` | 테스트 | `test: 전투 로직 단위 테스트 추가` |

### 5-3. 좋은 커밋 vs 나쁜 커밋

```bash
# 나쁜 예
git commit -m "수정"
git commit -m "작업 중"

# 좋은 예
git commit -m "feat: SignalR 허브 기본 연결 구현"
git commit -m "fix: 방 코드 중복 생성 버그 수정"
git commit -m "refactor: RoomService에서 게임 로직 분리"
```

---

## 6. 자주 쓰는 명령어 모음

### 기본 명령어

| 명령어 | 설명 |
|--------|------|
| `git status` | 현재 변경사항 확인 |
| `git log --oneline` | 커밋 이력 한 줄씩 보기 |
| `git diff` | 변경된 코드 구체적으로 보기 |
| `git branch` | 로컬 브랜치 목록 보기 |
| `git branch -a` | 원격 포함 전체 브랜치 보기 |

### 브랜치 관련

| 명령어 | 설명 |
|--------|------|
| `git checkout -b 브랜치명` | 새 브랜치 생성 + 이동 |
| `git checkout 브랜치명` | 해당 브랜치로 이동 |
| `git branch -d 브랜치명` | 브랜치 삭제 (머지 후) |

### 동기화 관련

| 명령어 | 설명 |
|--------|------|
| `git pull origin main` | GitHub에서 최신 코드 가져오기 |
| `git push origin 브랜치명` | 로컬 브랜치를 GitHub에 업로드 |

### 실수 복구

| 명령어 | 설명 |
|--------|------|
| `git checkout -- 파일명` | 수정한 파일을 마지막 커밋 상태로 되돌리기 |
| `git reset HEAD 파일명` | add 취소 (스테이징에서 제거) |
| `git reset --soft HEAD~1` | 마지막 커밋 취소 (코드는 유지) |
| `git stash` | 작업 중인 변경사항 임시 저장 |
| `git stash pop` | 임시 저장한 변경사항 복원 |

---

## 7. 충돌(Conflict) 해결

### 7-1. 충돌은 왜 발생하는가?

두 사람이 **같은 파일의 같은 부분**을 다르게 수정한 뒤 합치려 할 때 발생. Git이 "둘 중 뭘 살릴지 모르겠다"는 뜻.

### 7-2. 충돌이 발생하면?

```
<<<<<<< HEAD
내가 작성한 코드
=======
팀원이 작성한 코드
>>>>>>> feat/other-branch
```

### 7-3. 해결 방법

1. 충돌 파일 열기
2. `<<<`, `===`, `>>>` 마커 확인
3. 둘 중 하나를 선택하거나, 둘을 합쳐서 수정
4. 마커(`<<<`, `===`, `>>>`) 전부 삭제
5. 저장 후 커밋

```bash
git add .
git commit -m "fix: merge conflict 해결"
```

### 7-4. 충돌 예방법

- **작업 시작 전 항상 `git pull`**
- 같은 파일을 동시에 수정하지 않도록 역할 분담
- 브랜치를 오래 두지 말고 빠르게 PR → 머지
- 큰 기능은 작은 단위로 쪼개서 자주 머지

---

## 8. 프로젝트별 설정

### MintAndHeart 프로젝트 규칙

| 항목 | 규칙 |
|------|------|
| 기본 브랜치 | `main` |
| main 직접 push | **금지** (PR만 허용) |
| 브랜치 네이밍 | `feat/`, `fix/`, `docs/`, `refactor/`, `chore/` |
| PR 리뷰 | 상대방 1명 확인 후 머지 |
| 머지 후 | 브랜치 삭제 |
| 커밋 메시지 | `타입: 설명` 형식 |

---

## 9. 빠른 참조: 일상 워크플로우

**작업 시작**
- [ ] `git checkout main`
- [ ] `git pull origin main`
- [ ] `git checkout -b feat/기능명`

**작업 중**
- [ ] 코드 작업
- [ ] `git add .`
- [ ] `git commit -m "feat: 설명"`

**작업 완료**
- [ ] `git push origin feat/기능명`
- [ ] GitHub에서 PR 생성
- [ ] 팀원 리뷰 → 머지

**정리**
- [ ] `git checkout main`
- [ ] `git pull origin main`
- [ ] `git branch -d feat/기능명`

---

## 연관 노트

- [[기획안]]

*최종 수정: 2026-03-15*
