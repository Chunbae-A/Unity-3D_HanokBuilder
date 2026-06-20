# CultureAssetDownloader

문화재관광부 [메타버스데이터랩](https://www.culture.go.kr/datametaverse)의 3D 에셋(FBX, 유니티 패키지)을 자동으로 수집해 Google Drive에 업로드하는 스크립트.

---

## 다운로드 카테고리

| bo_table | 카테고리 |
|---|---|
| `buildings` | 건축물완성형 |
| `building` | 건축물부품형 |
| `digitalhuman` | 디지털휴먼 |
| `object` | 공간소품 |

- **포함**: FBX (`y_view_icon3`) + 유니티 패키지 (`y_view_icon2`)
- **제외**: 언리얼엔진 에셋, 3DS MAX, 스마트머터리얼, 전통문양3D도음

---

## 설치

```bash
pip install -r requirements.txt
```

lxml이 설치되면 HTML 파싱이 자동으로 빨라집니다. 없으면 html.parser로 폴백.

---

## 사용법

### 1. Google Drive 인증 (최초 1회)

```bash
cd Tools/CultureAssetDownloader
python auth_drive.py
```

브라우저가 열리면 Google 계정 로그인 → **고급 → 계속** 클릭 → Drive 권한 허용.  
완료되면 `token.json` 자동 생성.

### 2. 테스트 실행

```bash
python culture_asset_downloader.py --test
python culture_asset_downloader.py --test --test-limit 5
```

`buildings` 카테고리 3개(기본)만 처리해 전체 흐름 확인.

### 3. 전체 실행

```bash
python culture_asset_downloader.py
```

중단 후 재실행하면 `download_progress.json` 기반으로 자동 이어받기.

---

## 파일 구조

```
Tools/CultureAssetDownloader/
├── culture_asset_downloader.py  # 메인 스크립트
├── auth_drive.py                # Drive 최초 인증
├── requirements.txt             # 의존성
├── README.md
├── credentials.json             # Google OAuth 클라이언트 ID (비공개, gitignore)
├── token.json                   # OAuth 토큰 (자동 생성, gitignore)
├── download_progress.json       # 진행 상황 (자동 생성, gitignore)
└── downloader.log               # 실행 로그 (자동 생성, gitignore)
```

---

## 동작 방식

```
목록 수집 (카테고리별 순차)
    ↓
Drive 폴더 기존 파일 일괄 조회 (API 1회)
    ↓
ThreadPoolExecutor (WORKERS=5 병렬)
    ├─ 상세 페이지 → 다운로드 URL 추출
    └─ 다운로드 스트림 → Drive 32MB 청크로 직접 업로드 (로컬 저장 없음)
    ↓
download_progress.json 저장 (이어받기 지원)
```

**로컬 디스크를 사용하지 않습니다.** 사이트에서 받은 데이터를 32MB 청크 단위로 Drive resumable upload API에 직접 전송합니다.

---

## 주요 설정값

`culture_asset_downloader.py` 상단에서 변경 가능합니다.

| 설정 | 기본값 | 설명 |
|---|---|---|
| `WORKERS` | `5` | 동시 처리 에셋 수. 늘리면 빠르지만 서버 차단 위험 |
| `DELAY_BETWEEN_REQUESTS` | `0.3s` | 스레드당 요청 간격 |
| `STREAM_CHUNK` | `32MB` | Drive 업로드 청크 크기 |
| `DRIVE_FOLDER_ID` | (설정됨) | 업로드 대상 Drive 폴더 ID |

---

## Google Cloud Console 설정 (최초 1회)

1. [console.cloud.google.com](https://console.cloud.google.com) 접속
2. `APIs & Services → Library → Google Drive API` 활성화
3. `Credentials → + CREATE CREDENTIALS → OAuth client ID → Desktop app`
4. 다운로드한 JSON 파일을 `credentials.json`으로 이름 변경 후 이 폴더에 저장
5. `OAuth consent screen → Test users`에 사용할 Google 계정 추가
