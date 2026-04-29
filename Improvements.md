# New-RPG 프로젝트 개선점 분석 및 제안

## 1. DI 컨테이너 한계

### 문제점
- `DIContainer`는 전역 싱글톤 + 구체 클래스 등록만 가능
- 인터페이스 기반 의존성 주입이 아니라 테스트가 어렵고 유연성이 떨어짐
- 씬 전환 시 `ResetGlobal()` 수동 호출 필요

### 개선 방향
- **옵션 A**: VContainer나 Zenject 등 검증된 DI 프레임워크 도입
- **옵션 B**: 인터페이스 기반 의존성 주입 적용 (IPlayerService, IGameState 등)
- **옵션 C**: DI 수명 주기 관리 (씬 단위 Scope 분리)

---

## 2. GameManager 과도한 책임

### 문제점
- 게임 상태 관리 + 골드/통화 관리를 동시에 담당 (SRP 위반)
- 싱글톤으로 전역 접근, 캡슐화 파괴
- `Time.timeScale` 조작까지 담당

### 개선 방향
```csharp
// 분리 예시
public interface IGameStateManager { }
public interface ICurrencyManager { }
public interface ITimeManager { }
```

---

## 3. AutoBattleController = God Object

### 문제점
- **524줄**의 거대 클래스
- 스폰, 전투 AI, 이펙트, 풀링, 배경 스크롤, 골드 지급 등 전부 담당
- 의존성이 6개 이상

### 분리 제안
```
AutoBattleController
├── EnemySpawner (적 생성/관리)
├── CombatController (전투 로직)
├── EffectManager (이펙트/풀링)
├── RewardManager (골드/경험치)
└── BackgroundController (배경 스크롤)
```

---

## 4. 의존성 관리 혼용

### 문제점
- `SerializeField` 직접 참조
- `DIContainer.Resolve<T>`
- `FindFirstObjectByType`를 섞어 사용
- null 체크 과다

### 개선 방향
- 한 가지 방식으로 통일 (권장: DI 중심)
- 의존성 주입 시점 명확화 (Awake/Start 구분)

---

## 5. 데이터/설정 관리 미비

### 문제점
- 매직 넘버 코드 내 고정
- 디자이너가 수치 조정 불가

### 개선 방향
- ScriptableObject 기반 데이터 중앙화
- JSON/ScriptableObject 하이브리드 설정 시스템
- 게임 밸런스 테이블 외부화

---

## 6. 이벤트 시스템 강결합

### 문제점
- `Action<T>` 직접 구독/해제 패턴
- 구독 해제 누락 시 메모리 누수
- UI-게임 로직 직접 연결

### 개선 방향
- **옵션 A**: UniRx(Reactive Extensions) 도입
- **옵션 B**: 이벤트 버스 패턴 적용
- **옵션 C**: 메시지/커맨드 기반 아키텍처

---

## 7. 입력 처리 미흡

### 문제점
- `Keyboard.current` 직접 폴링 (구식 방식)
- Input Action Asset 미사용
- 게임패드/터치 지원 불가

### 개선 방향
```csharp
// Input Action Asset 기반으로 변경
[CreateAssetMenu(fileName = "GameInput", menuName = "Input/Game Input")]
public class GameInputActions : ScriptableObject { }
```

---

## 8. 코루틴 관리

### 문제점
- `StartCoroutine` 남발
- `IEnumerator` 반환 핸들 미관리
- 중지 처리 일관성 부족

### 개선 방향
- **옵션 A**: UniTask(async/await) 도입
- **옵션 B**: CoroutineManager 중앙 관리
- **옵션 C**: DOTween 체인 사용

---

## 개선 우선순위

| 우선순위 | 항목 | 난이도 | 영향도 |
|:---:|---|:---|:---|
| 1 | AutoBattleController 분리 | 중 | 높음 |
| 2 | DI → 인터페이스 기반 전환 | 중 | 높음 |
| 3 | ScriptableObject 설정 중앙화 | 낮음 | 중간 |
| 4 | Input Action Asset 적용 | 낮음 | 중간 |
| 5 | 이벤트 버스/UniRx 도입 | 중 | 중간 |
| 6 | UniTask 전환 | 중 | 낮음 |

---

## 추천 라이브러리

| 라이브러리 | 용도 | 설치 방법 |
|---|---|---|
| VContainer | 의존성 주입 | Package Manager |
| UniRx | 반응형 프로그래밍 | UPM 또는 .unitypackage |
| UniTask | 비동기/코루틴 대체 | UPM |
| DOTween | 애니메이션 트윈 | Asset Store |

---

## 다음 단계 제안

1. **Phase 1**: AutoBattleController 분리 리팩토링
2. **Phase 2**: DI 컨테이너 개선 또는 VContainer 도입
3. **Phase 3**: 설정 데이터 ScriptableObject화
4. **Phase 4**: UniRx/UniTask 점진적 도입
5. **Phase 5**: 입력 시스템 개선

---

*작성일: 2026-04-29*
*대상: Unity 6000.x*
