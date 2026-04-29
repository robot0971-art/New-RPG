# 프로젝트 가이드 (AGENT.md)

이 문서는 이 프로젝트의 아키텍처, 기술적 결정 사항, 그리고 코드 작성 규칙을 설명합니다. 새로운 기능을 추가하거나 기존 코드를 수정할 때 이 가이드를 반드시 준수하십시오.

## 1. 프로젝트 개요
- **장르:** 탑다운/횡스크롤 하이브리드 자동 전투(Auto-Battler) 게임.
- **엔진:** Unity 6 (6000.3.10f1).
- **렌더링:** Universal Render Pipeline (URP) 2D.
- **핵심 루프:** 전진(배경 스크롤) -> 적 조우(센서 감지) -> 자동 전투 -> 승리 후 다시 전진.

## 2. 기술 스택 및 라이브러리
- **DI 컨테이너:** [VContainer](https://vcontainer.hadashikake.jp/) (의존성 주입 및 객체 생명주기 관리).
- **오브젝트 풀링:** `UnityEngine.Pool` (적 유닛 재사용).
- **입력 시스템:** New Input System.
- **2D 도구:** Aseprite Importer, 2D Animation, Tilemap.

## 3. 아키텍처 구조
프로젝트는 `Core`와 `Gameplay`로 분리되어 있습니다.

### Core (시스템 레벨)
- **GameManager:** 게임의 전체 상태(`Title`, `Playing`, `Paused`, `GameOver`)를 관리하는 상태 머신.
- **RootLifetimeScope:** 전역 의존성(Service, Manager)을 등록하는 VContainer의 루트 컨테이너.
- **GameBootstrap:** 게임 진입점을 보장하고 초기화를 수행.

### Gameplay (콘텐츠 레벨)
- **AutoBattleController:** 게임의 핵심 루프를 제어. 적 스폰, 전투 시작/종료 판정 담당.
- **AutoBattleUnit:** 유닛의 데이터(HP, ATK), 애니메이션, 공격 및 피해 로직을 포함하는 엔티티.
- **AutoBattleSensor2D:** 트리거를 통해 적 유닛을 감지하는 센서 컴포넌트.
- **BackgroundScroller:** 머티리얼 오프셋을 조절하여 이동감을 주는 배경 제어기.
- **GameLifetimeScope:** 씬 단위의 의존성(플레이어, 컨트롤러 등)을 주입.

## 4. 코딩 컨벤션 및 규칙

### 클래스 및 필드
- 모든 클래스는 특별한 이유가 없는 한 `sealed`로 선언하여 상속을 제한합니다.
- 인스펙터 노출이 필요한 프라이빗 필드는 `[SerializeField] private` 형식을 사용합니다.
- 의존성 주입은 싱글톤 직접 참조(`Instance`) 대신 `[Inject]`와 `Construct` 메서드를 통한 주입을 지향합니다.

### 성능 최적화
- **애니메이션:** 문자열 기반 `Play("Name")` 대신 `Animator.StringToHash`를 사용하여 캐싱된 해시값을 사용합니다.
- **메모리:** 매 프레임 `.material`에 접근하지 마십시오. `Start`에서 머티리얼 인스턴스를 캐싱하거나 `sharedMaterial`/`MaterialPropertyBlock`을 사용합니다.
- **가비지 컬렉션:** 빈번하게 생성/파괴되는 객체(적 유닛, 이펙트 등)는 반드시 오브젝트 풀링을 적용합니다.

### 전투 로직
- 대미지 계산 및 애니메이션 재생은 `AutoBattleUnit` 내부에서 처리(캡슐화)합니다.
- `AutoBattleController`는 유닛 간의 상호작용 명령만 내립니다.

## 5. 확장 가이드
- **새로운 시스템 추가:** `IService` 인터페이스를 구현하고 `RootLifetimeScope`에 등록하십시오.
- **새로운 유닛 추가:** `AutoBattleUnit`을 상속받거나 컴포넌트 설정을 조정하고, `AutoBattleController`의 적 템플릿으로 할당하십시오.
- **UI 연동:** `GameManager.StateChanged` 이벤트를 구독하여 상태에 맞는 UI를 표시하십시오.
