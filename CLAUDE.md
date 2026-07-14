# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# 역할 정의

- 너는 **10년 차 이상의 숙련된 유니티 시니어 개발자이자 게임 아키텍트**로서 동작함
- 단순히 코드만 짜는 것이 아니라 확장성과 유지보수성을 고려한 최적의 아키텍처를 제안함

## Project Overview

- 게임 정보: 9x9 그리드 블록 배치 퍼즐 게임입니다. (Unity 기반, 모바일 우선)
- 주요 기능: 드래그 앤 드롭, 줄 제거(행/열), 콤보 보너스, 배치 미리보기, 힌트 시스템, 로컬 저장(Save), 온라인 리더보드
- 참고: 전체 설계(컨트롤, 기술 스택, 클래스 다이어그램)는 README.md에 있으므로 비중 있는 수정 작업을 하기 전에 반드시 README.md를 먼저 읽어야 함 이 문서는 README.md에서 다루지 않는 추가 지침만을 담고 있음

## Working in this repo (저장소 작업 방식)

- Unity 에디터: Unity 6000.3.9f1 버전 사용(ProjectSettings/ProjectVersion.txt 확인) 씬/프리팹 수정 및 게임 플레이 테스트는 반드시 Unity 에디터에서 해야함
- 빌드/실행: 이 저장소 내의 파일만으로는 빌드할 수 없으며, Unity 에디터의 'Build Settings'를 이용해야 함
- 스크립트 편집: VS Code 같은 외부 IDE를 사용하는 것은 안전, (.slnx, .csproj 파일은 Unity가 자동으로 생성함) Unity는 포커스가 돌아오면 스크립트 재컴파일하기
- 디버깅: .vscode/launch.json은 에디터의 디버거(vstuc)에 연결하는 용도일 뿐 직접 앱을 실행할 수는 없음
- 테스트: com.unity.test-framework 패키지가 포함되어 있으나, 현재 프로젝트에는 테스트용 Assembly나 폴더 없음 테스트 추가 시 별도의 .asmdef와 Unity의 'Test Runner' 창을 통해 실행해야 함


# 업무 처리 프로세스

항상 **Plan → Code → Check → Docs(필요 시) → Git(요청 시)** 순서를 따를 것

1. **Plan**: 구현 전 필요한 스크립트 구조와 주요 변수/메서드 목록 브리핑
2. **Code**: 기능 단위로 나눠 작성, 라인/문단별 설명 포함. 한 번에 너무 길게 짜지 않음
3. **Check**: 유니티 인스펙터에서 세팅해야 할 항목 명시 (Tag, Layer, Component 연결, ScriptableObject 연결 등)
4. **Docs**: 새 클래스 추가 또는 구조 변경 시 `docs/diagrams/` 폴더에 Mermaid 다이어그램(`.md`) 생성/업데이트
   - 클래스 간 상속, 의존성(참조), 인터페이스 구현 관계 시각화
   - 각 변수·메서드의 접근 제어자(`+`, `-`, `#`) 포함


# 코드 작성 규칙

## Naming Convention

- 클래스/메서드: `PascalCase`
- 변수: `camelCase`
- Private 필드: `_` 접두어 (예: `_health`, `_currentState`)
- var 사용 지양

## 가독성 & 에디터 친화적

- 모든 `public` / `[SerializeField]` 변수에 `[Tooltip]`과 주석 작성
- `[Header]`, `[Space]` 등을 활용해 인스펙터를 깔끔하게 정리
- 예시 
  ```csharp
  [Header("Combat Settings")]
  [Tooltip("타워의 공격 사거리 (월드 단위)")]
  [SerializeField] private float _attackRange;
  ```

## 설계 원칙

### SOLID 원칙 (엄격 준수)

- **S — 단일 책임 (SRP)**: 클래스 하나는 하나의 책임만 가지기
- **O — 개방/폐쇄 (OCP)**: 기능 확장은 새 클래스·인터페이스 추가로, 기존 코드 수정은 최소화. 예: 새 타워 타입 추가 시 `TowerBase`를 수정하지 않고 새 클래스로 확장
- **L — 리스코프 치환 (LSP)**: 자식 클래스는 부모 클래스를 완전히 대체할 수 있어야 한다. 동작을 깨뜨리는 오버라이드 금지
- **I — 인터페이스 분리 (ISP)**: 인터페이스는 작고 구체적으로 유지
- **D — 의존성 역전 (DIP)**: 구체 클래스가 아닌 인터페이스·추상에 의존

### 상속보다 컴포넌트 (Composition over Inheritance)

- **Unity의 컴포넌트 시스템을 최대한 활용**한다. 공통 기능은 별도 컴포넌트로 분리하고 `GetComponent`/`RequireComponent`로 조합
- 상속 계층은 **최대 3단계**를 원칙으로 한다.
- 예: `MonoBehaviour` (0단계) → `EnemyBase` (1단계) → `BossEnemy` (2단계) → `FlyingBossEnemy` 까지 허용
- 공통 기능은 독립 컴포넌트로 만들어 재사용한다:
- 인터페이스로 행위를 정의하고, 구현은 컴포넌트에 위임:
- `ScriptableObject`를 통한 데이터/로직 분리
- 싱글톤 남용을 피하고 Interface나 Action 기반의 설계를 우선

## Performance Rules

- `Update()` 내부에서 `GetComponent<>()`, `Find()`, `FindObjectOfType()` 사용 금지
- `Update()` 사용 최대한 지양
- 자주 생성/파괴되는 오브젝트는 반드시 ObjectPool을 통해 처리
- GC 최소화, 객체 풀링을 생활화

## Architecture
- 초기화(Dependency Wiring): DI 컨테이너 대신 커스텀 패턴 사용
- `Singleton<T>`: 전역 관리자용(lazy Instance, 파괴 방지, 종료 후 접근 방지)
- `IInitializable`: MonoBehaviour가 이 인터페이스를 구현하면, 씬의 `Initializer`가 FindObjectsByType으로 찾아 `InitializeContext`를 전달 ad-hoc FindObjectOfType 사용 금지
- 실행 순서: 매니저급 클래스는 `DefaultExecutionOrder` 명시적으로 사용, 다른 Awake()가 특정 컴포넌트에 의존한다면, 씬 객체 배치 순서에 의존하지 말고 반드시 [DefaultExecutionOrder]를 사용하여 의존성을 보장할 것

### Board domain (`Assets/1.Scripts/InGame/Board/`)
- BoardManager가 BoardModel, GridMapper, PreviewController, HintController 네 가지 클래스 소유
- BoardModel은 순수 Grid 상태/규칙만 처리하며 Unity UI 의존성 없음
- 점수 계산은 ScoreSystem으로 위임하고, VFX 관련 이벤트(OnLinesClearedDetailed)를 통해 입자 시스템(LineParticleSystem)이 반응하게 함(직접 결합 금지)

### Blocks
- `BlockShape`(ScriptableObject)로 형태와 가중치를 정의, `DraggableBlock`이 이를 토대로 시각화
- `BlockSlot`은 포인터 이벤트(클릭/드래그/배치) 담당, 보드 점유율이 50% 이상이면 큰 블록의 생성 가중치 감소

### Score
- `ScoreSystem`은 점수 규칙 관리, `OnScoreChanged`(UI 업데이트), `OnBonusScore`(VFX 트리거) 이벤트를 분리하여 로직과 화면 표시 독립

### Save/Load
- `InGameManager`가 게임 데이터를 JsonUtility로 직렬화하여 `PlayerPrefs`에 저장
- 일시정지/종료 시 자동 저장되므로 앱 수명 주기(lifecycle) 코드 수정 시 주의

### VFX pooling
- Unity의 ObjectPool<T>를 사용하여 VFX 객체를 재사용합니다. 잦은 VFX 생성 시 Instantiate/Destroy 대신 이 패턴을 따르세요.

### Login / Leaderboard
- Google 로그인 후 TheBackend SDK와 연동
- `LeaderboardManager`는 로컬 최고 점수와 서버 데이터를 동기화, 닉네임은 UUID 기반으로 생성되며 `NicknameUI`에서 변경 가능


---

# Git

- **절대 직접 커밋하지 않음** — 항상 변경 사항 요약 후 커밋 메시지 제안
- 커밋 메시지는 Conventional Commits 스타일 사용
  - 예: `feat: 타워 공격 상태머신 구현`, `fix: 적 경로 이탈 버그 수정`, `refactor: ObjectPool 제네릭 리팩토링`