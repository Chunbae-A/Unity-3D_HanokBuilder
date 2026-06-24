# HanokBuilder — 에셋 파이프라인 도구 가이드

한국 전통 건축 3D 에셋을 **문화포털 → Google Drive → Unity 프로젝트**까지 자동으로 처리하는 도구 모음입니다.

---

## 전체 워크플로 한눈에 보기

```
[문화포털 메타버스데이터랩]
        │  culture_asset_downloader.py  (최초 또는 업데이트 시)
        ▼
[Google Drive: 에셋 원본 저장소]
  https://drive.google.com/drive/folders/1J92prWdMR6HYr7WAaeIN-gsvgldHGNh9
        │  pipeline.py  (Phase 1)
        ▼
[로컬: Assets/HanokBuilder/Resources/HanokAssets/]
  ├── 건축물완성형/
  ├── 건축물부품형/
  ├── 디지털휴먼/
  ├── 공간소품/
  └── SharedTextures/
        │  Unity 에디터 3단계
        ▼
[Unity: FBX → Prefab + Material 변환]
        │  pipeline.py --phase2
        ▼
[Assets/HanokBuilder/Resources/HanokManifest/*.json  &  Prefab GUID 복구]
        │
        ▼
[게임 내 에셋 라이브러리 완성]
```

---

## 사전 준비

### Python 패키지 설치

```bash
# CultureAssetDownloader 의존성
pip install -r Tools/CultureAssetDownloader/requirements.txt

# DriveAssetSync 의존성 (CultureAssetDownloader와 대부분 공유)
pip install -r Tools/DriveAssetSync/requirements.txt
```

### Google Drive OAuth 인증 (최초 1회)

1. [Google Cloud Console](https://console.cloud.google.com) 접속
2. `APIs & Services → Library → Google Drive API` 활성화
3. `Credentials → + CREATE CREDENTIALS → OAuth client ID → Desktop app`
4. 다운로드한 JSON 파일을 `credentials.json`으로 이름 변경 후 `Tools/CultureAssetDownloader/`에 저장
5. `OAuth consent screen → Test users`에 사용할 Google 계정 추가

```bash
cd Tools/CultureAssetDownloader
python auth_drive.py
```

브라우저에서 Google 계정 로그인 → **고급 → 계속** → Drive 권한 허용.  
완료되면 `token.json`이 자동 생성됩니다. `DriveAssetSync`도 같은 파일을 공유합니다.

---

## Phase 0 — 문화포털에서 Drive로 다운로드

> 에셋이 이미 Drive에 있다면 이 단계는 건너뜁니다.

```bash
# 전체 카테고리 다운로드 (건축물완성형·부품형·디지털휴먼·공간소품)
python Tools/CultureAssetDownloader/culture_asset_downloader.py

# 테스트 실행 (buildings 3개만)
python Tools/CultureAssetDownloader/culture_asset_downloader.py --test

# 재시작 시 이어받기: 별도 조작 불필요. download_progress.json이 자동으로 처리합니다.
```

**동작 방식:** 문화포털 목록 페이지를 순회하여 FBX 다운로드 URL을 추출한 뒤,  
파일을 로컬에 저장하지 않고 32MB 청크 단위로 Google Drive에 직접 스트리밍 업로드합니다.

| 설정 | 기본값 | 설명 |
|---|---|---|
| `WORKERS` | 5 | 동시 처리 에셋 수 |
| `DELAY_BETWEEN_REQUESTS` | 0.3s | 스레드당 요청 간격 |
| `STREAM_CHUNK` | 32MB | Drive 업로드 청크 크기 |

---

## Phase 1 — Drive → 로컬 후처리

### 통합 실행 (권장)

```bash
# 프로젝트 루트에서 실행
python Tools/pipeline.py
```

Drive 동기화 + 정리 + 텍스처 이동을 순서대로 실행합니다.

### 개별 실행

Drive 동기화만 다시 실행하거나, 특정 단계만 반복할 때 사용합니다.

#### Step 1 — Drive → HanokAssets 동기화

```bash
python Tools/DriveAssetSync/drive_asset_sync.py

# 특정 카테고리만
python Tools/DriveAssetSync/drive_asset_sync.py --category 건축물완성형

# 진행 기록 초기화 후 전체 재다운로드
python Tools/DriveAssetSync/drive_asset_sync.py --reset
```

Drive의 카테고리 폴더(`건축물완성형` / `건축물부품형` / `디지털휴먼` / `공간소품`)를  
`Assets/HanokBuilder/Resources/HanokAssets/` 아래 같은 이름의 폴더로 동기화합니다.  
ZIP 파일은 다운로드 즉시 압축 해제 후 삭제됩니다.

#### Step 2 — 불필요 파일 정리 + 중복 텍스처 통합

```bash
python Tools/cleanup_hanokassets.py
```

| 내부 단계 | 처리 내용 |
|---|---|
| 1/3 | 중첩 Unity 프로젝트 폴더(`Library` · `Packages` · `ProjectSettings`) 삭제 |
| 2/3 | `.unitypackage` · `.spp` 파일 삭제 |
| 3/3 | `dedup_textures.py` 호출 — 동일 파일명 텍스처를 `SharedTextures/`로 통합, 중복 삭제 |

#### Step 3 — 한글 .fbm 텍스처 이동

```bash
python Tools/move_korean_fbm_textures.py
```

Unity AssetDatabase가 한글·괄호 포함 폴더명을 인덱싱하지 못하는 문제를 해결합니다.  
`.fbm` 폴더 중 한글 또는 괄호가 포함된 것의 텍스처를 `SharedTextures/`로 이동하고 빈 폴더를 삭제합니다.

---

## Unity 에디터 3단계 (Phase 1 → Phase 2 사이)

Phase 1 완료 후 Unity 에디터에서 아래 세 단계를 순서대로 실행합니다.

```
[1] HanokBuilder > Tools > Extract & Upgrade FBX Materials
    FBX 머티리얼 추출 + SharedTextures 연결

[2] Window > Rendering > Render Pipeline Converter
    Material Upgrade 체크 → Initialize and Convert
    (Standard → URP 변환, 흰색 메시 문제 해결)

[3] HanokBuilder > Tools > Import Culture Assets
    FBX → Prefab + AssetInfo ScriptableObject 일괄 생성
```

---

## Phase 2 — Prefab 후처리 (Unity Import 완료 후)

```bash
python Tools/pipeline.py --phase2
```

#### Step 4 — 매니페스트 생성

```bash
python Tools/generate_culture_manifests.py
```

각 카테고리 폴더의 `Prefabs/` 디렉터리를 스캔하여  
`Assets/HanokBuilder/Resources/HanokManifest/{카테고리}.json` 파일을 생성합니다.

생성된 JSON에는 각 Prefab의 `key` (파일명) · `display` (한글 표시명) · `sub` (서브카테고리) · `path` (Resources 경로)가 포함됩니다.

Unity에서 `HanokBuilder > Tools > Reload Culture Manifests`를 실행해 반영합니다.

#### Step 5 — Prefab GUID 복구

```bash
python Tools/fix_cm_prefab_guids.py
```

`CM_` 접두사 Prefab Variant의 `m_SourcePrefab` GUID가 FBX 재임포트 후 깨지는 문제를 복구합니다.  
현재 `.fbx.meta` 파일의 실제 GUID를 읽어 `.prefab` 파일의 참조를 교체합니다.

> **주의:** Unity 에디터가 열려 있으면 닫고 실행한 뒤 다시 여세요.

---

## 전체 파이프라인 CLI 요약

```bash
# Phase 1 전체 (Drive 동기화 포함)
python Tools/pipeline.py

# Phase 1 (Drive 동기화 건너뜀 — 이미 동기화된 경우)
python Tools/pipeline.py --skip-sync

# Phase 2만 (Unity Import 완료 후)
python Tools/pipeline.py --phase2

# Phase 1 + Phase 2 연속 실행 (Unity 단계는 중간에 수동 진행)
python Tools/pipeline.py --all
```

---

## 도구별 파일 위치

```
Tools/
├── pipeline.py                          ← 통합 실행기 (여기서 시작)
├── GUIDE.md                             ← 이 문서
├── cleanup_hanokassets.py               ← 불필요 파일 정리 + 중복 텍스처 통합
├── dedup_textures.py                    ← 중복 텍스처 제거 (cleanup이 내부 호출)
├── move_korean_fbm_textures.py          ← 한글 .fbm 텍스처 → SharedTextures
├── generate_culture_manifests.py        ← Prefab 스캔 → JSON 매니페스트
├── fix_cm_prefab_guids.py               ← CM_ Prefab GUID 복구
│
├── CultureAssetDownloader/
│   ├── culture_asset_downloader.py      ← 문화포털 → Drive 다운로더
│   ├── auth_drive.py                    ← Drive OAuth 인증 (최초 1회)
│   ├── requirements.txt
│   ├── credentials.json                 ← Google OAuth 클라이언트 ID (비공개)
│   └── token.json                       ← OAuth 토큰 (자동 생성)
│
└── DriveAssetSync/
    ├── drive_asset_sync.py              ← Drive → 로컬 HanokAssets 동기화
    └── requirements.txt
```

---

## Google Drive 에셋 폴더

에셋 원본(FBX)은 아래 Google Drive 폴더에서 관리됩니다.

```
https://drive.google.com/drive/folders/1J92prWdMR6HYr7WAaeIN-gsvgldHGNh9
```

| Drive 폴더명 | 로컬 경로 |
|---|---|
| 건축물완성형 | Assets/HanokBuilder/Resources/HanokAssets/건축물완성형/ |
| 건축물부품형 | Assets/HanokBuilder/Resources/HanokAssets/건축물부품형/ |
| 디지털휴먼   | Assets/HanokBuilder/Resources/HanokAssets/디지털휴먼/ |
| 공간소품     | Assets/HanokBuilder/Resources/HanokAssets/공간소품/ |
