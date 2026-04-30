# 🎮 게임 기획 문서 (GDD)
> 프로토타입 기반 2D 무한 자동 전투 RPG

---

## 1. 게임 개요

| 항목 | 내용 |
|------|------|
| **장르** | 2D 자동 전투 RPG |
| **플랫폼** | PC (Unity 6, URP 2D) |
| **타겟** | 방치형 RPG (Idle RPG) |
| **핵심 루프** | 자동 이동 → 몬스터 전투 → 성장 → 다음 구역 반복 |
| **플레이 타임** | 무한 진행형 (Idle 게임 요소) |

---

## 2. 핵심 시스템

### 2.1 전투 시스템

```
[자동 이동] → [몬스터 감지] → [배경 스크롤 정지] → [전투] → [처치] → [보상] → [다음 소환]
```

- **자동 전투**: 플레이어가 일정 거리(2.0) 이내 접근 시 자동 공격
- **배경 연동**: 전투 중 배경 스크롤 정지
- **오브젝트 풀링**: 몬스터, 이펙트, 코인 풀링 사용
- **몬스터 데이터**: ScriptableObject로 관리

### 2.2 성장 시스템

- **경험치 요구량**: `Level × 10`
- **레벨업 보상**:
  - 공격력 +2
  - 최대 HP +5
  - HP 전체 회복

| 능력치 | 기본값 | 성장 |
|--------|--------|------|
| HP | 10 | +5/레벨 |
| 공격력 | 1 | +2/레벨 |
| 공격 속도 | 1초 | 고정 |

### 2.3 경제 시스템

- **골드 획득**: 몬스터 처치 시 보상
- **골드 용도**: (향후) 장비 강화, 스킬 해금 등 확장 예정

---

## 3. 콘텐츠 구조

### 3.1 몬스터 데이터 (ScriptableObject)

```
MonsterData
├── monsterName (이름)
├── prefab (프리팹)
├── maxHealth (최대 HP)
├── attackPower (공격력)
├── attackInterval (공격 간격)
├── moveSpeed (이동 속도)
├── expReward (경험치 보상)
├── goldReward (골드 보상)
├── spawnWeight (스폰 가중치)
└── spawn transform (위치/회전 조정)
```

### 3.2 몬스터 스폰

- **가중치 기반 랜덤**: SpawnWeight 값에 따라 출현 확률 결정
- **오브젝트 풀링**: 몬스터별 3~10개 풀 유지

---

## 4. 기술 아키텍처

### 4.1 폴더 구조

```
Assets/
├── _Project/
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── GameManager.cs        # 싱글톤, 게임 상태, 골드 관리
│   │   │   ├── DIContainer.cs        # 의존성 주입 컨테이너
│   │   │   ├── GameState.cs          # 게임 상태 enum
│   │   │   ├── GameBootstrap.cs      # 게임 초기화
│   │   │   ├── RootLifetimeScope.cs  # DI 루트 스코프
│   │   │   └── GameLifetimeScope.cs  # DI 게임 스코프 (VContainer)
│   │   └── Gameplay/
│   │       ├── AutoBattleController.cs    # 전투 흐름 제어
│   │       ├── AutoBattleUnit.cs          # 캐릭터/몬스터 유닛
│   │       ├── AutoBattleAI.cs            # 몬스터 AI
│   │       ├── AutoBattleSensor2D.cs      # 충돌 감지
│   │       ├── MonsterData.cs             # 몬스터 데이터 (SO)
│   │       ├── ExpBarUI.cs                # 경험치 바
│   │       ├── BackgroundScroller.cs      # 배경 스크롤
│   │       ├── ParallaxBackground2D.cs    # 패럴랙스 배경
│   │       └── TopDownCharacterController2D.cs
│   └── Animations/
├── Prefab/              # 몬스터, 플레이어, 이펙트 프리팹
└── Scenes/              # 씬 파일
```

### 4.2 핵심 클래스 역할

| 클래스 | 역할 |
|--------|------|
| **GameManager** | 싱글톤, 게임 상태 관리 (Booting→Title→Playing→Paused→GameOver), 골드 관리 |
| **DIContainer** | 글로벌 의존성 주입 컨테이너, FindFirstObjectByType 폴백 |
| **AutoBattleController** | 전투 루프 전체 제어, 몬스터 스폰/풀링, 이펙트/코인 드롭 |
| **AutoBattleUnit** | 유닛 능력치 (HP/ATK), 애니메이션, 공격/피격/죽음, 레벨업 |
| **AutoBattleAI** | 몬스터 AI - 타겟 감지 → 이동 → 정지 |
| **AutoBattleSensor2D** | 2D 콜라이더 기반 타겟 감지 |
| **MonsterData** | 몬스터 정적 데이터 ScriptableObject |
| **ExpBarUI** | 경험치 바 UI (Slider 기반) |
| **BackgroundScroller** | 머티리얼 오프셋 기반 배경 스크롤 |

### 4.3 게임 상태

```
Booting → Title → Playing ↔ Paused
                   └→ GameOver
```

---

## 5. 개발 로드맵

### ✅ Phase 1: 프로토타입 (완료)
- [x] 기본 자동 전투 시스템
- [x] 몬스터 스폰 & 오브젝트 풀링
- [x] 경험치/레벨업
- [x] 무한 배경 스크롤링
- [x] 기본 UI (ExpBar)
- [x] VContainer 기반 DI 구조

### 🚧 Phase 2: MVP (1-2주)
- [x] 몬스터 3-5종 추가
- [ ] 보스 몬스터 시스템
- [ ] UI 개선 (HP바, 레벨 표시, 골드 UI)
- [ ] 사운드 효과
- [ ] 타이틀 화면

### 📋 Phase 3: 콘텐츠 확장 (2-4주)
- [ ] 장비 시스템 (무기/방어구)
- [ ] 스킬 시스템 (액티브/패시브)
- [ ] 스테이지/구역 시스템
- [ ] 저장/로드 기능


---

## 6. 참고사항

### 외부 에셋
- **FreeKnight_v1** - 플레이어 캐릭터 스프라이트 (칼라/아웃라인)
- **Dark fantasy - popular enemies** - 몬스터 스프라이트 (Skeleton 등)
- **Free Slash VFX** - 공격 이펙트 (SlashDecal, Projectile 등)



---

*문서 버전: 1.0*  
*작성일: 2026년*  

