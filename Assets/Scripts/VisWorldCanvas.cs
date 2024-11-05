using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VisWorldCanvas : MonoBehaviour
{
    public Sprite regsprite;
    private List<GameObject> regButtons = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        if (VisGraph.Instance)
        {
            List<int> NodeRegCount = new List<int>(VisGraph.Instance.node_num);
            for (int i = 0; i < VisGraph.Instance.node_num; i++)
                NodeRegCount.Add(0);
            for (int d = 0; d < VisGraph.Instance.demand_num; d++)
            {
                foreach (var v in VisGraph.Instance.demands[d].reg)
                {
                    Transform nodetran = VisGraph.Instance.nodeobjs[v].transform;
                    NodeRegCount[v]++;
                }
            }

            int regIndex = 0;
            for (int v = 0; v < VisGraph.Instance.node_num; v++)
            {
                if (NodeRegCount[v] > 0)
                {
                    Vector3 pos = VisGraph.Instance.nodeobjs[v].transform.position;
                    pos.x -= 95.0f;
                    pos.y += 150.0f;
                    pos.z -= 10.0f;
                    GameObject goReg = new GameObject();
                    goReg.name = "Reg" + regIndex.ToString() + "_on_v" + v.ToString();
                    goReg.transform.parent = this.transform;
                    goReg.transform.localPosition = pos;
                    Image img = goReg.AddComponent<Image>();
                    img.sprite = regsprite;
                    Button b = goReg.AddComponent<Button>();
                    regButtons.Add(goReg);
                    b.onClick.AddListener(OnRegClick);
                }
                regIndex++;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        Camera cam = Camera.main;
        if (cam)
        {
            foreach (var regButton in regButtons)
            {
                regButton.transform.forward = cam.transform.position - regButton.transform.position;
            }
        }
    }

    void OnRegClick()
    {
        Debug.Log("OnRegClick");
    }

}
