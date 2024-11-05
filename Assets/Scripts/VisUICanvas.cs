using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ShuJun.Touch;
using ShuJun.Event;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(VisUICanvas))]
class VisUICanvasEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        VisUICanvas com = (VisUICanvas)target;
        com.cameraOffset1b = EditorGUILayout.Vector3Field("cameraOffset1b:", com.cameraOffset1b);
        com.cameraOffset2a = EditorGUILayout.Vector3Field("cameraOffset2a:", com.cameraOffset2a);
        com.cameraOffset2b = EditorGUILayout.Vector3Field("cameraOffset2b:", com.cameraOffset2b);
        com.cameraOffset1a = EditorGUILayout.Vector3Field("cameraOffset1a:", com.cameraOffset1a);
        com.cameraAngle1b = EditorGUILayout.Vector3Field("cameraAngle1:", com.cameraAngle1b);
        com.cameraAngle2a = EditorGUILayout.Vector3Field("cameraAngle2a:", com.cameraAngle2a);
        com.cameraAngle2b = EditorGUILayout.Vector3Field("cameraAngle2b:", com.cameraAngle2b);
        com.cameraAngle1a = EditorGUILayout.Vector3Field("cameraAngle1a:", com.cameraAngle1a);
        com.cameraSize1 = EditorGUILayout.Slider("cameraSize1:", com.cameraSize1, 30.0f,2500.0f);
        com.cameraSize2a = EditorGUILayout.Slider("cameraSize2a:", com.cameraSize2a, 30.0f, 2500.0f);
        com.cameraSize2b = EditorGUILayout.Slider("cameraSize2b:", com.cameraSize2b, 30.0f, 2500.0f);
    }
}
#endif

public class RegButton : MonoBehaviour
{
    public int regindex = -1;
    public int nodeindex = -1;
    public List<int> demands = null;
}

public class BackupRegButton : MonoBehaviour
{
    public int regindex = -1;
    public int nodeindex = -1;
    public HashSet<int> fail_edges = null;
    public List<int> backups = null;
}

public class BackupEdgeButton : MonoBehaviour
{
    public int failindex = -1;
    public int failedge = -1;
    public int nodeindex1 = -1;
    public int nodeindex2 = -1;
    public List<int> backups = null;
}

public class VisUICanvas : MonoBehaviour
{
    public Sprite regsprite;
    public Sprite backupregsprite;
    public Sprite failedgesprite;
    public Font defaultfont;
    private List<Vector3> regWorldpos = new List<Vector3>();
    private List<GameObject> regButtons = new List<GameObject>();
    private List<Vector3> backupregWorldpos = new List<Vector3>();
    private List<GameObject> backupregButtons = new List<GameObject>();
    private List<Vector3> backupedgeWorldpos = new List<Vector3>();
    private List<GameObject> backupedgeButtons = new List<GameObject>();
    private Dropdown ddSelectDemand = null;
    private Dropdown ddSelectBackup = null;

    // Start is called before the first frame update
    void Start()
    {
        //var ms = GameObject.FindFirstObjectByType<Achonor.LBSMap.MapServices>(FindObjectsInactive.Include);
        //if (ms != null && ms.enabled)
        //{
        //    ms.SetMapType(Achonor.LBSMap.MapType.Street);
        //    ms.SetZoomLevel(19);
        //    ms.SetMapCenter(new Vector2D(112.888678, 28.213555));
        //    ms.DoRender();
        //}

        var tMapPanel = transform.Find("MapPanel");
        if (tMapPanel)
        {
            var tButton1 = tMapPanel.Find("Button1");
            if (tButton1)
            {
                var b = tButton1.GetComponent<Button>();
                if (b)
                    b.onClick.AddListener(OnButton1);
            }

            var tButton2 = tMapPanel.Find("Button2");
            if (tButton2)
            {
                var b = tButton2.GetComponent<Button>();
                if (b)
                    b.onClick.AddListener(OnButton2);
            }

            var tButton3 = tMapPanel.Find("Button3");
            if (tButton3)
            {
                var b = tButton3.GetComponent<Button>();
                if (b)
                    b.onClick.AddListener(OnButton3);
            }
        }

        var tNavPanel = transform.Find("NavPanel");
        if (tNavPanel)
        {
            var tButton1 = tNavPanel.Find("Button1");
            if (tButton1)
            {
                var b = tButton1.GetComponent<Button>();
                if (b)
                    b.onClick.AddListener(OnResetCamera);
            }

            var tButton2 = tNavPanel.Find("Button2");
            if (tButton2)
            {
                var b = tButton2.GetComponent<Button>();
                if (b)
                {
                    b.onClick.AddListener(
                        ()=>
                        {
                            toggle2D3D();
                            b.gameObject.GetComponentInChildren<Text>().text = b3D? "2D":"3D";
                        }
                    );
                }
            }

            var tButton3 = tNavPanel.Find("Button3");
            if (tButton3)
            {
                var b = tButton3.GetComponent<Button>();
                if (b)
                {
                    b.onClick.AddListener(
                        ()=>
                        {
                            Zoom(-1);
                        }
                    );
                }
            }

            var tButton4 = tNavPanel.Find("Button4");
            if (tButton4)
            {
                var b = tButton4.GetComponent<Button>();
                if (b)
                {
                    b.onClick.AddListener(
                        () =>
                        {
                            Zoom(1);
                        }
                    );
                }
            }
        }

        var tDemandPanel = transform.Find("DemandPanel");
        if (VisGraph.Instance && tDemandPanel)
        {
            ddSelectDemand = tDemandPanel.GetComponentInChildren<Dropdown>();
            if (ddSelectDemand)
            {
                for(int i=0;i< VisGraph.Instance.demand_num;i++)
                {
                    Dropdown.OptionData data = new Dropdown.OptionData();
                    data.text = "Demand:" + i.ToString();
                    ddSelectDemand.options.Add(data);
                }
            }
            ddSelectDemand.onValueChanged.AddListener(OnSelectDemand);

            var tButtonGo = tDemandPanel.Find("ButtonGo");
            if (tButtonGo)
            {
                var b = tButtonGo.GetComponent<Button>();
                if (b)
                    b.onClick.AddListener(OnButtonGo);
            }
        }

        var tBackupPanel = transform.Find("BackupPanel");
        if (VisGraph.Instance && tBackupPanel)
        {
            ddSelectBackup = tBackupPanel.GetComponentInChildren<Dropdown>();
            if (ddSelectBackup)
            {
                foreach (int e in VisGraph.Instance.fail_edges)
                {
                    Dropdown.OptionData data = new Dropdown.OptionData();
                    data.text = "Backups for:" + e.ToString();
                    ddSelectBackup.options.Add(data);
                }
            }
            ddSelectBackup.onValueChanged.AddListener(OnSelectBackup);

            var tButtonGo = tBackupPanel.Find("ButtonGo");
            if (tButtonGo)
            {
                var b = tButtonGo.GetComponent<Button>();
                if (b)
                    b.onClick.AddListener(OnButtonGo);
            }
        }

        EventManager.Register<TouchMoveEvent>((param) => {
            Vector3 offset = Vector3.zero;
            offset.x = param.MoveOffset.x;
            offset.z = param.MoveOffset.y;
            cameraOffset1b -= offset;
            cameraOffset2a -= offset;
            cameraOffset2b -= offset;
            cameraOffset1a -= offset;
            //var ms = GameObject.FindFirstObjectByType<Achonor.LBSMap.MapServices>(FindObjectsInactive.Include);
            //if (ms != null)
            //{
            //    ms.MoveMap(param.MoveOffset);
            //    ms.DoRender();
            //}
        }, this);

        EventManager.Register<TouchZoomEvent>((param) => {
            Zoom(param.ChangeZoom);
        }, this);

        EventManager.Register((TouchRotateEvent param) => {
            float angle = Mathf.Atan2(param.ChangedEuler.y, param.ChangedEuler.x);
            cameraAngle1a.y += angle;
            cameraAngle1b.y += angle;
            cameraAngle2a.y += angle;
            cameraAngle2b.y += angle;
            //mMapServices.RotateMap(param.ChangedEuler);
        }, this);

        OnResetCamera();


        InitRegButtons();
        InitBackupRegButtons();
        InitBackupEdgeButtons();
        backupregpanel.gameObject.SetActive(false);
        backupregpanel2.gameObject.SetActive(false);
        backupedgepanel.gameObject.SetActive(false);
    }

    private void Zoom(float ChangeZoom)
    {
        {
            float camLen = cameraOffset1b.magnitude;
            camLen -= ChangeZoom * 100.0f;
            camLen = Mathf.Clamp(camLen, 50.0f, 2000.0f);
            cameraOffset1b = camLen * cameraOffset1b.normalized;
        }
        {
            float camLen = cameraOffset2a.magnitude;
            camLen -= ChangeZoom * 100.0f;
            camLen = Mathf.Clamp(camLen, 50.0f, 2000.0f);
            cameraOffset2a = camLen * cameraOffset2a.normalized;
        }
        {
            float camLen = cameraOffset2b.magnitude;
            camLen -= ChangeZoom * 100.0f;
            camLen = Mathf.Clamp(camLen, 50.0f, 2000.0f);
            cameraOffset2b = camLen * cameraOffset2b.normalized;
        }
        {
            float camLen = cameraOffset1a.magnitude;
            camLen -= ChangeZoom * 100.0f;
            camLen = Mathf.Clamp(camLen, 50.0f, 2000.0f);
            cameraOffset1a = camLen * cameraOffset1a.normalized;
        }
        {
            cameraSize1 -= ChangeZoom * 100.0f;
            cameraSize1 = Mathf.Clamp(cameraSize1, 30.0f, 2500.0f);
        }
        {
            cameraSize2a -= ChangeZoom * 100.0f;
            cameraSize2a = Mathf.Clamp(cameraSize2a, 30.0f, 2500.0f);
        }
        {
            cameraSize2b -= ChangeZoom * 100.0f;
            cameraSize2b = Mathf.Clamp(cameraSize2b, 30.0f, 2500.0f);
        }
    }

    private RectTransform regpanel = null;
    private RectTransform regpanel2 = null;
    private ScrollRect contextrect = null;

    private RectTransform backupregpanel = null;
    private RectTransform backupregpanel2 = null;
    private ScrollRect backupregcontextrect = null;

    private RectTransform backupedgepanel = null;
    //private RectTransform backupedgepanel2 = null;
    //private ScrollRect backupedgecontextrect = null;

    void InitRegButtons()
    {
        regpanel = transform.Find("RegPanel") as RectTransform;
        regpanel2 = transform.Find("RegPanel2") as RectTransform;
        if (regpanel2)
        {
            contextrect = regpanel2.Find("ScrollView").GetComponentInChildren<ScrollRect>(true);
            contextrect.gameObject.SetActive(false);
            var buttontemplate = regpanel2.Find("Button").gameObject;
            buttontemplate.transform.localPosition = Vector3.left * 10000;
        }

        if (VisGraph.Instance && regpanel)
        {
            List<List<int>> NodeRegCount = new List<List<int>>(VisGraph.Instance.node_num);
            for (int i = 0; i < VisGraph.Instance.node_num; i++)
                NodeRegCount.Add(null);
            for (int d = 0; d < VisGraph.Instance.demand_num; d++)
            {
                foreach (var v in VisGraph.Instance.demands[d].reg)
                {
                    Transform nodetran = VisGraph.Instance.nodeobjs[v].transform;
                    if (NodeRegCount[v] == null)
                        NodeRegCount[v] = new List<int>();
                    NodeRegCount[v].Add(d);
                }
            }

            int regIndex = 0;
            for (int v = 0; v < VisGraph.Instance.node_num; v++)
            {
                if (NodeRegCount[v]!=null)
                {
                    Vector3 worldpos = VisGraph.Instance.nodeobjs[v].transform.position + new Vector3(-95, 135, -10);
                    Vector3 screenpos = Camera.main.WorldToScreenPoint(worldpos);
                    Vector2 localpt = Vector2.zero;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(regpanel, screenpos, null, out localpt);
                    GameObject goReg = new GameObject();
                    goReg.name = "Reg" + regIndex.ToString() + "_on_v" + v.ToString();
                    goReg.transform.parent = regpanel;
                    goReg.transform.localPosition = localpt;
                    goReg.transform.localScale = Vector3.one * Mathf.Sqrt(Mathf.Sqrt(NodeRegCount[v].Count)) * 0.5f;
                    Image img = goReg.AddComponent<Image>();
                    img.sprite = regsprite;
                    Button b = goReg.AddComponent<Button>();
                    b.onClick.AddListener(
                        () =>
                        {
                            OnRegClick(goReg);
                        }
                    );
                    var r = goReg.AddComponent<RegButton>();
                    r.regindex = regIndex;
                    r.nodeindex = v;
                    r.demands = NodeRegCount[v];

                    regButtons.Add(goReg);
                    regWorldpos.Add(worldpos);

                    GameObject goText = new GameObject();
                    goText.name = "reg:" + v.ToString();
                    goText.transform.parent = goReg.transform;
                    goText.transform.localPosition = Vector3.zero;
                    goText.transform.localScale = Vector3.one;
                    Text text = goText.AddComponent<Text>();
                    text.font = defaultfont;
                    text.text = ((char)((int)'¢Ù' + v-1)).ToString();
                    text.color = new Color(0,0,0.5f);
                    text.fontSize = 24;
                    text.fontStyle = FontStyle.Bold;
                    text.alignment = TextAnchor.LowerCenter;
                    RectTransform rtText = goText.GetComponent<RectTransform>();
                    rtText.sizeDelta = new Vector2(48, 48);
                    rtText.anchoredPosition = Vector2.zero;

                    regIndex++;
                }
            }
        }
    }
    void InitBackupRegButtons()
    {
        backupregpanel = transform.Find("BackupRegPanel") as RectTransform;
        backupregpanel2 = transform.Find("BackupRegPanel2") as RectTransform;
        if (backupregpanel2)
        {
            backupregcontextrect = backupregpanel2.Find("ScrollView").GetComponentInChildren<ScrollRect>(true);
            backupregcontextrect.gameObject.SetActive(false);
            var buttontemplate = backupregpanel2.Find("Button").gameObject;
            buttontemplate.transform.localPosition = Vector3.left * 10000;
        }

        if (VisGraph.Instance && backupregpanel)
        {
            List<List<int>> NodeBackupRegCount = new List<List<int>>(VisGraph.Instance.node_num);
            List<HashSet<int>> NodeFailEdgeCount = new List<HashSet<int>>(VisGraph.Instance.node_num);
            for (int i = 0; i < VisGraph.Instance.node_num; i++)
            {
                NodeBackupRegCount.Add(null);
                NodeFailEdgeCount.Add(null);
            }
            for (int b = 0; b < VisGraph.Instance.backups.Count; b++)
            {
                if (VisGraph.Instance.backups[b].backupregs != null)
                {
                    foreach (var v in VisGraph.Instance.backups[b].backupregs)
                    {
                        Transform nodetran = VisGraph.Instance.nodeobjs[v].transform;
                        if (NodeBackupRegCount[v] == null)
                            NodeBackupRegCount[v] = new List<int>();
                        if (NodeFailEdgeCount[v] == null)
                            NodeFailEdgeCount[v] = new HashSet<int>();

                        NodeBackupRegCount[v].Add(b);
                        NodeFailEdgeCount[v].Add(VisGraph.Instance.backups[b].failed_edge);
                    }
                }
            }

            int regIndex = 0;
            for (int v = 0; v < VisGraph.Instance.node_num; v++)
            {
                if (NodeFailEdgeCount[v] != null)
                {
                    Vector3 worldpos = VisGraph.Instance.nodeobjs[v].transform.position + new Vector3(-95, 135, -10);
                    Vector3 screenpos = Camera.main.WorldToScreenPoint(worldpos);
                    Vector2 localpt = Vector2.zero;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(backupregpanel, screenpos, null, out localpt);
                    GameObject goReg = new GameObject();
                    goReg.name = "BackupReg" + regIndex.ToString() + "_on_v" + v.ToString();
                    goReg.transform.parent = backupregpanel;
                    goReg.transform.localPosition = localpt;
                    goReg.transform.localScale = Vector3.one * Mathf.Sqrt(Mathf.Sqrt(NodeFailEdgeCount[v].Count)) * 0.5f;
                    Image img = goReg.AddComponent<Image>();
                    img.sprite = backupregsprite;
                    Button bb = goReg.AddComponent<Button>();
                    bb.onClick.AddListener(
                        () =>
                        {
                            OnBackupRegClick(goReg);
                        }
                    );
                    var r = goReg.AddComponent<BackupRegButton>();
                    r.regindex = regIndex;
                    r.nodeindex = v;
                    r.backups = NodeBackupRegCount[v];
                    r.fail_edges = NodeFailEdgeCount[v];

                    backupregButtons.Add(goReg);
                    backupregWorldpos.Add(worldpos);

                    GameObject goText = new GameObject();
                    goText.name = "backupreg:" + v.ToString();
                    goText.transform.parent = goReg.transform;
                    goText.transform.localPosition = Vector3.zero;
                    goText.transform.localScale = Vector3.one;
                    Text text = goText.AddComponent<Text>();
                    text.font = defaultfont;
                    text.text = ((char)((int)'¢Ù'+v-1)).ToString();
                    text.color = new Color(0,0.5f,0);
                    text.fontSize = 24;
                    text.fontStyle = FontStyle.Bold;
                    text.alignment = TextAnchor.LowerCenter;
                    RectTransform rtText = goText.GetComponent<RectTransform>();
                    rtText.sizeDelta = new Vector2(48, 48);
                    rtText.anchoredPosition = Vector2.zero;

                    regIndex++;
                }
            }
        }
    }

    void InitBackupEdgeButtons()
    {
        backupedgepanel = transform.Find("BackupEdgePanel") as RectTransform;
        //backupedgepanel2 = transform.Find("BackupEdgePanel2") as RectTransform;
        //if (backupedgepanel2)
        //{
        //    backupedgecontextrect = backupedgepanel2.Find("ScrollView").GetComponentInChildren<ScrollRect>(true);
        //    backupedgecontextrect.gameObject.SetActive(false);
        //    var buttontemplate = backupedgepanel2.Find("Button").gameObject;
        //    buttontemplate.transform.localPosition = Vector3.left * 10000;
        //}

        if (VisGraph.Instance && backupedgepanel)
        {
            List<List<int>> LinkFailBackupCount = new List<List<int>>(VisGraph.Instance.link_num);
            for (int i = 0; i < VisGraph.Instance.link_num; i++)
            {
                LinkFailBackupCount.Add(null);
            }
            for (int b = 0; b < VisGraph.Instance.backups.Count; b++)
            {
                int e = VisGraph.Instance.backups[b].failed_edge;
                if (e >=0)
                {
                    if (LinkFailBackupCount[e] == null)
                        LinkFailBackupCount[e] = new List<int>();

                    LinkFailBackupCount[e].Add(b);
                }
            }

            int failIndex = 0;
            for (int e = 0; e < VisGraph.Instance.link_num; e++)
            {
                if (LinkFailBackupCount[e] != null)
                {
                    int v1 = VisGraph.Instance.graph.links[e].u;
                    int v2 = VisGraph.Instance.graph.links[e].v;
                    Transform nodetran1 = VisGraph.Instance.nodeobjs[v1].transform;
                    Transform nodetran2 = VisGraph.Instance.nodeobjs[v2].transform;
                    Vector3 posc = (nodetran1.position + nodetran2.position) * 0.5f;

                    Vector3 worldpos = posc + new Vector3(-95, 135, -10);
                    Vector3 screenpos = Camera.main.WorldToScreenPoint(worldpos);
                    Vector2 localpt = Vector2.zero;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(backupedgepanel, screenpos, null, out localpt);
                    GameObject goFailEdge = new GameObject();
                    goFailEdge.name = "FailEdge:" + e.ToString();
                    goFailEdge.transform.parent = backupedgepanel;
                    goFailEdge.transform.localPosition = localpt;
                    goFailEdge.transform.localScale = Vector3.one * Mathf.Sqrt(Mathf.Sqrt(LinkFailBackupCount[e].Count)) * 0.2f;
                    Image img = goFailEdge.AddComponent<Image>();
                    img.sprite = failedgesprite;
                    Button bb = goFailEdge.AddComponent<Button>();
                    bb.onClick.AddListener(
                        () =>
                        {
                            OnBackupEdgeClick(goFailEdge);
                        }
                    );
                    var r = goFailEdge.AddComponent<BackupEdgeButton>();
                    r.failindex = failIndex;
                    r.failedge = e;
                    r.nodeindex1 = v1;
                    r.nodeindex2 = v2;
                    r.backups = LinkFailBackupCount[e];

                    backupedgeButtons.Add(goFailEdge);
                    backupedgeWorldpos.Add(worldpos);

                    GameObject goText = new GameObject();
                    goText.name = "FailEdge:" + e.ToString();
                    goText.transform.parent = goFailEdge.transform;
                    goText.transform.localPosition = Vector3.zero;
                    goText.transform.localScale = Vector3.one;
                    Text text = goText.AddComponent<Text>();
                    text.font = defaultfont;
                    text.fontStyle = FontStyle.Bold;
                    text.text = e.ToString();
                    text.color = Color.red;
                    text.fontSize = 64;
                    text.alignment = TextAnchor.LowerRight;
                    RectTransform rtText = goText.GetComponent<RectTransform>();
                    rtText.sizeDelta = new Vector2(150,150);
                    rtText.anchoredPosition = Vector2.right * 50.0f;

                    failIndex++;
                }
            }
        }
    }


    void OnRegClick(GameObject go)
    {
        var r = go.GetComponent<RegButton>();
        if (!r)
            return;

        if (!contextrect)
            return;

        if (!regpanel2)
            return;

        contextrect.transform.localPosition = r.transform.localPosition;

        List<Transform> deletelist = new List<Transform>();
        for(int i=0;i< contextrect.content.childCount;i++)
        {
            var child = contextrect.content.GetChild(i);
            deletelist.Add(child);
        }
        foreach(var child in deletelist)
        {
            GameObject.Destroy(child.gameObject);
        }

        Vector2 contextsize = Vector2.zero;
        contextsize.x = 100.0f;
        contextsize.y = 30 * r.demands.Count;
        contextrect.GetComponent<RectTransform>().sizeDelta = contextsize;
        contextsize.x = 0;
        contextrect.content.sizeDelta = contextsize;
        var buttontemplate = regpanel2.Find("Button").gameObject;
        int index = 0;
        foreach (var demand in r.demands)
        {
            GameObject goButton= GameObject.Instantiate(buttontemplate);
            goButton.gameObject.SetActive(true);
            var t = goButton.GetComponentInChildren<Text>();
            if (t)
                t.text= "Demand:" + demand.ToString();
            Vector2 pos = Vector2.zero;
            pos.x = 0;
            pos.y = contextsize.y/2 -15 - 30 * index;
            goButton.transform.parent = contextrect.content;
            goButton.transform.localScale = Vector3.one;
            goButton.GetComponent<RectTransform>().anchoredPosition = pos;

            goButton.GetComponentInChildren<Button>().onClick.AddListener
            (
                ()=>
                {
                    ddSelectDemand.value = demand + 1;
                }
            );

            index++;
        }

        contextrect.gameObject.SetActive(true);
    }

    void OnBackupRegClick(GameObject go)
    {
        backupregpanel2.gameObject.SetActive(true);

        var r = go.GetComponent<BackupRegButton>();
        if (!r)
            return;

        if (!backupregcontextrect)
            return;

        if (!backupregpanel2)
            return;

        backupregcontextrect.transform.localPosition = r.transform.localPosition;

        List<Transform> deletelist = new List<Transform>();
        for (int i = 0; i < backupregcontextrect.content.childCount; i++)
        {
            var child = backupregcontextrect.content.GetChild(i);
            deletelist.Add(child);
        }
        foreach (var child in deletelist)
        {
            GameObject.Destroy(child.gameObject);
        }

        HashSet<int> edges;
        if (ddSelectDemand.value == 0)
        {
            edges = r.fail_edges;
        }
        else
        {
            edges = new HashSet<int>();
            foreach (int _b in r.backups)
            {
                foreach (int b in VisGraph.Instance.demands[ddSelectDemand.value - 1].backups)
                {
                    if (_b == b)
                    {
                        edges.Add(VisGraph.Instance.backups[b].failed_edge);
                    }
                }
            }
        }

        Vector2 contextsize = Vector2.zero;
        contextsize.x = 100.0f;
        contextsize.y = 30 * edges.Count;
        backupregcontextrect.GetComponent<RectTransform>().sizeDelta = contextsize;
        contextsize.x = 0;
        backupregcontextrect.content.sizeDelta = contextsize;
        var buttontemplate = backupregpanel2.Find("Button").gameObject;
        int index = 0;
        foreach (var e in edges)
        {
            GameObject goButton = GameObject.Instantiate(buttontemplate);
            goButton.gameObject.SetActive(true);
            var t = goButton.GetComponentInChildren<Text>();
            if (t)
                t.text = "Backups for:" + e.ToString();
            Vector2 pos = Vector2.zero;
            pos.x = 0;
            pos.y = contextsize.y / 2 - 15 - 30 * index;
            goButton.transform.parent = backupregcontextrect.content;
            goButton.transform.localScale = Vector3.one;
            goButton.GetComponent<RectTransform>().anchoredPosition = pos;

            goButton.GetComponentInChildren<Button>().onClick.AddListener
            (
                () =>
                {
                    VisGraph.Instance.HighlightAllBackup(false);
                    if (ddSelectDemand.value == 0)
                    {
                        for (int b = 0; b < VisGraph.Instance.backups.Count; b++)
                            if (VisGraph.Instance.backups[b].failed_edge == e)
                                VisGraph.Instance.HighlightBackup(b, true);
                    }
                    else
                    {
                        foreach(int b in VisGraph.Instance.demands[ddSelectDemand.value - 1].backups)
                            if (VisGraph.Instance.backups[b].failed_edge == e)
                                VisGraph.Instance.HighlightBackup(b, true);
                    }
                    backupregpanel2.gameObject.SetActive(false);
                }
            );

            index++;
        }

        backupregcontextrect.gameObject.SetActive(true);
    }

    void OnBackupEdgeClick(GameObject go)
    {
        var r = go.GetComponent<BackupEdgeButton>();
        if (!r)
            return;

        VisGraph.Instance.HighlightAllBackup(false);
        if (ddSelectDemand.value == 0)
        {
            for (int b = 0; b < VisGraph.Instance.backups.Count; b++)
                if (VisGraph.Instance.backups[b].failed_edge == r.failedge)
                    VisGraph.Instance.HighlightBackup(b, true);
        }
        else
        {
            foreach (int b in VisGraph.Instance.demands[ddSelectDemand.value - 1].backups)
                if (VisGraph.Instance.backups[b].failed_edge == r.failedge)
                    VisGraph.Instance.HighlightBackup(b, true);
        }

        //backupedgepanel2.gameObject.SetActive(true);


        //if (!backupedgecontextrect)
        //    return;

        //if (!backupedgepanel2)
        //    return;

        //backupedgecontextrect.transform.localPosition = r.transform.localPosition;

        //List<Transform> deletelist = new List<Transform>();
        //for (int i = 0; i < backupedgecontextrect.content.childCount; i++)
        //{
        //    var child = backupedgecontextrect.content.GetChild(i);
        //    deletelist.Add(child);
        //}
        //foreach (var child in deletelist)
        //{
        //    GameObject.Destroy(child.gameObject);
        //}

        //HashSet<int> edges;
        //if (ddSelectDemand.value == 0)
        //{
        //    edges = r.fail_edges;
        //}
        //else
        //{
        //    edges = new HashSet<int>();
        //    foreach (int _b in r.backups)
        //    {
        //        foreach (int b in VisGraph.Instance.demands[ddSelectDemand.value - 1].backups)
        //        {
        //            if (_b == b)
        //            {
        //                edges.Add(VisGraph.Instance.backups[b].failed_edge);
        //            }
        //        }
        //    }
        //}

        //Vector2 contextsize = Vector2.zero;
        //contextsize.x = 100.0f;
        //contextsize.y = 30 * edges.Count;
        //backupedgecontextrect.GetComponent<RectTransform>().sizeDelta = contextsize;
        //contextsize.x = 0;
        //backupedgecontextrect.content.sizeDelta = contextsize;
        //var buttontemplate = backupregpanel2.Find("Button").gameObject;
        //int index = 0;
        //foreach (var e in edges)
        //{
        //    GameObject goButton = GameObject.Instantiate(buttontemplate);
        //    goButton.gameObject.SetActive(true);
        //    var t = goButton.GetComponentInChildren<Text>();
        //    if (t)
        //        t.text = "Backups for:" + e.ToString();
        //    Vector2 pos = Vector2.zero;
        //    pos.x = 0;
        //    pos.y = contextsize.y / 2 - 15 - 30 * index;
        //    goButton.transform.parent = backupedgecontextrect.content;
        //    goButton.transform.localScale = Vector3.one;
        //    goButton.GetComponent<RectTransform>().anchoredPosition = pos;

        //    goButton.GetComponentInChildren<Button>().onClick.AddListener
        //    (
        //        () =>
        //        {

        //        }
        //    );

        //    index++;
        //}

        //backupedgecontextrect.gameObject.SetActive(true);
    }


    public void OnButton1()
    {
        //var ms = GameObject.FindFirstObjectByType<Achonor.LBSMap.MapServices>(FindObjectsInactive.Include);
        //if (ms != null)
        //{
        //    ms.gameObject.transform.parent.gameObject.SetActive(true);
        //    ms.SetMapType(Achonor.LBSMap.MapType.Satellite);
        //    ms.SetZoomLevel(19);
        //    ms.SetMapCenter(new Vector2D(112.888678, 28.213555));
        //    ms.DoRender();
        //}

        var mc = GameObject.FindFirstObjectByType<Grid>(FindObjectsInactive.Include);
        if (mc != null)
        {
            mc.gameObject.SetActive(false);
        }

    }

    public void OnButton2()
    {
        //var ms = GameObject.FindFirstObjectByType<Achonor.LBSMap.MapServices>(FindObjectsInactive.Include);
        //if (ms != null)
        //{
        //    ms.gameObject.transform.parent.gameObject.SetActive(true);
        //    ms.SetMapType(Achonor.LBSMap.MapType.Street);
        //    ms.SetZoomLevel(19);
        //    ms.SetMapCenter(new Vector2D(112.888678, 28.213555));
        //    ms.DoRender();
        //}

        var mc = GameObject.FindFirstObjectByType<Grid>(FindObjectsInactive.Include);
        if (mc != null)
        {
            mc.gameObject.SetActive(false);
        }

    }

    public void OnButton3()
    {
        //var ms = GameObject.FindFirstObjectByType<Achonor.LBSMap.MapServices>(FindObjectsInactive.Include);
        //if (ms != null)
        //{
        //    ms.gameObject.transform.parent.gameObject.SetActive(false);
        //}
        
        var mc = GameObject.FindFirstObjectByType<Grid>(FindObjectsInactive.Include);
        if (mc != null)
        {
            mc.gameObject.SetActive(true);
        }
    }


    public void OnSelectDemand(int index)
    {
        var visgraph = VisGraph.Instance;
        if (visgraph)
        {
            visgraph.HighlightAllDemand(false);
            visgraph.HighlightAllBackup(false);
            if (index>0)
                visgraph.HighlightDemand(index - 1,true);

            ddSelectBackup.options.Clear();
            {
                Dropdown.OptionData data = new Dropdown.OptionData();
                data.text = "Backups for: none";
                ddSelectBackup.options.Add(data);
            }
            {
                Dropdown.OptionData data = new Dropdown.OptionData();
                data.text = "Backups for: all";
                ddSelectBackup.options.Add(data);
            }
            if (index==0)
            {
                foreach(int e in visgraph.fail_edges)
                {
                    Dropdown.OptionData data = new Dropdown.OptionData();
                    data.text = "Backups for:" + e.ToString();
                    ddSelectBackup.options.Add(data);
                }
            }
            else
            {
                foreach(int b in visgraph.demands[index-1].backups)
                {
                    int failed_edge = visgraph.backups[b].failed_edge;
                    Dropdown.OptionData data = new Dropdown.OptionData();
                    data.text = "Backups for:" + failed_edge.ToString();
                    ddSelectBackup.options.Add(data);
                }
            }
        }

        if (contextrect)
            contextrect.gameObject.SetActive(false);

        regpanel.gameObject.SetActive(true);
        regpanel2.gameObject.SetActive(true);
        backupregpanel.gameObject.SetActive(false);
        backupregpanel2.gameObject.SetActive(false);
        backupedgepanel.gameObject.SetActive(false);
        ddSelectBackup.value = 0;
    }

    public void OnSelectBackup(int index)
    {
        var visgraph = VisGraph.Instance;
        if (visgraph)
        {
            visgraph.HighlightAllBackup(false);
            if (index == 0)
            {
                if (ddSelectDemand.value>0)
                    visgraph.HighlightDemand(ddSelectDemand.value - 1, true);
            }
            else
            {
                visgraph.HighlightAllDemand(false);
            }

            if (ddSelectDemand.value == 0)
            {
                if (index == 1) //show all backups for all demand
                { 
                    for(int b = 0;b<visgraph.backups.Count;b++)
                        visgraph.HighlightBackup(b, true);
                }
                else if (index >= 2)
                {
                    int e = visgraph.fail_edges[index - 2];
                    for(int b = 0; b < visgraph.backups.Count; b++)
                        if (visgraph.backups[b].failed_edge == e)
                               visgraph.HighlightBackup(b, true);
                }
            }
            else
            {
                if (index == 1) //show all backups for selected demand
                {
                    foreach (int b in visgraph.demands[ddSelectDemand.value - 1].backups)
                        visgraph.HighlightBackup(b, true);
                }
                else if (index >= 2)
                {
                    int b = visgraph.demands[ddSelectDemand.value - 1].backups[index - 2];
                    visgraph.HighlightBackup(b, true);
                }
            }

            if (index==0)
            {
                regpanel.gameObject.SetActive(true);
                regpanel2.gameObject.SetActive(true);
                backupregpanel.gameObject.SetActive(false);
                backupregpanel2.gameObject.SetActive(false);
                backupedgepanel.gameObject.SetActive(false);
                //backupedgepanel2.gameObject.SetActive(false);
            }
            else
            {
                regpanel.gameObject.SetActive(false);
                regpanel2.gameObject.SetActive(false);
                backupregpanel.gameObject.SetActive(true);
                backupregpanel2.gameObject.SetActive(false);
                backupedgepanel.gameObject.SetActive(true);
                //backupedgepanel2.gameObject.SetActive(false);
                if (ddSelectDemand.value > 0)
                {
                    foreach (var go in backupregButtons)
                    {
                        var bb = go.GetComponent<BackupRegButton>();
                        bool vis = false;
                        if (index == 1)
                        {
                            foreach (int b in visgraph.demands[ddSelectDemand.value - 1].backups)
                            {
                                if (bb.backups.Contains(b))
                                {
                                    vis = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            int b = visgraph.demands[ddSelectDemand.value - 1].backups[index - 2];
                            if (bb.backups.Contains(b))
                            {
                                vis = true;
                            }
                        }
                        go.gameObject.SetActive(vis);
                    }
                    foreach (var go in backupedgeButtons)
                    {
                        var bb = go.GetComponent<BackupEdgeButton>();
                        bool vis = false;
                        if (index == 1)
                        {
                            foreach (int b in visgraph.demands[ddSelectDemand.value - 1].backups)
                            {
                                if (bb.backups.Contains(b))
                                {
                                    vis = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            int b = visgraph.demands[ddSelectDemand.value - 1].backups[index - 2];
                            if (bb.backups.Contains(b))
                            {
                                vis = true;
                            }
                        }
                        go.gameObject.SetActive(vis);
                    }
                }
                else
                {
                    if (index == 1)
                    {
                        foreach (var go in backupregButtons)
                        {
                            go.gameObject.SetActive(true);
                        }
                    }
                    else if (index>=2)
                    {
                        int e = visgraph.fail_edges[index - 2];
                        foreach (var go in backupregButtons)
                        {
                            var bb = go.GetComponent<BackupRegButton>();
                            bool vis = false;
                            foreach (int b in bb.backups)
                            {
                                if (visgraph.backups[b].failed_edge == e)
                                {
                                    vis = true;
                                    break;
                                }
                            }
                            go.gameObject.SetActive(vis);
                        }
                    }

                    if (index == 1)
                    {
                        foreach (var go in backupedgeButtons)
                        {
                            go.gameObject.SetActive(true);
                        }
                    }
                    else if (index>=2)
                    {
                        int e = visgraph.fail_edges[index - 2];
                        foreach (var go in backupedgeButtons)
                        {
                            var bb = go.GetComponent<BackupEdgeButton>();
                            bool vis = false;
                            foreach (int b in bb.backups)
                            {
                                if (visgraph.backups[b].failed_edge==e)
                                {
                                    vis = true;
                                    break;
                                }
                            }
                            go.gameObject.SetActive(vis);
                        }
                    }
                }
            }
        }
    }

    public void OnButtonGo()
    {
        int index = ddSelectDemand.value;

        OnResetCamera();

        var visgraph = VisGraph.Instance;
        if (visgraph)
        {
            visgraph.StopAllDemandEnumerator();
            visgraph.HideAllDemands();
            if (index == 0)
                visgraph.ShowAllDemands();
            else
                visgraph.ShowDemand(index - 1);
            RefreshRegButtons();
        }
    }

    private void RefreshRegButtons()
    {
        var visgraph = VisGraph.Instance;
        if (visgraph.CurrentDemand == -1)
        {
            for (int i = 0; i < regButtons.Count; i++)
            {
                regButtons[i].SetActive(true);
            }
        }
        else
        {
            for (int i = 0; i < regButtons.Count; i++)
            {
                RegButton regbutton = regButtons[i].GetComponent<RegButton>();
                bool isDemand = regbutton.demands.Contains(visgraph.CurrentDemand);
                regButtons[i].SetActive(isDemand);
            }
        }
    }

    public void OnResetCamera()
    {
        cameraOffset1a = new Vector3(-30, 500, -50);
        cameraOffset1b = new Vector3(0, 140, -320);
        cameraOffset2a = new Vector3(-30, 500, -50);
        cameraOffset2b = new Vector3(-321.683075f, 750, -1137.22717f);

        cameraAngle1a = new Vector3(90, 0, 0);
        cameraAngle1b = new Vector3(30, 0, 0);
        cameraAngle2a = new Vector3(85, 0, 0);
        cameraAngle2b = new Vector3(40, 15, 0);

        cameraSize1 = 250.0f;
        cameraSize2a = 1200.0f;
        cameraSize2b = 300.0f;
    }

    [System.NonSerialized]
    public Vector3 cameraOffset1b = Vector3.zero;
    [System.NonSerialized]
    public Vector3 cameraOffset2a = Vector3.zero;
    [System.NonSerialized]
    public Vector3 cameraOffset2b = Vector3.zero;
    [System.NonSerialized]
    public Vector3 cameraOffset1a = Vector3.zero;
    [System.NonSerialized]
    public Vector3 cameraAngle1b = Vector3.zero;
    [System.NonSerialized]
    public Vector3 cameraAngle2a = Vector3.zero;
    [System.NonSerialized]
    public Vector3 cameraAngle2b = Vector3.zero;
    [System.NonSerialized]
    public Vector3 cameraAngle1a = Vector3.zero;
    [System.NonSerialized]
    public float cameraSize1 = 0.0f;
    [System.NonSerialized]
    public float cameraSize2a = 0.0f;
    [System.NonSerialized]
    public float cameraSize2b = 0.0f;

    private bool b3D = true;

    public void toggle2D3D()
    {
        b3D = !b3D;
    }

    // Update is called once per frame
    void Update()
    {
        var visgraph = VisGraph.Instance;
        var cam = Camera.main;
        if (visgraph)
        {
            if (visgraph.CurrentDemand != -1)
            {
                Vector3 pathpos = visgraph.GetCurrentPosition();
                if (!b3D)
                {
                    if (cam)
                    {
                        cam.transform.position = pathpos + cameraOffset1a;
                        cam.transform.eulerAngles = cameraAngle1a;
                        cam.fieldOfView = 75.0f;
                        cam.orthographicSize = cameraSize1;
                        cam.orthographic = true;
                    }
                }
                else
                {
                    if (cam)
                    {
                        cam.transform.position = pathpos + cameraOffset1b;
                        cam.transform.eulerAngles = cameraAngle1b;
                        cam.fieldOfView = 45.0f;
                        cam.orthographic = false;
                    }
                }
            }
            else
            {
                if (!b3D)
                {
                    if (cam)
                    {
                        cam.transform.position = cameraOffset2a;
                        cam.transform.eulerAngles = cameraAngle2a;
                        cam.fieldOfView = 75.0f;
                        cam.orthographicSize = cameraSize2a;
                        cam.orthographic = true;
                    }
                }
                else
                {
                    if (cam)
                    {
                        cam.transform.position = cameraOffset2b;
                        cam.transform.eulerAngles = cameraAngle2b;
                        cam.fieldOfView = 65.0f;
                        cam.orthographicSize = cameraSize2b;
                        cam.orthographic = false;
                    }
                }
            }

        }

        UpdateRegButtons();
    }

    private Vector3 lastcampos = Vector3.zero;
    private void UpdateRegButtons()
    {
        if (!regpanel)
            return;
        Camera cam = Camera.main;
        if (!cam)
            return;

        bool changed = false;
        float dis = Vector3.Distance(cam.transform.position, lastcampos);
        if (dis>0.001f)
        {
            lastcampos = cam.transform.position;
            changed = true;
        }
        if (!changed)
            return;

        for(int i=0;i<regButtons.Count;i++)
        {
            Vector3 worldpos = regWorldpos[i];
            Vector3 screenpos = cam.WorldToScreenPoint(worldpos);
            if (screenpos.z < 0)
            {
                Vector2 localpt = Vector2.zero;
                localpt.y += Screen.height;
                regButtons[i].transform.localPosition = localpt;
            }
            else
            {
                Vector2 localpt = Vector2.zero;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(regpanel, screenpos, null, out localpt);
                regButtons[i].transform.localPosition = localpt;
            }
        }

        for (int i = 0; i < backupregButtons.Count; i++)
        {
            Vector3 worldpos = backupregWorldpos[i];
            Vector3 screenpos = cam.WorldToScreenPoint(worldpos);
            if (screenpos.z < 0)
            {
                Vector2 localpt = Vector2.zero;
                localpt.y += Screen.height;
                backupregButtons[i].transform.localPosition = localpt;
            }
            else
            {
                Vector2 localpt = Vector2.zero;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(backupregpanel, screenpos, null, out localpt);
                backupregButtons[i].transform.localPosition = localpt;
            }
        }

        for (int i = 0; i < backupedgeButtons.Count; i++)
        {
            Vector3 worldpos = backupedgeWorldpos[i];
            Vector3 screenpos = cam.WorldToScreenPoint(worldpos);
            if (screenpos.z < 0)
            {
                Vector2 localpt = Vector2.zero;
                localpt.y += Screen.height;
                backupedgeButtons[i].transform.localPosition = localpt;
            }
            else
            {
                Vector2 localpt = Vector2.zero;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(backupedgepanel, screenpos, null, out localpt);
                backupedgeButtons[i].transform.localPosition = localpt;
            }
        }
    }
}
