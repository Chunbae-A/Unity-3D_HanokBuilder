<p align="center">
  <img src="https://img.shields.io/badge/Unity-6000.3.16f1-000000?style=flat-square&logo=unity&logoColor=white" alt="Unity">
  <img src="https://img.shields.io/badge/Platform-Windows%20%7C%20WebGL%20(미배포)-E44D26?style=flat-square" alt="Platform">
  <img src="https://img.shields.io/badge/AI-Claude%20Haiku-6B5B95?style=flat-square" alt="Claude Haiku">
  <img src="https://img.shields.io/badge/Render-URP-3C4A5E?style=flat-square" alt="URP">
  <img src="https://img.shields.io/badge/Input-New%20Input%20System-0070CC?style=flat-square" alt="Input System">
  <img src="https://img.shields.io/badge/Data-문화포털%20메타버스데이터랩-2E7D59?style=flat-square" alt="Culture Data">
</p>

<h1 align="center">HanokBuilder — Unity Source</h1>

<p align="center">
  한국 전통 건축 공간 3D 제작 도구 · Unity 6 소스 레포지토리
</p>

<p align="center">
  <a href="https://chunbae-a.github.io/HanokBuilder-guide/"><strong>사용자 가이드 →</strong></a>
  &nbsp;·&nbsp;
  <s>WebGL 체험</s> <em>(미배포 — 준비 중)</em>
  &nbsp;·&nbsp;
  <a href="https://drive.google.com/drive/folders/1gCTwTNuJAuEa3ueKQiMCqAYTLsg8wmWK?usp=drive_link"><strong>Windows 다운로드 →</strong></a>
</p>

> **사용 방법은 [사용자 가이드](https://chunbae-a.github.io/HanokBuilder-guide/)를 참고하세요.**
> 이 문서는 코드 구조, 개발 환경 세팅, 빌드, 기술 이슈 해결에 집중합니다.

---

## 목차

- [기술 스택](#기술-스택)
- [아키텍처 개요](#아키텍처-개요)
- [코드 구조](#코드-구조)
- [개발 환경 세팅](#개발-환경-세팅)
- [에셋 추가 워크플로](#에셋-추가-워크플로)
- [AI 연동 구조](#ai-연동-구조)
- [빌드](#빌드)
- [알려진 기술 이슈 & 해결책](#알려진-기술-이슈--해결책)
- [데이터 출처](#데이터-출처)
- [관련 레포지토리](#관련-레포지토리)

---

## 기술 스택

| 항목 | 내용 |
| --- | --- |
| 엔진 | Unity 6 (6000.3.16f1) |
| 렌더 파이프라인 | Universal Render Pipeline (URP) |
| 입력 시스템 | Unity New Input System (`Mouse.current`, `Keyboard.current`) |
| UI 시스템 | uGUI — Canvas · CanvasScaler · ScrollRect (코드 생성, Inspector 비의존) |
| AI | Claude Haiku (`claude-haiku-4-5-20251001`) — Anthropic REST API |
| 폰트 | NotoSansKR SDF (TextMesh Pro) |
| 에셋 로드 | `Resources.LoadAll` — `HanokBuilder/Resources/HanokAssets/` |
| 직렬화 | Newtonsoft.Json (`Newtonsoft.Json.dll`) |
| 플랫폼 | Windows 64-bit · ~~WebGL~~ *(미배포 — 준비 중)* |

---

## 아키텍처 개요

```
┌─────────────────────────────────────────────────┐
│                  HanokUIManager                  │  ← 진입점 · 전체 조율
│  선택 상태 / 툴 모드 / Undo 스택 / 이벤트 허브  │
└────┬──────────┬──────────┬────────────┬──────────┘
     │          │          │            │
     ▼          ▼          ▼            ▼
HanokAsset  HanokEdit  HanokCamera  HanokAIPanel
Panel       Panel      Controller   │
(라이브러리) (Transform) (오비트·줌)  ├─ HanokAgentLayout    [맵] 자동 레이아웃
                                    ├─ HanokSceneEditAgent  [씬] 자연어 편집
                                    └─ HanokGuidePanel      문화재 해설

기즈모 레이어 (독립 MonoBehaviour)
  HanokRotationGizmo  ·  HanokScaleGizmo  ·  HanokSelectionHighlight

씬 오브젝트 레이어
  SelectableAsset (모든 Collider에 자동 부착)
  HanokPlacedAssetMetadata (배치 시 주입)
```

**설계 원칙**

- **UI 코드 생성**: `HanokUIBuilder`가 런타임에 전체 Canvas를 코드로 빌드. Inspector 배치 없음.
- **단방향 선택 흐름**: `SelectableAsset` → `HanokUIManager.SelectObject()` → 패널/기즈모 갱신.
- **Undo**: `List<Action>` 기반 20단계 스택. 모든 배치·삭제·Transform 변경이 델리게이트로 등록.
- **AI 호출**: 모든 Claude 호출은 `ClaudeApiConfig`를 통해 단일 HTTP 클라이언트로 처리.

---

## 코드 구조

```
Assets/HanokBuilder/Scripts/
│
├── 코어
│   ├── HanokUIManager.cs           선택 상태 · 툴 모드 · Undo 스택 · 클릭 감지
│   ├── HanokUIBuilder.cs           Canvas · 패널 · 버튼 런타임 코드 생성
│   └── SelectableAsset.cs          Collider에 부착, 루트 오브젝트 선택 위임
│
├── 패널
│   ├── HanokAssetPanel.cs          에셋 라이브러리 · 썸네일 RenderTexture · 검색 필터
│   ├── HanokEditPanel.cs           Position · Rotation · Scale 수치 편집
│   └── HanokAIPanel.cs             프롬프트 바 · 추천 결과 가로 스크롤 UI
│
├── AI
│   ├── ClaudeApiConfig.cs          API 키 저장 · HTTP 헤더 구성
│   ├── HanokApiKeyPanel.cs         API 키 입력 모달
│   ├── HanokAgentLayout.cs         [맵] 최대 20턴 반복 배치 에이전트
│   ├── HanokSceneEditAgent.cs      [씬] 대화 히스토리 유지 씬 편집 에이전트
│   └── HanokGuidePanel.cs          문화재 해설 3문장 생성 · 말풍선 UI
│
├── 카메라
│   └── HanokCameraController.cs    오비트 · 패닝 · 줌투커서 · Numpad 뷰 · 포커스
│
├── 기즈모
│   ├── HanokRotationGizmo.cs       3색 링(X·Y·Z) · 카메라 거리 기반 크기 보정
│   ├── HanokScaleGizmo.cs          축 방향 큐브 핸들 · 중앙 균일 구형 핸들
│   └── HanokSelectionHighlight.cs  선택 에셋 바닥 남색 원형 링
│
├── 씬 환경
│   └── HanokSceneSetup.cs          배경 프리셋 4종 전환 · 조명 · 환경
│
├── 데이터
│   ├── HanokAssetInfo.cs           에셋 메타(assetKey · 표시명 · 카테고리 · 태그)
│   ├── HanokAssetCategory.cs       카테고리 열거형 및 한글 표시명
│   ├── HanokAssetTags.cs           assetKey → 검색 태그 매핑 딕셔너리
│   └── HanokPlacedAssetMetadata.cs 배치 오브젝트 런타임 메타
│
└── 유틸
    └── UIDraggablePanel.cs         PointerDrag 기반 플로팅 패널 이동
```

---

## 개발 환경 세팅

### 요구 사항

| 항목 | 버전 |
| --- | --- |
| Unity Hub | 최신 권장 |
| Unity Editor | **6000.3.16f1** (정확한 버전 필요) |
| Git LFS | 대용량 FBX · 텍스처 관리 |

### 클론 및 열기

```bash
git clone https://github.com/Chunbae-A/Unity-3D_Korean_Traditional_Architecture.git
cd Unity-3D_Korean_Traditional_Architecture
git lfs pull          # FBX · 텍스처 다운로드
```

Unity Hub → `Open` → 클론 폴더 → Unity 6000.3.16f1로 열기

> 처음 열면 패키지 임포트와 셰이더 컴파일에 수 분이 소요됩니다.

### 필수 패키지 확인 (Package Manager)

| 패키지 | 확인 방법 |
| --- | --- |
| Universal RP | `Window → Package Manager → In Project` |
| Input System | 동일. 설치 후 `Edit → Project Settings → Player → Active Input Handling = Input System Package` |
| TextMesh Pro | `Window → TextMeshPro → Import TMP Essential Resources` |

---

## 에셋 추가 워크플로

```
1. FBX를 Unity Project 창으로 드래그 임포트

2. FBX 임포트 설정 확인 (Inspector)
   Scale Factor = 0.01  (cm 단위 FBX인 경우)
   Read/Write  = true   (bounds 계산 필요 시)

3. Hierarchy에 FBX 드래그 → 씬 배치 확인

4. Hierarchy 오브젝트 우클릭
   → Prefab → Create Original Prefab

5. 생성된 .prefab 파일을
   Assets/HanokBuilder/Resources/HanokAssets/ 로 이동

6. Play Mode → 에셋 라이브러리에 자동 표시
```

`Resources.LoadAll<GameObject>("HanokAssets")` 로 로드됩니다.
Prefab 루트 및 모든 자식 Collider에 `SelectableAsset` 이 `AttachSelectable()` 으로 자동 부착됩니다.

---

## AI 연동 구조

```
HanokAIPanel  ──입력──▶  ClaudeApiConfig
                              │  API 키 · 엔드포인트
                              ▼
                     Anthropic Messages API
                     POST /v1/messages
                     model: claude-haiku-4-5-20251001
                              │
            ┌─────────────────┼──────────────────┐
            ▼                 ▼                  ▼
   HanokAgentLayout   HanokSceneEditAgent  HanokGuidePanel
   (맵 — 최대 20턴)   (씬 — 대화 히스토리)  (해설 — 단발 호출)
```

| 모드 | 클래스 | 호출 방식 |
| --- | --- | --- |
| 에셋 추천 (기본) | `HanokAIPanel` | 에셋 카탈로그 전체를 system prompt에 포함, 단발 호출 |
| 자동 레이아웃 `[맵]` | `HanokAgentLayout` | 배치 상태를 피드백으로 넣어 최대 20턴 반복 |
| 자연어 씬 편집 `[씬]` | `HanokSceneEditAgent` | `List<Message>` 히스토리 유지, 대화 맥락 인식 |
| 문화재 해설 | `HanokGuidePanel` | 에셋 선택 이벤트에서 단발 호출, 말풍선 렌더링 |

API 키는 `PlayerPrefs`에 저장됩니다. 키 없으면 키워드 기반 로컬 필터로 자동 폴백합니다.

---

## 빌드

### Windows

```
File → Build Settings
  Platform  : PC, Mac & Linux Standalone
  Target    : Windows  |  Architecture : x86_64
→ Build
```

### WebGL *(미배포 — 준비 중)*

> 현재 WebGL 빌드는 배포되지 않았습니다. 완료 시 이 섹션이 업데이트됩니다.

```
File → Build Settings
  Platform : WebGL
→ Switch Platform → Build
```

빌드 완료 후 `index.html` + `Build/` 폴더를 웹 서버 또는 GitHub Pages에 업로드합니다.

---

## 알려진 기술 이슈 & 해결책

| # | 이슈 | 원인 | 해결책 |
| --- | --- | --- | --- |
| 1 | `TextAlignmentOptions.MidlineCenter` 컴파일 오류 | Unity 6 TMP에서 해당 열거형 제거 | `TextAlignmentOptions.Center` 로 교체 |
| 2 | `Image` + `TextMeshProUGUI` 동일 오브젝트 부착 불가 | 둘 다 `Graphic` 상속 — 충돌 | 텍스트를 자식 GameObject에 분리 |
| 3 | `UnityEngine.Input` vs New Input System 충돌 | 구형 API 혼용 | `Mouse.current.leftButton.wasPressedThisFrame` 으로 통일 |
| 4 | 에셋 목록 미표시 | Prefab 미변환 또는 Resources 경로 오류 | Prefab 변환 후 `HanokAssets/` 경로 확인 |
| 5 | 에셋 배치 후 화면에 미표시 | 카메라 Z값 이상 이동 | Camera Position `(0, 5, -10)` · Rotation `(20, 0, 0)` 재설정 |
| 6 | 오브젝트 클릭 선택 안 됨 | Collider가 자식에만 존재, 루트에 `SelectableAsset` 없음 | `AttachSelectable()` 으로 전체 계층 일괄 부착 |
| 7 | 회전 기즈모 링 크기 이상 | FBX cm 스케일로 bounds 100× 오인식 | 카메라 거리 기반 폴백 크기 적용 + `Scale Factor = 0.01` |
| 8 | `ContentSizeFitter` 첫 프레임 레이아웃 미계산 | `VerticalLayoutGroup` 초기화 타이밍 | `LayoutRebuilder.ForceRebuildLayoutImmediate` 코루틴 강제 갱신 |

---

## 데이터 출처

에셋 라이브러리는 국가기관이 공개한 [문화공공데이터](https://www.culture.go.kr/datametaverse)를 원천으로 합니다.

| 데이터 | 제공 기관 | 플랫폼 |
| --- | --- | --- |
| 건축물 완성형 3D 데이터 | 한국문화정보원 | 문화포털 메타버스데이터랩 |
| 건축물 부품형 3D 데이터 | 한국문화정보원 | 문화포털 메타버스데이터랩 |
| 디지털 휴먼 데이터 | 한국문화정보원 | 문화포털 메타버스데이터랩 |
| 공간소품 3D 오브젝트 | 한국문화정보원 | 문화포털 메타버스데이터랩 |

원본 FBX · 텍스처 파일은 Git LFS로 코드와 분리 관리합니다.
에셋 식별자(`assetKey`), 한글 표시명, 카테고리, 검색 태그로 재구조화해 `HanokAssetTags.cs`에서 관리합니다.

---

## 관련 레포지토리

| 레포 | 설명 |
| --- | --- |
| [HanokBuilder-guide](https://github.com/Chunbae-A/HanokBuilder-guide) | 공식 사용자 가이드 사이트 (HTML · CSS · JS) |
