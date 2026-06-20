# 문화재관광부 메타버스데이터랩 FBX 자동 다운로더

문화재관광부 [메타버스데이터랩](https://www.culture.go.kr/datametaverse)에서 한국 전통 문화유산 3D 에셋(FBX)을 자동으로 전수 다운로드하고 Google Drive에 업로드하는 스크립트입니다.

---

## 다운로드 대상 카테고리

| bo_table | 카테고리명 | 설명 |
|----------|-----------|------|
| `modeling` | 전통문양3D도음 | 전통 문양 3D 모델 (표면문양, 모델형, 건축재료) |
| `buildings` | 건축물완성형 | 한국 전통 건축물 완성형 3D 스캔 에셋 |
| `building` | 건축물부품형 | 한국 전통 건축물 부품(기와, 기둥 등) 에셋 |
| `digitalhuman` | 디지털휴먼 | 한국 전통 복식 디지털 휴먼 캐릭터 |
| `object` | 공간소품 | 전통 공간 소품 3D 오브젝트 |

---

## 필터링 규칙

- **제외**: 에셋 제목에 `언리얼엔진` 포함 시 자동 제외
- **포함**: FBX 파일만 다운로드 (`.max` 3DS MAX 파일 제외)
- 언리얼엔진 미포함 에셋(FBX 원본)은 모두 포함

---

## 사이트 구조 분석 결과

### 목록 페이지
- URL 패턴: `https://www.culture.go.kr/datametaverse/bbs/board.php?bo_table={카테고리}&page={N}`
- 에셋 항목 CSS 선택자: `li.y_sub_list1_img.itemBox`
- 에셋 제목: `h5.downloadTitle span`
- 페이지네이션: `a.pg_end` href에서 마지막 페이지 번호 추출
- **중요**: 메인 페이지(`/datametaverse/`) 먼저 방문해 PHPSESSID + IP해시 쿠키 획득 필요

### 상세 페이지
- URL 패턴: `?bo_table={카테고리}&wr_id={번호}`
- 다운로드 버튼 CSS: `li.downloadBtn a[download]`
- FBX 파일 경로 패턴: `https://www.culture.go.kr/datametaverse/data/file/{bo_table}/{파일명}.fbx`

---

## 파일 구조

```
Tools/CultureAssetDownloader/
├── culture_asset_downloader.py   # 메인 다운로드 스크립트
├── auth_drive.py                 # Google Drive OAuth 인증 전용
├── credentials.json              # Google OAuth 클라이언트 ID (비공개)
├── token.json                    # Google OAuth 토큰 (자동 생성, 비공개)
├── README.md                     # 이 파일
└── logs/
    ├── downloader.log            # 실행 로그
    └── download_progress.json    # 진행 상황 (이어받기용)
```

> `credentials.json`, `token.json`은 `.gitignore`에 등록되어 있어 git에 업로드되지 않습니다.

---

## 사용 방법

### 1. 의존성 설치

```bash
pip install requests beautifulsoup4 tqdm google-auth google-auth-oauthlib google-auth-httplib2 google-api-python-client
```

### 2. Google Drive 인증 (최초 1회)

```bash
cd Tools/CultureAssetDownloader
python auth_drive.py
```

브라우저 창이 열리면:
1. Google 계정 로그인
2. "Google에서 확인하지 않은 앱" 경고 → **고급** → **계속** 클릭
3. Drive 권한 허용

인증 완료 후 `token.json`이 자동 생성됩니다.

### 3. 테스트 실행 (buildings 카테고리 3개)

```bash
python culture_asset_downloader.py --test
python culture_asset_downloader.py --test --test-limit 5
```

### 4. 전체 실행

```bash
python culture_asset_downloader.py
```

다운로드 파일은 `./downloaded_fbx/{카테고리명}/` 에 저장됩니다.

---

## 백그라운드 실행 방법

파일 크기가 수 GB이므로 시간이 오래 걸립니다. 터미널을 닫아도 계속 실행되게 하려면 아래 방법을 사용하세요.

### 방법 1 — 터미널 그냥 켜두기 (제일 간단)

```bash
python culture_asset_downloader.py
```

창을 닫지 않고 두면 됩니다. 중간에 끊겨도 `download_progress.json`으로 자동 이어받기됩니다.

### 방법 2 — 창 없이 조용히 실행 (Windows)

```powershell
pythonw culture_asset_downloader.py
```

콘솔 창 없이 백그라운드로 실행됩니다. 로그는 `downloader.log`에서 확인하세요.

```powershell
# 진행 상황 실시간 확인
Get-Content downloader.log -Wait -Tail 20

# 종료할 때
Get-Process python | Stop-Process
```

### 방법 3 — 창 없이 조용히 실행 (macOS / Linux)

```bash
nohup python culture_asset_downloader.py &

# 로그 실시간 확인
tail -f downloader.log

# 종료할 때
kill $(pgrep -f culture_asset_downloader.py)
```

---

## 주요 동작 방식

```
메인 페이지 방문 (쿠키 획득)
    ↓
카테고리별 목록 전체 페이지 수집
    ↓
언리얼엔진 에셋 필터링 (제외)
    ↓
상세 페이지에서 FBX URL 추출
    ↓
FBX 다운로드 (최대 3회 재시도, 1.5초 딜레이)
    ↓
Google Drive 카테고리별 폴더에 업로드
    ↓
download_progress.json에 진행 상황 저장 (이어받기 지원)
```

---

## Google Drive 업로드 설정

- **대상 폴더 ID**: `1J92prWdMR6HYr7WAaeIN-gsvgldHGNh9`
- 카테고리별 하위 폴더 자동 생성
- 이미 업로드된 파일은 중복 업로드 건너뜀

### Google Cloud Console 설정 방법
1. [console.cloud.google.com](https://console.cloud.google.com) 접속
2. `APIs & Services` → `Library` → `Google Drive API` 활성화
3. `Credentials` → `+ CREATE CREDENTIALS` → `OAuth client ID` → `Desktop app`
4. 다운로드한 JSON을 `credentials.json`으로 저장
5. `OAuth consent screen` → `Test users`에 사용 계정 추가

---

## 참고사항

- 파일 1개당 수 GB 크기 (3D 스캔 고해상도 데이터)
- 전체 다운로드 시 수백 GB 예상
- 서버 부하 방지를 위해 요청 간 1.5초 딜레이 적용
- 중간에 중단해도 `download_progress.json` 기반으로 이어받기 가능
