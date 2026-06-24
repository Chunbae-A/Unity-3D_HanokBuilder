<p align="center">
  <img src="https://img.shields.io/badge/Unity-6000.3.16f1-000000?style=flat-square&logo=unity&logoColor=white" alt="Unity">
  <img src="https://img.shields.io/badge/Platform-Windows%20%7C%20WebGL-E44D26?style=flat-square" alt="Platform">
  <img src="https://img.shields.io/badge/AI-Claude%20Haiku-6B5B95?style=flat-square" alt="Claude Haiku">
  <img src="https://img.shields.io/badge/Render-URP-3C4A5E?style=flat-square" alt="URP">
  <img src="https://img.shields.io/badge/Input-New%20Input%20System-0070CC?style=flat-square" alt="Input System">
  <img src="https://img.shields.io/badge/Data-문화포털%20메타버스데이터랩-2E7D59?style=flat-square" alt="Culture Data">
</p>

<p align="center">
  <img src="https://chunbae-a.github.io/HanokBuilder-guide/assets/screenshots/workspace-overview.png" alt="HanokBuilder 실행 화면" width="100%">
</p>

<h1 align="center">HanokBuilder</h1>

<p align="center">
  한국 전통 건축 공간을 3D로 직접 조립하고 편집하는 인터랙티브 제작 도구<br>
  Unity 6 · URP · Claude Haiku AI · 문화공공데이터 기반
</p>

<p align="center">
  <a href="https://chunbae-a.github.io/HanokBuilder-guide/"><strong>사용자 가이드 →</strong></a>
  &nbsp;·&nbsp;
  <a href="https://chunbae-a.github.io/Unity-3D_Korean_Traditional_Architecture/"><strong>WebGL 체험 →</strong></a>
  &nbsp;·&nbsp;
  <a href="https://drive.google.com/drive/folders/1gCTwTNuJAuEa3ueKQiMCqAYTLsg8wmWK?usp=drive_link"><strong>Windows 다운로드 →</strong></a>
</p>

---

## 목차

- [프로젝트 소개](#프로젝트-소개)
- [주요 기능](#주요-기능)
- [AI 기능](#ai-기능)
- [화면 구성](#화면-구성)
- [기술 스택](#기술-스택)
- [프로젝트 구조](#프로젝트-구조)
- [시작하기](#시작하기)
- [에셋 추가 방법](#에셋-추가-방법)
- [빌드](#빌드)
- [데이터 출처](#데이터-출처)
- [관련 레포지토리](#관련-레포지토리)

---

## 프로젝트 소개

HanokBuilder는 **Unity 6** 기반으로 개발된 한국 전통 건축 공간 3D 제작 도구입니다.

한국문화정보원이 공개한 문화공공데이터를 원천으로 완성형 한옥 건물, 건축 부품, 자연물, 소품, 디지털 휴먼 에셋을 제공하며, **Claude Haiku API**를 통해 자연어 기반 에셋 추천, AI 자동 레이아웃, 씬 편집, 문화재 해설 4가지 AI 기능을 지원합니다.

| 대상 | 활용 방식 |
| --- | --- |
| 게임 개발자 · 인디 팀 | 한국 전통 배경 씬 프로토타입 제작 |
| 교육자 · 학생 | 한국 전통 건축 구조 학습 및 시각 자료 |
| 웹툰 · 일러스트 작가 | 전통 공간 구도 참고용 3D 렌더링 |
| 메타버스 기획자 | 공간 레이아웃 설계 및 검토 |
| 박물관 · 문화기관 | 전통 건축 디지털 아카이브 구성 |

---

## 주요 기능

| 기능 | 설명 |
| --- | --- |
| **에셋 라이브러리** | 완성형 한옥·부품·소품·디지털 휴먼 에셋 썸네일 탐색 및 즉시 배치 |
| **실시간 3D 편집** | 이동 · 회전 · 크기 핸들 조작 및 수치 입력으로 정밀 배치 |
| **4가지 배경 프리셋** | 한옥 마당 · 사랑채 · 조선 장터 · 전통 정원 원클릭 전환 |
| **20단계 되돌리기** | `Ctrl+Z`로 최대 20단계 작업 이력 복원 |
| **복제** | `Ctrl+D`로 선택 에셋을 오른쪽 2m에 즉시 복제 |
| **PNG 캡처** | `P`키로 UI 제외 뷰포트만 바탕화면에 날짜·시간 파일명으로 저장 |
| **카메라 조작** | 오비트 · 패닝 · 줌 · Numpad 고정 시점 · 포커스(`F`/`Z`) |

---

## AI 기능

모든 AI 기능은 **Claude Haiku** (`claude-haiku-4-5-20251001`) 모델을 사용합니다.  
API 키는 [console.anthropic.com](https://console.anthropic.com)에서 발급(`sk-ant-…`)하며, 첫 실행 시 자동 팝업으로 안내됩니다.  
키 없이도 키워드 기반 로컬 추천이 자동 작동합니다.

<br>

<table>
<tr>
<td width="50%">
<img src="https://chunbae-a.github.io/HanokBuilder-guide/assets/screenshots/recommendation-normal.png" alt="AI 추천 패널"/>
</td>
<td width="50%" valign="top">

### 프롬프트 바 구조

```
[ 맵 ]  [ 씬 ]  [  입력 필드  ]  [ ⚙ ]
```

| 버튼 | 역할 |
| --- | --- |
| `맵` | AI 자동 레이아웃 모드 |
| `씬` | 자연어 씬 편집 모드 |
| 기본 | AI 에셋 추천 |
| `⚙` | API 키 설정 |

</td>
</tr>
</table>

### AI 에셋 추천 (기본 모드)

자연어를 입력하면 Claude가 에셋 카탈로그에서 최대 30개를 추천합니다.

| 입력 예시 | 추천 결과 |
| --- | --- |
| `"나무로 둘러싸인 집"` | 수목 · 정자 · 담장 관련 에셋 |
| `"조선 장터 상인과 바구니"` | 행상인 · 전통 수레 · 깃발 에셋 |

### [맵] AI 자동 레이아웃

테마를 입력하면 Claude가 최대 20턴에 걸쳐 건물과 자연물을 단계적으로 배치합니다.

| 입력 예시 | 설계 결과 |
| --- | --- |
| `"소규모 양반가 안채 배치"` | 건물 2~4개 + 자연물 6~10개 |
| `"대규모 궁궐 배치"` | 건물 13~18개 + 자연물 15~25개 |

### [씬] 자연어 씬 편집

씬에 배치된 오브젝트를 자연어로 직접 수정합니다. 대화 맥락을 기억합니다.

| 명령 예시 | 동작 |
| --- | --- |
| `"사랑채를 동쪽으로 5m 옮겨줘"` | X축 +5m 이동 |
| `"정자 삭제해줘"` | 오브젝트 제거 |
| `"연못 하나 추가해줘"` | 카메라 중심에 배치 |

방향 기준: 동쪽 = +X · 서쪽 = −X · 남쪽 = −Z · 북쪽 = +Z

### 문화재 해설 패널

에셋을 클릭하면 역사적 배경 · 건축 특징 · 문화적 의미를 담은 3문장 해설을 즉시 생성합니다.

---

## 화면 구성

<table>
<tr>
<td width="60%">
<img src="https://chunbae-a.github.io/HanokBuilder-guide/assets/screenshots/workspace-overview.png" alt="화면 구성"/>
</td>
<td width="40%" valign="top">

| # | 영역 |
| ---: | --- |
| 1 | 에셋 라이브러리 |
| 2 | 검색 · 카테고리 필터 |
| 3 | 배경 프리셋 패널 |
| 4 | 3D 뷰포트 |
| 5 | 편집 도구 툴바 |
| 6 | 부재 정보 패널 |
| 7 | AI 프롬프트 바 |

</td>
</tr>
</table>

---

## 기술 스택

| 항목 | 내용 |
| --- | --- |
| 엔진 | Unity 6 (6000.3.16f1) |
| 렌더 파이프라인 | Universal Render Pipeline (URP) |
| 입력 시스템 | Unity New Input System |
| AI | Claude Haiku (`claude-haiku-4-5-20251001`) |
| 폰트 | NotoSansKR SDF (TextMesh Pro) |
| 플랫폼 | Windows 64-bit · WebGL |
| 에셋 로드 | `Resources.LoadAll` — `HanokBuilder/Resources/HanokAssets/` |
| 에셋 원천 | 문화포털 메타버스데이터랩 공개 3D 데이터 |

---

## 프로젝트 구조

```
Assets/
└── HanokBuilder/
    ├── Resources/
    │   └── HanokAssets/          ← Prefab 여기에 넣으면 자동 로드
    └── Scripts/
        ├── HanokUIManager.cs     # 메인 UI 관리 (Canvas · 레이아웃 · 선택)
        ├── HanokUIBuilder.cs     # UI 컴포넌트 코드 생성
        ├── HanokCameraController.cs  # 오비트 · 줌 · 뷰 프리셋
        ├── HanokAssetPanel.cs    # 에셋 라이브러리 패널
        ├── HanokEditPanel.cs     # 부재 정보 (위치 · 회전 · 크기) 패널
        ├── HanokAIPanel.cs       # AI 프롬프트 바 · 추천 결과 UI
        ├── HanokAgentLayout.cs   # [맵] AI 자동 레이아웃 에이전트
        ├── HanokSceneEditAgent.cs # [씬] 자연어 씬 편집 에이전트
        ├── HanokGuidePanel.cs    # 문화재 해설 말풍선 패널
        ├── HanokRotationGizmo.cs # 3축 회전 링 기즈모
        ├── HanokScaleGizmo.cs    # 크기 조절 핸들 기즈모
        ├── HanokSceneSetup.cs    # 배경 프리셋 환경 설정
        ├── HanokSelectionHighlight.cs # 선택 에셋 바닥 링 표시
        ├── SelectableAsset.cs    # 씬 오브젝트 클릭 선택 컴포넌트
        ├── HanokAssetInfo.cs     # 에셋 메타데이터 정의
        ├── HanokAssetCategory.cs # 카테고리 분류
        ├── HanokAssetTags.cs     # 검색 태그 데이터
        ├── HanokPlacedAssetMetadata.cs  # 배치 오브젝트 메타
        ├── HanokApiKeyPanel.cs   # API 키 입력 모달
        ├── ClaudeApiConfig.cs    # Claude API 설정
        └── UIDraggablePanel.cs   # 드래그 가능 플로팅 패널
```

---

## 시작하기

### 요구 사항

- Unity **6000.3.16f1** (Unity 6)
- Universal Render Pipeline (URP) 패키지
- Unity New Input System 패키지
- TextMesh Pro (Unity 내장)

### 설치

```bash
git clone https://github.com/Chunbae-A/Unity-3D_Korean_Traditional_Architecture.git
```

Unity Hub에서 `Open` → 클론한 폴더 선택 → Unity 6000.3.16f1로 열기

> **주의**: FBX · 텍스처 등 대용량 에셋은 Git LFS로 관리됩니다. `git lfs pull`로 받아야 에셋이 로드됩니다.

### AI 기능 활성화

1. [console.anthropic.com](https://console.anthropic.com)에서 API 키 발급 (`sk-ant-…`)
2. 앱 실행 시 자동으로 나타나는 모달에 키 입력
3. 이후 프롬프트 바 `⚙` 버튼으로 언제든 변경 가능
4. 키 없이도 키워드 기반 로컬 추천은 작동

---

## 에셋 추가 방법

```
1. FBX 파일을 Unity Project 창으로 드래그 임포트
2. Hierarchy에서 FBX 오브젝트 우클릭 → Prefab → Create Original Prefab
3. 생성된 .prefab 파일을
   Assets/HanokBuilder/Resources/HanokAssets/ 폴더로 이동
4. Play 시 에셋 라이브러리에 자동 표시
```

> **FBX cm 스케일 이슈**: FBX가 cm 단위로 모델링된 경우  
> FBX 임포트 설정 → `Scale Factor = 0.01` 로 설정하세요.

---

## 빌드

### WebGL

```
File → Build Settings → WebGL → Switch Platform → Build
```

빌드 결과물(`index.html` + `Build/`) 을 웹 서버에 업로드하면 브라우저에서 실행됩니다.  
GitHub Pages 배포 시 레포 루트에 결과물을 올리고 Pages를 활성화하세요.

### Windows

```
File → Build Settings → PC, Mac & Linux Standalone
Target Platform: Windows → Architecture: x86_64 → Build
```

---

## 데이터 출처

에셋 라이브러리는 국가기관이 공개한 [문화공공데이터](https://www.culture.go.kr/datametaverse)를 원천으로 합니다.

| 데이터 | 제공 기관 | 플랫폼 |
| --- | --- | --- |
| 건축물 완성형 3D 데이터 | 한국문화정보원 | 문화포털 메타버스데이터랩 |
| 건축물 부품형 3D 데이터 | 한국문화정보원 | 문화포털 메타버스데이터랩 |
| 디지털 휴먼 데이터 | 한국문화정보원 | 문화포털 메타버스데이터랩 |
| 공간소품 3D 오브젝트 | 한국문화정보원 | 문화포털 메타버스데이터랩 |

수집 데이터는 에셋 식별자(`assetKey`), 한글 표시명, 카테고리, 검색 태그로 재구조화해  
라이브러리 검색과 AI 추천에서 동일하게 활용합니다.  
원본 FBX · 텍스처 파일은 Git LFS로 코드와 분리 관리합니다.

---

## 관련 레포지토리

| 레포 | 설명 |
| --- | --- |
| [HanokBuilder-guide](https://github.com/Chunbae-A/HanokBuilder-guide) | 공식 사용자 가이드 사이트 (HTML · CSS · JS) |
