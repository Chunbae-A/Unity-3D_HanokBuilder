using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class HanokUIManager : MonoBehaviour
{
    [Header("한글 폰트")]
    public TMP_FontAsset koreanFont;

    private GameObject     selectedObject;
    private TMP_Text       selectedNameText;
    private TMP_InputField posX, posY, posZ;
    private TMP_InputField rotX, rotY, rotZ;
    private TMP_InputField scaleF;
    private Transform      assetContent;
    private bool           uiBuilt = false;

    const string ASSET_PATH = "HanokAssets";

    static Color H(string h){ ColorUtility.TryParseHtmlString(h,out Color c); return c; }
    static readonly Color BG_PANEL  = H("#0E1210");
    static readonly Color BG_CARD   = H("#161C15");
    static readonly Color BG_INPUT  = H("#0A0D09");
    static readonly Color GOLD      = H("#C8A040");
    static readonly Color TEAL      = H("#1A7065");
    static readonly Color RED_BTN   = H("#6A1A1A");
    static readonly Color BLUE_BTN  = H("#1A3860");
    static readonly Color DARK_BTN  = H("#1A201A");
    static readonly Color TEXT_H    = H("#EDE0C4");
    static readonly Color TEXT_MAIN = H("#AEA47E");
    static readonly Color TEXT_SUB  = H("#484E46");
    static readonly Color COL_X     = H("#A03030");
    static readonly Color COL_Y     = H("#30A030");
    static readonly Color COL_Z     = H("#3050A0");

        void Start()
        {
            BuildUI();
            uiBuilt = true;
            // 레이아웃 강제 갱신 (ContentSizeFitter 버그 우회)
            StartCoroutine(ForceRebuildLayout());
        }

        System.Collections.IEnumerator ForceRebuildLayout()
        {
            // 2프레임 대기 후 모든 레이아웃 강제 재계산
            yield return null;
            yield return null;

            foreach (var fitter in FindObjectsByType<ContentSizeFitter>(FindObjectsSortMode.None))
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(
                    fitter.GetComponent<RectTransform>());
            }

            foreach (var group in FindObjectsByType<VerticalLayoutGroup>(FindObjectsSortMode.None))
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(
                    group.GetComponent<RectTransform>());
            }
}

    void Update()
    {
        if (!uiBuilt) return;
        SyncInputs();
        HandleClick();
    }

    // ══════════════════════════════════════════════════
    void BuildUI()
    {
        EnsureEventSystem();

        var cv = new GameObject("HanokCanvas").AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 10;
        var cvs = cv.gameObject.AddComponent<CanvasScaler>();
        cvs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cvs.referenceResolution = new Vector2(1280, 720);
        cvs.matchWidthOrHeight = 0.5f;
        cv.gameObject.AddComponent<GraphicRaycaster>();

        Transform root = cv.transform;

        // ── 왼쪽 패널 ─────────────────────────────────
        var left = Panel(root, "Left",
            new Vector2(0,0), new Vector2(0,1),
            new Vector2(0,0.5f), new Vector2(120,0), new Vector2(240,0));

        PanelTitle(left, "한옥 에셋");

        var leftScroll = MakeScroll(left, 48);
        assetContent = leftScroll.transform.Find("Viewport/Content");

        // ── 오른쪽 패널 ───────────────────────────────
        var right = Panel(root, "Right",
            new Vector2(1,0), new Vector2(1,1),
            new Vector2(1,0.5f), new Vector2(-120,0), new Vector2(240,0));

        PanelTitle(right, "공간 편집");

        var rightScroll = MakeScroll(right, 48);
        var rc = rightScroll.transform.Find("Viewport/Content");

        // 오른쪽 패널 내용 채우기
        FillEditPanel(rc);

        // ── 에셋 버튼 생성 ─────────────────────────────
        LoadAssets();
    }

    // ══════════════════════════════════════════════════
    //  편집 패널
    // ══════════════════════════════════════════════════
    void FillEditPanel(Transform rc)
    {
        // 선택된 이름
        Sp(rc,6);
        var nameCard = Rect(rc, "NameCard", 40);
        nameCard.GetComponent<Image>().color = BG_CARD;
        TopLine(nameCard, GOLD, 2);

        var nt = new GameObject("T"); nt.transform.SetParent(nameCard, false);
        var ntRT = nt.AddComponent<RectTransform>();
        ntRT.anchorMin=Vector2.zero; ntRT.anchorMax=Vector2.one;
        ntRT.offsetMin=new Vector2(12,0); ntRT.offsetMax=Vector2.zero;
        selectedNameText = nt.AddComponent<TextMeshProUGUI>();
        selectedNameText.text = "선택된 부재 없음";
        selectedNameText.fontSize = 11;
        selectedNameText.color = TEXT_SUB;
        selectedNameText.alignment = TextAlignmentOptions.Left;
        if (koreanFont) selectedNameText.font = koreanFont;

        Sp(rc,6);
        Badge(rc,"위  치",TEAL); Sp(rc,3);
        posX = InputRow(rc,"X",COL_X);
        posY = InputRow(rc,"Y",COL_Y);
        posZ = InputRow(rc,"Z",COL_Z);
        posX.onEndEdit.AddListener(_=>ApplyPos());
        posY.onEndEdit.AddListener(_=>ApplyPos());
        posZ.onEndEdit.AddListener(_=>ApplyPos());

        Sp(rc,6); Hr(rc); Sp(rc,6);
        Badge(rc,"회  전",TEAL); Sp(rc,3);
        rotX = InputRow(rc,"X",COL_X);
        rotY = InputRow(rc,"Y",COL_Y);
        rotZ = InputRow(rc,"Z",COL_Z);
        rotX.onEndEdit.AddListener(_=>ApplyRot());
        rotY.onEndEdit.AddListener(_=>ApplyRot());
        rotZ.onEndEdit.AddListener(_=>ApplyRot());
        Sp(rc,3);
        BtnRow(rc,26,
            ("-90°",(System.Action)(()=>QRot(-90)),DARK_BTN),
            ("+90°",(System.Action)(()=>QRot( 90)),DARK_BTN),
            ("초기화",(System.Action)ResetRot,      DARK_BTN));

        Sp(rc,6); Hr(rc); Sp(rc,6);
        Badge(rc,"크  기",TEAL); Sp(rc,3);
        scaleF = InputRow(rc,"S",GOLD);
        scaleF.onEndEdit.AddListener(_=>ApplyScale());
        Sp(rc,3);
        BtnRow(rc,26,
            ("0.5×",(System.Action)(()=>SetScale(.5f)),DARK_BTN),
            (" 1× ",(System.Action)(()=>SetScale(1f)), H("#1A3A1A")),
            (" 2× ",(System.Action)(()=>SetScale(2f)), DARK_BTN));

        Sp(rc,8); Hr(rc); Sp(rc,8);
        BtnRow(rc,34,
            ("복  제",(System.Action)Dup,    BLUE_BTN),
            ("삭  제",(System.Action)DelSel, RED_BTN));
        Sp(rc,3);
        BtnRow(rc,26,("선택 해제",(System.Action)ClearSel,DARK_BTN));
        Sp(rc,20);
    }

    // ══════════════════════════════════════════════════
    //  에셋 로드
    // ══════════════════════════════════════════════════
    void LoadAssets()
    {
        if (assetContent == null)
        {
            Debug.LogError("[HanokBuilder] assetContent null!");
            return;
        }

        var all = Resources.LoadAll(ASSET_PATH);
        var list = new List<GameObject>();
        foreach (var o in all)
            if (o is GameObject g) list.Add(g);

        Debug.Log($"[HanokBuilder] 에셋 {list.Count}개 로드");

        if (list.Count == 0)
        {
            var empty = Rect(assetContent, "Empty", 80);
            Destroy(empty.GetComponent<Image>());
            var t = empty.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = "Resources/HanokAssets\n폴더에 Prefab을 넣으세요";
            t.fontSize=10; t.color=TEXT_SUB;
            t.alignment=TextAlignmentOptions.Center;
            if (koreanFont) t.font=koreanFont;
            return;
        }

        list.Sort((a,b)=>string.Compare(a.name,b.name));
        foreach (var p in list)
        {
            var cap = p;
            AssetBtn(assetContent, cap.name, ()=>Spawn(cap));
        }
    }

    // ══════════════════════════════════════════════════
    //  기능
    // ══════════════════════════════════════════════════
    void Spawn(GameObject prefab)
    {
        Vector3 pos = Vector3.zero;
        if (Camera.main != null)
        {
            var ray = Camera.main.ScreenPointToRay(
                new Vector3(Screen.width/2f, Screen.height/2f));
            if (Physics.Raycast(ray, out RaycastHit hit, 500f))
                pos = hit.point;
            else
            {
                pos = Camera.main.transform.position
                    + Camera.main.transform.forward * 10f;
                pos.y = 0;
            }
        }

        var obj = Instantiate(prefab, pos, Quaternion.identity);
        obj.name = prefab.name;

        // 콜라이더 자동 추가 (없으면)
        if (obj.GetComponentInChildren<Collider>() == null)
        {
            var col = obj.AddComponent<BoxCollider>();
            var rends = obj.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                var b = rends[0].bounds;
                foreach (var r in rends) b.Encapsulate(r.bounds);
                col.center = obj.transform.InverseTransformPoint(b.center);
                col.size   = obj.transform.InverseTransformVector(b.size);
            }
        }

        // SelectableAsset을 루트 및 모든 자식 Collider에 붙이기
        AttachSelectable(obj);
        SelectObject(obj);
        Debug.Log($"[HanokBuilder] 배치: {obj.name} @ {pos}");
    }

    void AttachSelectable(GameObject root)
    {
        // 루트에 붙이기
        if (!root.GetComponent<SelectableAsset>())
            root.AddComponent<SelectableAsset>().Init(this, root);

        // 모든 자식 Collider에도 붙이기 (FBX 구조 대응)
        foreach (var col in root.GetComponentsInChildren<Collider>())
        {
            if (!col.gameObject.GetComponent<SelectableAsset>())
                col.gameObject.AddComponent<SelectableAsset>().Init(this, root);
        }
    }

    public void SelectObject(GameObject obj)
    {
        selectedObject = obj;
        if (selectedNameText != null)
        {
            selectedNameText.text  = obj != null ? $"▶  {obj.name}" : "선택된 부재 없음";
            selectedNameText.color = obj != null ? GOLD : TEXT_SUB;
        }
        SyncInputs(true);
    }

    void SyncInputs(bool force=false)
    {
        if (!selectedObject) return;
        if (posX==null) return;
        var t = selectedObject.transform;
        if (force||!posX.isFocused) posX.text=t.position.x.ToString("F2");
        if (force||!posY.isFocused) posY.text=t.position.y.ToString("F2");
        if (force||!posZ.isFocused) posZ.text=t.position.z.ToString("F2");
        if (force||!rotX.isFocused) rotX.text=t.eulerAngles.x.ToString("F1");
        if (force||!rotY.isFocused) rotY.text=t.eulerAngles.y.ToString("F1");
        if (force||!rotZ.isFocused) rotZ.text=t.eulerAngles.z.ToString("F1");
        if (force||!scaleF.isFocused) scaleF.text=t.localScale.x.ToString("F2");
    }

    void ApplyPos()
    {
        if (!selectedObject) return;
        selectedObject.transform.position =
            new Vector3(F(posX.text),F(posY.text),F(posZ.text));
    }
    void ApplyRot()
    {
        if (!selectedObject) return;
        selectedObject.transform.eulerAngles =
            new Vector3(F(rotX.text),F(rotY.text),F(rotZ.text));
    }
    void ApplyScale()
    {
        if (!selectedObject) return;
        float s=Mathf.Max(0.01f,F(scaleF.text));
        selectedObject.transform.localScale=Vector3.one*s;
    }
    void QRot(float d)    { if(selectedObject) selectedObject.transform.Rotate(0,d,0); }
    void ResetRot()       { if(selectedObject) selectedObject.transform.eulerAngles=Vector3.zero; }
    void SetScale(float s){ if(!selectedObject)return; selectedObject.transform.localScale=Vector3.one*s; if(scaleF)scaleF.text=s.ToString("F2"); }
    public void DelSel()
    {
        if(!selectedObject)return;
        Destroy(selectedObject); selectedObject=null;
        if(selectedNameText){selectedNameText.text="선택된 부재 없음";selectedNameText.color=TEXT_SUB;}
    }
    public void Dup()
    {
        if(!selectedObject)return;
        var c=Instantiate(selectedObject);
        c.name=selectedObject.name+"_복사";
        c.transform.position+=Vector3.right*2f;
        AttachSelectable(c);
        SelectObject(c);
    }
    public void ClearSel()
    {
        selectedObject=null;
        if(selectedNameText){selectedNameText.text="선택된 부재 없음";selectedNameText.color=TEXT_SUB;}
    }
    float F(string s)=>float.TryParse(s,out float v)?v:0f;

    // ── 클릭 감지 ─────────────────────────────────────
    void HandleClick()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        var ray = Camera.main.ScreenPointToRay(
            (Vector3)mouse.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            var sa = hit.collider.GetComponent<SelectableAsset>();
            if (sa != null) SelectObject(sa.Root);
        }
    }

    // ══════════════════════════════════════════════════
    //  UI 빌더 헬퍼
    // ══════════════════════════════════════════════════
    GameObject Panel(Transform parent, string name,
        Vector2 aMin, Vector2 aMax, Vector2 pivot,
        Vector2 ancPos, Vector2 size)
    {
        var go = new GameObject(name); go.transform.SetParent(parent,false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin=aMin; rt.anchorMax=aMax; rt.pivot=pivot;
        rt.anchoredPosition=ancPos; rt.sizeDelta=size;
        go.AddComponent<Image>().color=BG_PANEL;
        return go;
    }

    void PanelTitle(GameObject panel, string title)
    {
        var hdr=new GameObject("Hdr"); hdr.transform.SetParent(panel.transform,false);
        var rt=hdr.AddComponent<RectTransform>();
        rt.anchorMin=new Vector2(0,1); rt.anchorMax=new Vector2(1,1);
        rt.pivot=new Vector2(.5f,1);
        rt.offsetMin=new Vector2(0,-48); rt.offsetMax=Vector2.zero;
        hdr.AddComponent<Image>().color=BG_CARD;

        var line=new GameObject("L"); line.transform.SetParent(hdr.transform,false);
        var lRT=line.AddComponent<RectTransform>();
        lRT.anchorMin=new Vector2(0,0); lRT.anchorMax=new Vector2(1,0);
        lRT.pivot=new Vector2(.5f,0);
        lRT.offsetMin=new Vector2(8,-2); lRT.offsetMax=new Vector2(-8,0);
        line.AddComponent<Image>().color=GOLD;

        var tgo=new GameObject("T"); tgo.transform.SetParent(hdr.transform,false);
        var tRT=tgo.AddComponent<RectTransform>();
        tRT.anchorMin=Vector2.zero; tRT.anchorMax=Vector2.one;
        tRT.offsetMin=new Vector2(12,2); tRT.offsetMax=new Vector2(-8,-2);
        var t=tgo.AddComponent<TextMeshProUGUI>();
        t.text=title; t.fontSize=13; t.fontStyle=FontStyles.Bold;
        t.color=TEXT_H; t.alignment=TextAlignmentOptions.Left;
        if(koreanFont)t.font=koreanFont;
    }

    GameObject MakeScroll(GameObject parent, float topOff)
    {
        var go=new GameObject("Scroll"); go.transform.SetParent(parent.transform,false);
        var rt=go.AddComponent<RectTransform>();
        rt.anchorMin=Vector2.zero; rt.anchorMax=Vector2.one;
        rt.offsetMin=Vector2.zero; rt.offsetMax=new Vector2(0,-topOff);
        go.AddComponent<Image>().color=Color.clear;
        var sr=go.AddComponent<ScrollRect>(); sr.horizontal=false;

        var vp=new GameObject("Viewport"); vp.transform.SetParent(go.transform,false);
        var vpRT=vp.AddComponent<RectTransform>();
        vpRT.anchorMin=Vector2.zero; vpRT.anchorMax=Vector2.one;
        vpRT.offsetMin=vpRT.offsetMax=Vector2.zero;
        vp.AddComponent<Image>().color=Color.clear;
        vp.AddComponent<Mask>().showMaskGraphic=false;

        var ct=new GameObject("Content"); ct.transform.SetParent(vp.transform,false);
        var cRT=ct.AddComponent<RectTransform>();
        cRT.anchorMin=new Vector2(0,1); cRT.anchorMax=new Vector2(1,1);
        cRT.pivot=new Vector2(.5f,1); cRT.offsetMin=cRT.offsetMax=Vector2.zero;
        var vlg=ct.AddComponent<VerticalLayoutGroup>();
        vlg.spacing=2; vlg.padding=new RectOffset(6,6,6,6);
        vlg.childForceExpandWidth=true; vlg.childForceExpandHeight=false;
        ct.AddComponent<ContentSizeFitter>().verticalFit=
            ContentSizeFitter.FitMode.PreferredSize;
        sr.viewport=vpRT; sr.content=cRT;
        return go;
    }

    void AssetBtn(Transform p, string label, System.Action onClick)
    {
        var go=new GameObject("AB"); go.transform.SetParent(p,false);
        var le=go.AddComponent<LayoutElement>();
        le.preferredHeight=44; le.flexibleWidth=1;
        go.AddComponent<Image>().color=BG_CARD;
        var btn=go.AddComponent<Button>();
        var cs=btn.colors;
        cs.normalColor=BG_CARD; cs.highlightedColor=H("#1E2C1E");
        cs.pressedColor=H("#0A100A"); btn.colors=cs;
        btn.onClick.AddListener(()=>onClick?.Invoke());

        // 왼쪽 금선
        var bar=new GameObject("Bar"); bar.transform.SetParent(go.transform,false);
        var bRT=bar.AddComponent<RectTransform>();
        bRT.anchorMin=new Vector2(0,.1f); bRT.anchorMax=new Vector2(0,.9f);
        bRT.offsetMin=Vector2.zero; bRT.offsetMax=new Vector2(3,0);
        bar.AddComponent<Image>().color=GOLD;

        // 아이콘 박스 (컬러 박스로 대체)
        var ico=new GameObject("Ico"); ico.transform.SetParent(go.transform,false);
        var iRT=ico.AddComponent<RectTransform>();
        iRT.anchorMin=new Vector2(0,.1f); iRT.anchorMax=new Vector2(0,.9f);
        iRT.offsetMin=new Vector2(6,0); iRT.offsetMax=new Vector2(42,0);
        ico.AddComponent<Image>().color=H("#1A2A1A");
        // 아이콘 텍스트
        var iT=new GameObject("IT"); iT.transform.SetParent(ico.transform,false);
        var iTRT=iT.AddComponent<RectTransform>();
        iTRT.anchorMin=Vector2.zero; iTRT.anchorMax=Vector2.one;
        iTRT.offsetMin=iTRT.offsetMax=Vector2.zero;
        var it=iT.AddComponent<TextMeshProUGUI>();
        it.text="🏯"; it.fontSize=18;
        it.alignment=TextAlignmentOptions.Center;
        if(koreanFont)it.font=koreanFont;

        // 이름
        var tgo=new GameObject("T"); tgo.transform.SetParent(go.transform,false);
        var tRT=tgo.AddComponent<RectTransform>();
        tRT.anchorMin=Vector2.zero; tRT.anchorMax=Vector2.one;
        tRT.offsetMin=new Vector2(48,0); tRT.offsetMax=new Vector2(-4,0);
        var t=tgo.AddComponent<TextMeshProUGUI>();
        t.text=label; t.fontSize=10; t.color=TEXT_MAIN;
        t.alignment=TextAlignmentOptions.Left;
        t.overflowMode=TextOverflowModes.Ellipsis;
        t.enableWordWrapping=false;
        if(koreanFont)t.font=koreanFont;
    }

    // 섹션 배지
    void Badge(Transform p, string text, Color col)
    {
        var go=new GameObject("Badge"); go.transform.SetParent(p,false);
        var le=go.AddComponent<LayoutElement>();
        le.preferredHeight=20; le.flexibleWidth=1;
        go.AddComponent<Image>().color=new Color(col.r,col.g,col.b,.1f);
        var tgo=new GameObject("T"); tgo.transform.SetParent(go.transform,false);
        var tRT=tgo.AddComponent<RectTransform>();
        tRT.anchorMin=Vector2.zero; tRT.anchorMax=Vector2.one;
        tRT.offsetMin=new Vector2(8,0); tRT.offsetMax=Vector2.zero;
        var t=tgo.AddComponent<TextMeshProUGUI>();
        t.text=text; t.fontSize=10; t.fontStyle=FontStyles.Bold;
        t.color=col; t.characterSpacing=3;
        t.alignment=TextAlignmentOptions.Left;
        if(koreanFont)t.font=koreanFont;
    }

    // X/Y/Z 입력 행
    TMP_InputField InputRow(Transform p, string lbl, Color col)
    {
        var row=new GameObject("R_"+lbl); row.transform.SetParent(p,false);
        var le=row.AddComponent<LayoutElement>();
        le.preferredHeight=26; le.flexibleWidth=1;
        var hlg=row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing=4; hlg.padding=new RectOffset(6,6,0,0);
        hlg.childForceExpandHeight=true; hlg.childForceExpandWidth=false;

        var lgo=new GameObject("L"); lgo.transform.SetParent(row.transform,false);
        lgo.AddComponent<LayoutElement>().preferredWidth=20;
        lgo.AddComponent<Image>().color=new Color(col.r,col.g,col.b,.2f);
        var lt=new GameObject("LT"); lt.transform.SetParent(lgo.transform,false);
        var ltRT=lt.AddComponent<RectTransform>();
        ltRT.anchorMin=Vector2.zero; ltRT.anchorMax=Vector2.one;
        ltRT.offsetMin=ltRT.offsetMax=Vector2.zero;
        var ltT=lt.AddComponent<TextMeshProUGUI>();
        ltT.text=lbl; ltT.fontSize=10; ltT.fontStyle=FontStyles.Bold;
        ltT.color=col; ltT.alignment=TextAlignmentOptions.Center;
        if(koreanFont)ltT.font=koreanFont;

        var f=MakeIF(row.transform);
        f.gameObject.AddComponent<LayoutElement>().flexibleWidth=1;
        return f;
    }

    // 버튼 행
    void BtnRow(Transform p, float h,
        params (string l, System.Action a, Color c)[] btns)
    {
        var row=new GameObject("BR"); row.transform.SetParent(p,false);
        var le=row.AddComponent<LayoutElement>();
        le.preferredHeight=h; le.flexibleWidth=1;
        var hlg=row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing=3; hlg.padding=new RectOffset(6,6,0,0);
        hlg.childForceExpandHeight=true; hlg.childForceExpandWidth=true;
        foreach(var(lbl,action,col)in btns)
        {
            var go=new GameObject("B"); go.transform.SetParent(row.transform,false);
            go.AddComponent<RectTransform>();
            go.AddComponent<Image>().color=col;
            var btn=go.AddComponent<Button>();
            var cs=btn.colors;
            cs.highlightedColor=new Color(
                Mathf.Min(col.r+.12f,1),
                Mathf.Min(col.g+.12f,1),
                Mathf.Min(col.b+.12f,1));
            cs.pressedColor=new Color(col.r*.5f,col.g*.5f,col.b*.5f);
            btn.colors=cs;
            btn.onClick.AddListener(()=>action?.Invoke());
            var tgo=new GameObject("T"); tgo.transform.SetParent(go.transform,false);
            var tRT=tgo.AddComponent<RectTransform>();
            tRT.anchorMin=Vector2.zero; tRT.anchorMax=Vector2.one;
            tRT.offsetMin=tRT.offsetMax=Vector2.zero;
            var t=tgo.AddComponent<TextMeshProUGUI>();
            t.text=lbl; t.fontSize=11;
            t.alignment=TextAlignmentOptions.Center; t.color=TEXT_H;
            if(koreanFont)t.font=koreanFont;
        }
    }

    // ── 공통 헬퍼 ─────────────────────────────────────
    Transform Rect(Transform p, string name, float h)
    {
        var go=new GameObject(name); go.transform.SetParent(p,false);
        var le=go.AddComponent<LayoutElement>();
        le.preferredHeight=h; le.flexibleWidth=1;
        go.AddComponent<Image>().color=BG_CARD;
        return go.transform;
    }

    void TopLine(Transform p, Color col, float thick)
    {
        var go=new GameObject("TL"); go.transform.SetParent(p,false);
        var rt=go.AddComponent<RectTransform>();
        rt.anchorMin=new Vector2(0,1); rt.anchorMax=new Vector2(1,1);
        rt.pivot=new Vector2(.5f,1);
        rt.offsetMin=new Vector2(0,-thick); rt.offsetMax=Vector2.zero;
        go.AddComponent<Image>().color=col;
    }

    void Hr(Transform p)
    {
        var go=new GameObject("Hr"); go.transform.SetParent(p,false);
        go.AddComponent<LayoutElement>().preferredHeight=1;
        go.AddComponent<Image>().color=new Color(1,1,1,.05f);
    }

    void Sp(Transform p, float h)
    {
        var go=new GameObject("Sp"); go.transform.SetParent(p,false);
        go.AddComponent<LayoutElement>().preferredHeight=h;
    }

    TMP_InputField MakeIF(Transform parent)
    {
        var go=new GameObject("IF"); go.transform.SetParent(parent,false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color=BG_INPUT;
        var area=new GameObject("A"); area.transform.SetParent(go.transform,false);
        var aRT=area.AddComponent<RectTransform>();
        aRT.anchorMin=Vector2.zero; aRT.anchorMax=Vector2.one;
        aRT.offsetMin=new Vector2(5,1); aRT.offsetMax=new Vector2(-5,-1);
        area.AddComponent<RectMask2D>();
        var tgo=new GameObject("T"); tgo.transform.SetParent(area.transform,false);
        var tRT=tgo.AddComponent<RectTransform>();
        tRT.anchorMin=Vector2.zero; tRT.anchorMax=Vector2.one;
        tRT.offsetMin=tRT.offsetMax=Vector2.zero;
        var t=tgo.AddComponent<TextMeshProUGUI>();
        t.fontSize=11; t.color=TEXT_MAIN;
        if(koreanFont)t.font=koreanFont;
        var pgo=new GameObject("P"); pgo.transform.SetParent(area.transform,false);
        var pRT=pgo.AddComponent<RectTransform>();
        pRT.anchorMin=Vector2.zero; pRT.anchorMax=Vector2.one;
        pRT.offsetMin=pRT.offsetMax=Vector2.zero;
        var ph=pgo.AddComponent<TextMeshProUGUI>();
        ph.text="0.00"; ph.fontSize=11; ph.color=TEXT_SUB;
        if(koreanFont)ph.font=koreanFont;
        var f=go.AddComponent<TMP_InputField>();
        f.textViewport=aRT; f.textComponent=t; f.placeholder=ph;
        f.contentType=TMP_InputField.ContentType.DecimalNumber;
        return f;
    }

    void EnsureEventSystem()
    {
        if(FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>()!=null)return;
        var es=new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }
}