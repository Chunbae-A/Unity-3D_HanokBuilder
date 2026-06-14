using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// HanokBuilder — 메인 관리자 (상태·로직)
/// 파일 구성:
///   HanokUIBuilder.cs  — Canvas/패널 생성
///   HanokAssetPanel.cs — 왼쪽 에셋 목록
///   HanokEditPanel.cs  — 오른쪽 정보+편집 패널
/// </summary>
public partial class HanokUIManager : MonoBehaviour
{
    [Header("폰트")]
    public TMP_FontAsset koreanFont;

    // ── 뷰포트 툴 모드 ────────────────────────────────────
    public enum EditTool { Select, Rotate, Scale, Delete }
    EditTool currentTool = EditTool.Select;

    // ── 내부 상태 ─────────────────────────────────────────
    GameObject     selectedObject;
    Transform      assetContent;
    Button[]       toolBtns;
    Button[]       _bgBtns;

    RectTransform leftPanelRT;
    RectTransform _leftExpandBtnRT;

    TMP_Text infoNameText;
    TMP_InputField posX, posY, posZ;
    TMP_InputField rotX, rotY, rotZ;
    TMP_InputField scaleF;

    // ── 실행 취소 ─────────────────────────────────────────
    struct UndoEntry
    {
        public enum Op { Move, Spawn }
        public Op op;
        public GameObject obj;
        public Vector3 prevPos;
        public Quaternion prevRot;
    }
    System.Collections.Generic.List<UndoEntry> _undoStack = new();

    // ── 드래그 상태 ──────────────────────────────────────
    bool    _isDragging;
    bool    _pendingDrag;
    Vector2 _dragStartMouse;
    Vector3 _dragOffset;
    Plane   _dragPlane;

    // ── 스케일 핸들 (뷰포트 플로팅 위젯) ─────────────────
    GameObject    _scaleHandleGO;
    RectTransform _scaleHandleRT;
    TMP_Text      _scaleHandleText;
    Image         _scaleHandleImg;
    RectTransform _canvasRT;
    bool          _shDragging;
    Vector2       _shDragPrev;
    const float   SH_FACTOR = 0.006f;   // px → scale 배율

    // ── 기즈모 ──────────────────────────────────────────
    HanokRotationGizmo _rotGizmo;
    HanokScaleGizmo    _scaleGizmo;

    // ── 캡처·토스트·뷰 배지·격자 ─────────────────────────
    GameObject  _captureFlash;
    GameObject  _toastGO;
    TMP_Text    _toastText;
    TMP_Text    _viewBadgeText;
    bool        _capturing    = false;
    Coroutine   _toastRoutine;

    // ── 라이트 테마 색상 팔레트 ───────────────────────────
    static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out Color c); return c; }

    // 기반
    static readonly Color BG_ROOT    = Hex("#EAE6DF");
    static readonly Color BG_PANEL   = Hex("#FFFFFF");
    static readonly Color BG_CARD    = Hex("#F7F4EF");
    static readonly Color BG_INPUT   = Hex("#EDEAE4");
    static readonly Color BORDER     = Hex("#D4CFC8");

    // 브랜드
    static readonly Color NAVY       = Hex("#1B3A6B");
    static readonly Color NAVY_LIGHT = Hex("#2C5282");
    static readonly Color FOREST     = Hex("#3D6B4F");
    static readonly Color GOLD       = Hex("#9A7228");

    // 텍스트
    static readonly Color TEXT_H     = Hex("#1A1A1A");
    static readonly Color TEXT_MAIN  = Hex("#333333");
    static readonly Color TEXT_SUB   = Hex("#888888");
    static readonly Color TEXT_HINT  = Hex("#BBBBBB");

    // 버튼
    static readonly Color BTN_PRI    = Hex("#1B3A6B");
    static readonly Color BTN_SEC    = Hex("#3D6B4F");
    static readonly Color BTN_DANGER = Hex("#B03030");
    static readonly Color BTN_GHOST  = Hex("#E8E4DC");

    // 축
    static readonly Color COL_X = Hex("#C0392B");
    static readonly Color COL_Y = Hex("#27AE60");
    static readonly Color COL_Z = Hex("#2980B9");

    const string ASSET_PATH     = "HanokAssets";
    const string CATEGORY_PATH  = "HanokCategories";
    const string ASSETINFO_PATH = "HanokAssetInfo";
    const int    THUMB_LAYER = 31;
    const string KOREAN_FONT_WARMUP = "가나다라마바사아자차카타파하한글한옥배치편집모듈라이브러리검색위치회전크기삭제복제선택해제문화해설";

    // ── 생명주기 ──────────────────────────────────────────
    // 씬에 HanokUIManager가 중복 배치된 경우(머지로 인한 잔존 오브젝트 등)
    // 두 번째 이후 인스턴스는 UI·씬 환경을 다시 만들지 않도록 비활성화한다.
    static HanokUIManager _activeInstance;

    void Start()
    {
        if (_activeInstance != null && _activeInstance != this)
        {
            Debug.LogWarning($"[HanokUIManager] 씬에 중복된 HanokUIManager('{name}')가 있어 비활성화합니다.");
            enabled = false;
            return;
        }
        _activeInstance = this;

        if (!IsUsableKoreanFont(koreanFont))
            koreanFont = Resources.Load<TMP_FontAsset>("NotoSansKR-Regular SDF")
                      ?? Resources.Load<TMP_FontAsset>("MalgunGothic SDF");

        // ── 한국어 동적 폰트 초기화 ─────────────────────────────
        InitKoreanFont();

        // 씬 환경 초기화 (바닥·조명·카메라 배경)
        HanokSceneSetup.Setup();

        // 카메라 컨트롤러 자동 추가
        if (Camera.main != null &&
            Camera.main.GetComponent<HanokCameraController>() == null)
            Camera.main.gameObject.AddComponent<HanokCameraController>();

        // 기즈모 생성
        _rotGizmo   = gameObject.AddComponent<HanokRotationGizmo>();
        _scaleGizmo = gameObject.AddComponent<HanokScaleGizmo>();

        BuildUI();
        LoadAssets();
        StartCoroutine(ForceLayout());
    }

    void Update()
    {
        SyncTransformInputs();
        HandleViewportScale();
        HandleScaleHandleDrag();
        if (!_shDragging) HandleViewportClick();
        HandleKeyboardShortcuts();
        UpdateScaleHandle();
        UpdateViewBadge();
    }

    // ── Ctrl+스크롤 → 선택 오브젝트 크기 조절 ────────────
    void HandleViewportScale()
    {
        if (selectedObject == null) return;
        var kb = Keyboard.current;
        if (kb == null || !kb.ctrlKey.isPressed) return;
        var mouse = Mouse.current;
        if (mouse == null) return;

        bool overUI = UnityEngine.EventSystems.EventSystem.current != null &&
                      UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        if (overUI) return;

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) < 0.01f) return;

        float factor   = scroll > 0 ? 1.12f : (1f / 1.12f);
        float newScale = Mathf.Clamp(selectedObject.transform.localScale.x * factor, 0.1f, 200f);
        selectedObject.transform.localScale = Vector3.one * newScale;
    }

    // ── 스케일 핸들: 위치 갱신 + 호버 효과 ──────────────
    void UpdateScaleHandle()
    {
        if (_scaleHandleGO == null || _canvasRT == null) return;
        if (selectedObject == null || Camera.main == null)
        { _scaleHandleGO.SetActive(false); return; }

        _scaleHandleGO.SetActive(true);

        // 오브젝트 바운드 최상단 월드 좌표
        var rends = selectedObject.GetComponentsInChildren<Renderer>();
        Vector3 worldTop;
        if (rends.Length > 0)
        {
            var b = rends[0].bounds;
            foreach (var r in rends) b.Encapsulate(r.bounds);
            worldTop = new Vector3(b.center.x, b.max.y, b.center.z);
        }
        else worldTop = selectedObject.transform.position + Vector3.up * 2f;

        Vector3 sp = Camera.main.WorldToScreenPoint(worldTop);
        if (sp.z <= 0f) { _scaleHandleGO.SetActive(false); return; }

        // 스크린 좌표 → 캔버스 로컬 좌표
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRT, new Vector2(sp.x, sp.y), null, out Vector2 lp);
        _scaleHandleRT.anchoredPosition = lp + new Vector2(0f, 30f);

        // 호버 색상
        if (!_shDragging && Mouse.current != null)
        {
            bool hov = RectTransformUtility.RectangleContainsScreenPoint(
                           _scaleHandleRT, Mouse.current.position.ReadValue());
            _scaleHandleImg.color = hov
                ? new Color(0.12f, 0.74f, 1.00f, 1.00f)
                : new Color(0.10f, 0.62f, 0.92f, 0.95f);
        }

        if (_scaleHandleText != null)
            _scaleHandleText.text = "↔  " + selectedObject.transform.localScale.x.ToString("F1") + "×";
    }

    // ── 스케일 핸들: 드래그 처리 ─────────────────────────
    void HandleScaleHandleDrag()
    {
        if (_scaleHandleGO == null || !_scaleHandleGO.activeSelf) return;
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 mp = mouse.position.ReadValue();

        if (!_shDragging)
        {
            if (mouse.leftButton.wasPressedThisFrame &&
                RectTransformUtility.RectangleContainsScreenPoint(_scaleHandleRT, mp))
            {
                _shDragging = true;
                _shDragPrev = mp;
                if (_scaleHandleImg != null)
                    _scaleHandleImg.color = new Color(0.06f, 0.46f, 0.72f, 1.00f);
            }
        }
        else
        {
            if (mouse.leftButton.isPressed && selectedObject != null)
            {
                float dx = mp.x - _shDragPrev.x;
                if (Mathf.Abs(dx) > 0.3f)
                {
                    float factor  = 1f + dx * SH_FACTOR;
                    float newScale = Mathf.Clamp(
                        selectedObject.transform.localScale.x * factor, 0.1f, 200f);
                    selectedObject.transform.localScale = Vector3.one * newScale;
                }
                _shDragPrev = mp;
            }
            else
            {
                _shDragging = false;
                if (_scaleHandleImg != null)
                    _scaleHandleImg.color = new Color(0.10f, 0.62f, 0.92f, 0.95f);
            }
        }
    }

    // 카메라 컨트롤러에서 Ctrl+스크롤 소비 여부 확인
    public bool IsScaleScrollConsumed()
        => selectedObject != null
        && Keyboard.current != null && Keyboard.current.ctrlKey.isPressed;

    // ── 한국어 폰트 초기화 (다단계 폴백) ────────────────────
    void InitKoreanFont()
    {
        if (IsUsableKoreanFont(koreanFont))
        {
            Debug.Log($"[KorFont] Existing Korean font OK: {koreanFont.name}");
            return;
        }

        // 1단계: 프로젝트 내 MalgunGothic.ttf로 Dynamic TMP 폰트 생성
        // Resources.Load<Font> → 실제 TTF 바이너리 포함 → FreeType이 한글 글리프를 온디맨드 렌더링
        string bundledMalgun = Path.Combine(Application.dataPath, "HanokBuilder/Resources/MalgunGothic.ttf");
        Font ttf = IsLikelyFontFile(bundledMalgun) ? Resources.Load<Font>("MalgunGothic") : null;
        if (ttf != null)
        {
            var dyn = TMP_FontAsset.CreateFontAsset(ttf);

            if (PrepareKoreanFont(dyn))
            {
                dyn.name = "KorDynamic";
                koreanFont = dyn;
                Debug.Log("[KorFont] Dynamic font from MalgunGothic.ttf OK");
                return;
            }

            Debug.Log("[KorFont] CreateFontAsset(ttf) returned no usable Korean glyphs");
        }
        else Debug.Log("[KorFont] Bundled MalgunGothic.ttf is missing or still a Git LFS pointer; using OS Korean font");

        // 2단계: Git LFS 폰트 파일이 내려오지 않은 경우 OS 기본 한글 폰트로 Dynamic TMP 폰트 생성
        string[] osKoreanFonts =
        {
            "Apple SD Gothic Neo",
            "AppleGothic",
            "Malgun Gothic",
            "맑은 고딕",
            "Noto Sans CJK KR",
            "Noto Sans KR",
            "NanumGothic"
        };

        string home = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
        string[] osKoreanFontFiles =
        {
            Path.Combine(home, "Library/Fonts/NotoSansCJKkr-Regular.otf"),
            Path.Combine(home, "Library/Fonts/Pretendard-Regular.ttf"),
            Path.Combine(home, "Library/Fonts/NanumSquareNeo-bRg.ttf"),
            Path.Combine(home, "Library/Fonts/NANUMSQUARER.TTF"),
            "/System/Library/Fonts/AppleSDGothicNeo.ttc",
            "/System/Library/Fonts/Supplemental/AppleGothic.ttf",
            "/System/Library/Fonts/Supplemental/NotoSansGothic-Regular.ttf"
        };

        foreach (var path in osKoreanFontFiles)
        {
            if (!File.Exists(path)) continue;

            for (int faceIndex = 0; faceIndex < 16; faceIndex++)
            {
                var dyn = TMP_FontAsset.CreateFontAsset(
                    path, faceIndex, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
                if (PrepareKoreanFont(dyn))
                {
                    dyn.name = "KorDynamicFile";
                    koreanFont = dyn;
                    Debug.Log($"[KorFont] Dynamic font from file OK: {path} face {faceIndex}");
                    return;
                }
            }
        }

        string[] osFontStyles = { "Regular", "Normal", "Medium" };
        foreach (var family in osKoreanFonts)
        {
            foreach (var style in osFontStyles)
            {
                var dyn = TMP_FontAsset.CreateFontAsset(family, style, 90);
                if (PrepareKoreanFont(dyn))
                {
                    dyn.name = "KorDynamicOS";
                    koreanFont = dyn;
                    Debug.Log($"[KorFont] Dynamic OS font OK: {family} {style}");
                    return;
                }
            }
        }

        // 3단계: 실패 시 static baked 폰트 그대로 유지 (atlasPopulationMode 등 절대 수정 금지)
        if (!IsUsableKoreanFont(koreanFont))
        {
            string fontName = koreanFont != null ? koreanFont.name : "null";
            Debug.LogWarning($"[KorFont] No usable Korean TMP font found. Current font: {fontName}");
            koreanFont = null;
            return;
        }

        Debug.Log($"[KorFont] Fallback: static font {koreanFont.name} ({koreanFont.characterTable.Count} chars)");
    }

    bool IsUsableKoreanFont(TMP_FontAsset font)
    {
        if (font == null) return false;
        return font.HasCharacter('가') && font.HasCharacter('한');
    }

    bool PrepareKoreanFont(TMP_FontAsset font)
    {
        if (font == null) return false;
        if (IsUsableKoreanFont(font)) return true;
        if (font.atlasPopulationMode == AtlasPopulationMode.Static) return false;
        font.TryAddCharacters(KOREAN_FONT_WARMUP);
        return IsUsableKoreanFont(font);
    }

    bool IsLikelyFontFile(string path)
    {
        try
        {
            return File.Exists(path) && new FileInfo(path).Length > 1024;
        }
        catch
        {
            return false;
        }
    }

    void OnDestroy()
    {
        if (_thumbCam != null) Destroy(_thumbCam.gameObject);
        // 씬 종료 시 남은 썸네일 RenderTexture GPU 메모리 해제
        if (assetContent != null)
            foreach (var ri in assetContent.GetComponentsInChildren<RawImage>())
                if (ri.texture is RenderTexture rt) { rt.Release(); Destroy(rt); }
    }

    IEnumerator ForceLayout()
    {
        yield return null; yield return null;
        foreach (var f in FindObjectsByType<ContentSizeFitter>(FindObjectsSortMode.None))
            LayoutRebuilder.ForceRebuildLayoutImmediate(f.GetComponent<RectTransform>());
    }

    // ── 배경 프리셋 전환 ──────────────────────────────────
    public void SelectBgPreset(int idx)
    {
        HanokSceneSetup.SetPreset(idx);
        if (_bgBtns == null) return;
        for (int i = 0; i < _bgBtns.Length; i++)
        {
            bool sel = (i == idx);
            _bgBtns[i].GetComponent<Image>().color = sel ? NAVY : BTN_GHOST;
            foreach (var txt in _bgBtns[i].GetComponentsInChildren<TMP_Text>())
            {
                bool bold = txt.fontStyle.HasFlag(FontStyles.Bold);
                txt.color = sel
                    ? (bold ? Color.white : new Color(1f, 1f, 1f, 0.65f))
                    : (bold ? TEXT_MAIN   : TEXT_HINT);
            }
        }
    }

    // ── 툴 전환 ───────────────────────────────────────────
    public void SetTool(EditTool tool)
    {
        currentTool = tool;
        RefreshToolBtns();
        SyncGizmo();
    }

    void SyncGizmo()
    {
        // 회전 기즈모 — Rotate 도구일 때만
        if (_rotGizmo != null)
        {
            _rotGizmo.onDragEnd = PlaceOnFloor; // 회전 후 바닥 자동 스냅
            if (currentTool == EditTool.Rotate && selectedObject != null)
                _rotGizmo.Attach(selectedObject);
            else
                _rotGizmo.Detach();
        }
        // 스케일 기즈모 — Select 도구 + 오브젝트 선택 시
        if (_scaleGizmo != null)
        {
            _scaleGizmo.onDragEnd = PlaceOnFloor; // 크기 변경 후 바닥 자동 스냅
            if (currentTool == EditTool.Scale && selectedObject != null)
                _scaleGizmo.Attach(selectedObject);
            else
                _scaleGizmo.Detach();
        }
    }

    void RefreshToolBtns()
    {
        if (toolBtns == null) return;
        for (int i = 0; i < toolBtns.Length; i++)
        {
            bool active = (i == (int)currentTool);
            var  tool   = (EditTool)i;
            bool isDel  = (tool == EditTool.Delete);

            // 다크 패널 기반 버튼 색상
            toolBtns[i].GetComponent<Image>().color =
                active ? new Color(1f, 1f, 1f, 0.15f) : Color.clear;

            var texts = toolBtns[i].GetComponentsInChildren<TMP_Text>();
            foreach (var txt in texts)
            {
                bool isIcon = txt.fontSize >= 10f; // 메인라벨(12) vs 단축키힌트(8) 구분
                if (active)
                {
                    txt.color = isIcon
                        ? (isDel ? new Color(1f, 0.55f, 0.52f) : Color.white)
                        : new Color(1f, 1f, 1f, 0.55f);
                }
                else
                {
                    txt.color = isIcon
                        ? (isDel ? new Color(1f, 0.50f, 0.46f) : new Color(1f, 1f, 1f, 0.35f))
                        : new Color(1f, 1f, 1f, 0.18f);
                }
            }
        }
    }

    // ── 에셋 배치 ─────────────────────────────────────────
    public void Spawn(GameObject prefab)
    {
        var obj = Instantiate(prefab, Vector3.zero, prefab.transform.rotation);
        obj.name = prefab.name;

        // FBX Scale Factor 100 자동 보정 (단위: cm → m)
        if (obj.transform.localScale.magnitude > 50f)
            obj.transform.localScale = Vector3.one;

        OptimizeRenderers(obj);

        var sp = GetSpawnPos();
        obj.transform.position = new Vector3(sp.x, 0f, sp.z);

        AttachSelectable(obj);
        PushUndoSpawn(obj);
        SelectObject(obj);

        var camCtrl = Camera.main?.GetComponent<HanokCameraController>();
        // bounds 계산은 다음 프레임에 — 동일 프레임 내 transform 변경 후 bounds가 미갱신되는 문제 방지
        StartCoroutine(FinishSpawn(obj, camCtrl));
    }

    // 지정한 위치에 배치 — AI 추천 다중 배치에 사용
    public GameObject SpawnAt(GameObject prefab, Vector3 position)
    {
        var obj = Instantiate(prefab, Vector3.zero, prefab.transform.rotation);
        obj.name = prefab.name;
        if (obj.transform.localScale.magnitude > 50f)
            obj.transform.localScale = Vector3.one;
        OptimizeRenderers(obj);
        obj.transform.position = position;
        PlaceOnFloor(obj);
        EnsureCollider(obj);
        AttachSelectable(obj);
        PushUndoSpawn(obj);
        return obj;
    }

    IEnumerator FinishSpawn(GameObject obj, HanokCameraController camCtrl)
    {
        yield return null; // 한 프레임 대기 → Renderer.bounds 갱신 보장
        if (obj == null) yield break;
        PlaceOnFloor(obj);
        EnsureCollider(obj);
        camCtrl?.FocusObject(obj);
    }

    // 모델 바닥면(bounds.min.y)이 Y=0에 오도록 위치 조정
    public void PlaceOnFloor(GameObject obj)
    {
        var rends = obj.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return;
        var b = rends[0].bounds;
        foreach (var r in rends) b.Encapsulate(r.bounds);
        obj.transform.position += Vector3.up * (-b.min.y);
    }

    Vector3 GetSpawnPos()
    {
        if (Camera.main == null) return Vector3.zero;
        // 화면 정중앙 레이캐스트 — 씬 어디든 정확히 배치
        var ray = Camera.main.ScreenPointToRay(
            new Vector3(Screen.width * .5f, Screen.height * .5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, 500f))
            return hit.point;
        // 폴백: 카메라 전방 10m XZ 평면
        var pt = Camera.main.transform.position + Camera.main.transform.forward * 10f;
        pt.y = 0f;
        return pt;
    }

    // 배치된 에셋 렌더러 최적화: 그림자 + 재질 색상 보존
    void OptimizeRenderers(GameObject obj)
    {
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");

        foreach (var r in obj.GetComponentsInChildren<Renderer>())
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            r.receiveShadows    = true;

            if (urpLit == null) continue;

            // sharedMaterials 로 깨진 셰이더 여부만 확인 (인스턴스 미생성)
            bool needFix = false;
            foreach (var sm in r.sharedMaterials)
            {
                if (sm == null) continue;
                var sn = sm.shader?.name ?? "";
                if (sn == "Hidden/InternalErrorShader" || sn == "")
                { needFix = true; break; }
            }
            if (!needFix) continue;

            // r.materials → 인스턴스 생성 (원본 프리팹 재질 보호)
            foreach (var m in r.materials)
            {
                if (m == null) continue;
                var sn = m.shader?.name ?? "";
                if (sn != "Hidden/InternalErrorShader" && sn != "") continue;
                Color   col = m.HasProperty("_Color")   ? m.GetColor("_Color")     : Color.white;
                Texture tx  = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
                m.shader = urpLit;
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
                if (m.HasProperty("_Color"))     m.SetColor("_Color",     col);
                if (tx != null && m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tx);
                if (tx != null && m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tx);
            }
        }
    }

    void EnsureCollider(GameObject obj)
    {
        FixNegativeBoxColliders(obj);
        if (obj.GetComponentInChildren<Collider>() != null) return;
        var col = obj.AddComponent<BoxCollider>();
        var rs  = obj.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return;
        var b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        col.center = obj.transform.InverseTransformPoint(b.center);
        var raw = obj.transform.InverseTransformVector(b.size);
        col.size = new Vector3(Mathf.Abs(raw.x), Mathf.Abs(raw.y), Mathf.Abs(raw.z));
    }

    void FixNegativeBoxColliders(GameObject root)
    {
        foreach (var bc in root.GetComponentsInChildren<BoxCollider>())
        {
            var ls = bc.transform.lossyScale;
            if (ls.x >= 0f && ls.y >= 0f && ls.z >= 0f) continue;
            var mf = bc.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                var mc = bc.gameObject.AddComponent<MeshCollider>();
                mc.convex    = true;
                mc.sharedMesh = mf.sharedMesh;
            }
            Destroy(bc);
        }
    }

    void AttachSelectable(GameObject root)
    {
        if (!root.GetComponent<SelectableAsset>())
            root.AddComponent<SelectableAsset>().Init(this, root);
        foreach (var col in root.GetComponentsInChildren<Collider>())
            if (!col.gameObject.GetComponent<SelectableAsset>())
                col.gameObject.AddComponent<SelectableAsset>().Init(this, root);
    }

    // ── 선택 ─────────────────────────────────────────────
    public GameObject GetSelectedObject() => selectedObject;

    public void SelectObject(GameObject obj)
    {
        // 이전 선택 하이라이트 제거
        if (selectedObject != null)
        {
            var prev = selectedObject.GetComponent<HanokSelectionHighlight>();
            if (prev != null) prev.Hide();
        }

        selectedObject = obj;

        // 새 선택 하이라이트 적용
        if (obj != null)
        {
            var hl = obj.GetComponent<HanokSelectionHighlight>();
            if (hl == null) hl = obj.AddComponent<HanokSelectionHighlight>();
            hl.Show();
        }

        // 바닥 아래로 박힌 오브젝트 자동 보정 (b.min.y < 0 이면 올려서 바닥에 정렬)
        if (obj != null)
        {
            var rends = obj.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                var b = rends[0].bounds;
                foreach (var r in rends) b.Encapsulate(r.bounds);
                if (b.min.y < -0.02f)
                    obj.transform.position += Vector3.up * (-b.min.y);
            }
        }

        RefreshInfoPanel();
        if (obj != null) ForceSyncTransform();
        SyncGizmo();

        // 선택 시 카메라 피벗을 오브젝트 방향으로 부드럽게 이동
        if (obj != null)
            Camera.main?.GetComponent<HanokCameraController>()
                        ?.ShiftPivotToward(obj.transform.position);
    }

    void RefreshInfoPanel()
    {
        if (infoNameText == null) return;
        bool has = selectedObject != null;
        infoNameText.text  = has ? selectedObject.name : "부재를 선택하세요";
        infoNameText.color = has ? TEXT_H : TEXT_HINT;
    }

    // ── Transform 동기화 ──────────────────────────────────
    void SyncTransformInputs()
    {
        if (selectedObject == null || posX == null) return;
        var t = selectedObject.transform;
        if (!posX.isFocused)  posX.SetTextWithoutNotify(t.position.x.ToString("F2"));
        if (!posY.isFocused)  posY.SetTextWithoutNotify(t.position.y.ToString("F2"));
        if (!posZ.isFocused)  posZ.SetTextWithoutNotify(t.position.z.ToString("F2"));
        if (!rotX.isFocused)  rotX.SetTextWithoutNotify(t.eulerAngles.x.ToString("F1"));
        if (!rotY.isFocused)  rotY.SetTextWithoutNotify(t.eulerAngles.y.ToString("F1"));
        if (!rotZ.isFocused)  rotZ.SetTextWithoutNotify(t.eulerAngles.z.ToString("F1"));
        if (!scaleF.isFocused) scaleF.SetTextWithoutNotify(t.localScale.x.ToString("F2"));
    }

    void ForceSyncTransform()
    {
        if (selectedObject == null || posX == null) return;
        var t = selectedObject.transform;
        posX.SetTextWithoutNotify(t.position.x.ToString("F2"));
        posY.SetTextWithoutNotify(t.position.y.ToString("F2"));
        posZ.SetTextWithoutNotify(t.position.z.ToString("F2"));
        rotX.SetTextWithoutNotify(t.eulerAngles.x.ToString("F1"));
        rotY.SetTextWithoutNotify(t.eulerAngles.y.ToString("F1"));
        rotZ.SetTextWithoutNotify(t.eulerAngles.z.ToString("F1"));
        scaleF.SetTextWithoutNotify(t.localScale.x.ToString("F2"));
    }

    // ── Transform 적용 ───────────────────────────────────
    public void ApplyPos()
    {
        if (!selectedObject) return;
        selectedObject.transform.position =
            new Vector3(Pf(posX.text), Pf(posY.text), Pf(posZ.text));
    }

    public void ApplyRot()
    {
        if (!selectedObject) return;
        selectedObject.transform.eulerAngles =
            new Vector3(Pf(rotX.text), Pf(rotY.text), Pf(rotZ.text));
    }

    public void ApplyScale()
    {
        if (!selectedObject) return;
        float s = Mathf.Max(0.001f, Pf(scaleF.text));
        selectedObject.transform.localScale = Vector3.one * s;
    }

    public void QuickRot(float deg)
    {
        if (selectedObject) selectedObject.transform.Rotate(0, deg, 0, Space.World);
    }

    public void ResetRot()
    {
        if (selectedObject) selectedObject.transform.eulerAngles = Vector3.zero;
    }

    public void SetScale(float s)
    {
        if (!selectedObject) return;
        selectedObject.transform.localScale = Vector3.one * s;
        scaleF?.SetTextWithoutNotify(s.ToString("F2"));
    }

    public void Duplicate()
    {
        if (!selectedObject) return;
        var c = Instantiate(selectedObject);
        c.name = selectedObject.name + "_복사";
        c.transform.position += Vector3.right * 2f;
        AttachSelectable(c);
        SelectObject(c);
    }

    public void DeleteSelected()
    {
        if (!selectedObject) return;
        Destroy(selectedObject);
        selectedObject = null;
        RefreshInfoPanel();
        SyncGizmo();
    }

    public void ClearSelection()
    {
        if (selectedObject != null)
        {
            var hl = selectedObject.GetComponent<HanokSelectionHighlight>();
            if (hl != null) hl.Hide();
        }
        selectedObject = null;
        RefreshInfoPanel();
        SyncGizmo();
    }

    // ── 뷰포트 클릭 / 드래그 ─────────────────────────────
    void HandleViewportClick()
    {
        var mouse = Mouse.current;
        if (mouse == null || Camera.main == null) return;

        bool overUI = UnityEngine.EventSystems.EventSystem.current != null &&
                      UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        Vector2 mp = mouse.position.ReadValue();

        // ═══════════════════════════════════════════════════
        // SELECT 모드: 클릭=선택, 클릭+드래그=서피스 스냅 이동
        // ═══════════════════════════════════════════════════
        if (currentTool == EditTool.Select)
        {
            if (mouse.leftButton.wasPressedThisFrame && !overUI)
            {
                _isDragging  = false;
                _pendingDrag = false;
                var ray = Camera.main.ScreenPointToRay((Vector3)mp);
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
                {
                    var sa = hit.collider.GetComponent<SelectableAsset>();
                    if (sa != null)
                    {
                        SelectObject(sa.Root);
                        _pendingDrag    = true;
                        _dragStartMouse = mp;
                        _dragPlane      = MakeDragPlane(sa.Root.transform.position);
                        if (_dragPlane.Raycast(ray, out float e))
                            _dragOffset = sa.Root.transform.position - ray.GetPoint(e);
                        else _dragOffset = Vector3.zero;
                    }
                    else ClearSelection();
                }
                else ClearSelection();
            }

            // 드래그 시작 판정 — 시작 직전 위치를 undo 스택에 기록
            if (_pendingDrag && !_isDragging && mouse.leftButton.isPressed &&
                Vector2.Distance(mp, _dragStartMouse) > 5f)
            {
                if (selectedObject != null) PushUndoMove(selectedObject);
                _isDragging  = true;
                _pendingDrag = false;
            }

            // 드래그 적용: 다른 오브젝트 위면 서피스 스냅, 폴백은 Y=0 바닥 평면
            if (_isDragging && selectedObject != null && mouse.leftButton.isPressed)
            {
                var ray = Camera.main.ScreenPointToRay((Vector3)mp);
                if (TryRaycastSurface(ray, selectedObject, out Vector3 surfPt))
                    SnapToSurface(selectedObject, surfPt);
                else
                {
                    // 바닥(Y=0) XZ 평면과 교차: 항상 오브젝트를 바닥에 붙임
                    var ground = new Plane(Vector3.up, Vector3.zero);
                    if (ground.Raycast(ray, out float gd))
                        SnapToSurface(selectedObject, ray.GetPoint(gd));
                }
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            { _isDragging = false; _pendingDrag = false; }
            return;
        }

        // ═══════════════════════════════════════════════════
        // SCALE 모드: 스케일 기즈모 조작 또는 오브젝트 클릭→선택
        // ═══════════════════════════════════════════════════
        if (currentTool == EditTool.Scale)
        {
            if (_scaleGizmo != null &&
                (_scaleGizmo.IsConsuming || _scaleGizmo.WouldCapture(mp)))
                return;
            if (mouse.leftButton.wasPressedThisFrame && !overUI)
            {
                var ray = Camera.main.ScreenPointToRay((Vector3)mp);
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
                {
                    var sa = hit.collider.GetComponent<SelectableAsset>();
                    if (sa != null) SelectObject(sa.Root);
                    else ClearSelection();
                }
                else ClearSelection();
            }
            return;
        }

        // ═══════════════════════════════════════════════════
        // ROTATE 모드: 기즈모 드래그 또는 오브젝트 클릭→선택
        // ═══════════════════════════════════════════════════
        if (currentTool == EditTool.Rotate)
        {
            if (_rotGizmo != null && _rotGizmo.IsConsuming) return;
            if (mouse.leftButton.wasPressedThisFrame && !overUI)
            {
                var ray = Camera.main.ScreenPointToRay((Vector3)mp);
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
                {
                    var sa = hit.collider.GetComponent<SelectableAsset>();
                    if (sa != null) SelectObject(sa.Root);
                }
            }
            return;
        }

        // ═══════════════════════════════════════════════════
        // DELETE 모드: 좌클릭 즉시 삭제
        // ═══════════════════════════════════════════════════
        if (currentTool == EditTool.Delete)
        {
            if (!mouse.leftButton.wasPressedThisFrame || overUI) return;
            var ray = Camera.main.ScreenPointToRay((Vector3)mp);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                var sa = hit.collider.GetComponent<SelectableAsset>();
                if (sa != null) { SelectObject(sa.Root); DeleteSelected(); }
            }
        }
    }

    // ── 서피스 스냅 헬퍼 ──────────────────────────────────
    bool TryRaycastSurface(Ray ray, GameObject exclude, out Vector3 point)
    {
        var hits = Physics.RaycastAll(ray, 1000f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            if (h.collider.transform == exclude.transform) continue;
            if (h.collider.transform.IsChildOf(exclude.transform)) continue;
            point = h.point;
            return true;
        }
        point = Vector3.zero;
        return false;
    }

    void SnapToSurface(GameObject obj, Vector3 point)
    {
        var rends = obj.GetComponentsInChildren<Renderer>();
        float bottomOffset = 0f;
        if (rends.Length > 0)
        {
            var b = rends[0].bounds;
            foreach (var r in rends) b.Encapsulate(r.bounds);
            bottomOffset = obj.transform.position.y - b.min.y;
        }
        obj.transform.position = new Vector3(point.x, point.y + bottomOffset, point.z);
    }

    // ── 실행 취소 ─────────────────────────────────────────
    void PushUndoMove(GameObject obj)
    {
        if (_undoStack.Count >= 20) _undoStack.RemoveAt(0);
        _undoStack.Add(new UndoEntry
        {
            op = UndoEntry.Op.Move, obj = obj,
            prevPos = obj.transform.position,
            prevRot = obj.transform.rotation
        });
    }

    void PushUndoSpawn(GameObject obj)
    {
        if (_undoStack.Count >= 20) _undoStack.RemoveAt(0);
        _undoStack.Add(new UndoEntry { op = UndoEntry.Op.Spawn, obj = obj });
    }

    void DoUndo()
    {
        if (_undoStack.Count == 0) return;
        var entry = _undoStack[_undoStack.Count - 1];
        _undoStack.RemoveAt(_undoStack.Count - 1);
        if (entry.obj == null) return; // 이미 삭제됨
        switch (entry.op)
        {
            case UndoEntry.Op.Move:
                entry.obj.transform.position = entry.prevPos;
                entry.obj.transform.rotation = entry.prevRot;
                SelectObject(entry.obj);
                break;
            case UndoEntry.Op.Spawn:
                if (selectedObject == entry.obj) ClearSelection();
                Destroy(entry.obj);
                break;
        }
    }

    // 현재 카메라 뷰에 맞는 드래그 평면을 반환
    // Top/3D → XZ 수평면 | Front/Back → XY 수직면 | Right/Left → ZY 수직면
    Plane MakeDragPlane(Vector3 objPos)
    {
        var cam = Camera.main?.GetComponent<HanokCameraController>();
        if (cam == null) return new Plane(Vector3.up, objPos);

        switch (cam.CurrentPreset)
        {
            case HanokCameraController.ViewPreset.Front:
            case HanokCameraController.ViewPreset.Back:
                return new Plane(Vector3.forward, objPos); // XY 평면

            case HanokCameraController.ViewPreset.Right:
            case HanokCameraController.ViewPreset.Left:
                return new Plane(Vector3.right, objPos);   // ZY 평면

            default: // Perspective, Top
                return new Plane(Vector3.up, objPos);      // XZ 평면
        }
    }

    // ── 키보드 단축키 ─────────────────────────────────────
    void HandleKeyboardShortcuts()
    {
        var kb = Keyboard.current;
        if (kb == null || AnyInputFocused()) return;

        if (kb.digit1Key.wasPressedThisFrame) SetTool(EditTool.Select);
        if (kb.digit2Key.wasPressedThisFrame) SetTool(EditTool.Rotate);
        if (kb.digit3Key.wasPressedThisFrame) SetTool(EditTool.Scale);
        if (kb.digit4Key.wasPressedThisFrame) SetTool(EditTool.Delete);
        if (kb.deleteKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame)
            DeleteSelected();
        if (kb.escapeKey.wasPressedThisFrame)
        { ClearSelection(); SetTool(EditTool.Select); }
        if (kb.homeKey.wasPressedThisFrame)
            Camera.main?.GetComponent<HanokCameraController>()?.ResetView();
        if (kb.ctrlKey.isPressed && kb.zKey.wasPressedThisFrame) DoUndo();
        if (kb.ctrlKey.isPressed && kb.dKey.wasPressedThisFrame) Duplicate();
        if (kb.pKey.wasPressedThisFrame) TriggerCapture();
    }

    bool AnyInputFocused()
    {
        var sel = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
        return sel != null && sel.GetComponent<TMPro.TMP_InputField>() != null;
    }

    // ── 유틸 ─────────────────────────────────────────────
    static float Pf(string s) =>
        float.TryParse(s,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out float v) ? v : 0f;

    void KorFont(TMP_Text t)
    {
        if (!IsUsableKoreanFont(koreanFont))
            InitKoreanFont();

        if (koreanFont) t.font = koreanFont;
    }

    TMP_FontAsset _lat;
    TMP_FontAsset LatinFont =>
        _lat ??= Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF")
              ?? TMP_Settings.defaultFontAsset;
    void LatFont(TMP_Text t)  { var f = LatinFont; if (f) t.font = f; }

    static bool HasKorean(string s)
    {
        foreach (char c in s)
            if (c >= '가' && c <= '힣') return true;
        return false;
    }


    // ── 뷰포트 캡처 ───────────────────────────────────────────
    public void TriggerCapture() { if (!_capturing) StartCoroutine(CaptureViewport()); }

    System.Collections.IEnumerator CaptureViewport()
    {
        _capturing = true;

        // 플래시 즉시 표시
        if (_captureFlash != null)
        {
            _captureFlash.SetActive(true);
            _captureFlash.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.80f);
        }
        yield return new WaitForEndOfFrame();

        // 전체 화면 픽셀 읽기
        var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        tex.Apply();

        // 바탕화면에 PNG 저장
        string folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        string fname  = "Hanok_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        string path   = System.IO.Path.Combine(folder, fname);
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        Destroy(tex);

        // 플래시 페이드 아웃
        if (_captureFlash != null)
        {
            var fi = _captureFlash.GetComponent<Image>();
            for (float t = 0f; t < 0.40f; t += Time.unscaledDeltaTime)
            {
                fi.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.80f, 0f, t / 0.40f));
                yield return null;
            }
            _captureFlash.SetActive(false);
        }

        ShowToast("저장됨  " + fname);
        _capturing = false;
    }

    // ── 토스트 알림 ───────────────────────────────────────────
    public void ShowToast(string msg)
    {
        if (_toastGO == null) return;
        if (_toastText != null) _toastText.text = msg;
        if (_toastRoutine != null) StopCoroutine(_toastRoutine);
        _toastRoutine = StartCoroutine(ToastAnim());
    }

    System.Collections.IEnumerator ToastAnim()
    {
        _toastGO.SetActive(true);
        var bg  = _toastGO.GetComponent<Image>();
        var txt = _toastText;
        static Color BgCol(float a)  => new Color(0.08f, 0.08f, 0.12f, a * 0.90f);

        for (float t = 0f; t < 0.20f; t += Time.unscaledDeltaTime)
        {
            float a = t / 0.20f;
            if (bg)  bg.color  = BgCol(a);
            if (txt) txt.color = new Color(1f, 1f, 1f, a);
            yield return null;
        }
        if (bg)  bg.color  = BgCol(1f);
        if (txt) txt.color = Color.white;

        yield return new WaitForSeconds(2.5f);

        for (float t = 0f; t < 0.30f; t += Time.unscaledDeltaTime)
        {
            float a = 1f - t / 0.30f;
            if (bg)  bg.color  = BgCol(a);
            if (txt) txt.color = new Color(1f, 1f, 1f, a);
            yield return null;
        }
        _toastGO.SetActive(false);
    }

    // ── 뷰 배지 갱신 (매 프레임) ─────────────────────────────
    void UpdateViewBadge()
    {
        if (_viewBadgeText == null) return;
        var cam = Camera.main?.GetComponent<HanokCameraController>();
        if (cam == null) return;
        _viewBadgeText.text = cam.CurrentPreset switch
        {
            HanokCameraController.ViewPreset.Top         => "위",
            HanokCameraController.ViewPreset.Front       => "정면",
            HanokCameraController.ViewPreset.Back        => "후면",
            HanokCameraController.ViewPreset.Right       => "우측",
            HanokCameraController.ViewPreset.Left        => "좌측",
            _                                            => "3D",
        };
    }
}
