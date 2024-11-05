using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Globalization;
using UnityEngine;
using Unity.Mathematics;

public struct Link
{
    public int u;
	public int v; // ���˵������˵�
	public int len;  // ���˵ĳ���
};

public struct Backup
{
	public int failed_edge;
	public int demandid;
	public int wavelength;
	public List<int> backuppath;
	public List<int> backupregs;
}

public struct Demand
{
	public int source;      // ҵ���Դ��
	public int target;      // ҵ����޵�
	public int wave;        // ����·����ռ�Ĳ���
	public bool[] installed; // ����Ƿ��ڹ���·���а�װ�м�
	public bool[] work_link; //	��·�Ƿ��ڹ���·����

	public List<int> reg;   // ����·����ʹ�õ��м�
	public List<int> epath; // ����·�����Աߴ��棩
	public List<int> vpath; // ����·�����Խ�㴢�棩

	public int src_dim;                                // Դ��ʹ�õı���ά��
	public int tar_dim;                                // �޵�ʹ�õı���ά��
	public Dictionary<int, int2> reg_dim; // �����м�ʹ�õ���������ά�ȣ�{v: (l1,l2)}

	public List<int> backups;
};

/// @brief ����ͼ��
public class Graph
{
	public int node_num; // �����
	public int link_num; // ����

	public int[,] min_links; // ��������У������ڵ����̵ıߣ��ڽӾ���, [v1][v2]

	public List<int>[,] MG; // �����ṹͼ���ڽӾ��󣩣����������ڵ�����еı�, [v1][v2]
	public Link[] links;      // ������·��, [e]
	public List<Vector2> nodes;

	/// @brief ���캯��
	/// @param _node_num �����
	/// @param _link_num ����
	public Graph(int _node_num, int _link_num)
	{
		node_num = _node_num;
		link_num = _link_num;

		MG = new List<int>[node_num,node_num];
		links = new Link[link_num];
		min_links = new int[node_num, node_num];

		for (int j = 0; j < node_num; j++)
		{
			for (int i = 0; i < node_num; i++)
			{
				MG[j,i] = new List<int>();
				min_links[j, i] = 0;
			}
		}
	}

	/// @brief ��ͼ�����һ����
	/// @param e �ߵ��±�
	/// @param u ���1
	/// @param v ���2
	/// @param len �ߵĳ���
	public void add_edge(int e, int u, int v, int len)
	{
		links[e].u = u;
		links[e].v = v;
		links[e].len = len;
		MG[u,v].Add(e);
		MG[v,u].Add(e);
	}

	double[] weight_len = null;
	double[] weight_node = null;
	KeyValuePair<List<int>, List<int>> test(int s, int t)
	{
		// ��㵽��������·��
		List<double> dist = new List<double>();
		for (int i = 0; i < node_num; i++)
			dist.Add(double.MaxValue);

		// �����ǰ׺���
		List<int> pre = new List<int>();
		for (int i = 0; i < node_num; i++)
			pre.Add(-1);

		List<bool> visited = new List<bool>();
		for (int i = 0; i < node_num; i++)
			visited.Add(false);

		if (weight_len == null)
		{ // Ĭ��ʹ����·����
			weight_len = new double[link_num];
			for (int e = 0; e<link_num; e++)
				weight_len[e] = links[e].len;
		}

		for (int j = 0; j < node_num; j++)
		{
			for (int i = 0; i < node_num; i++)
			{
				min_links[j, i] = -1;
			}
		}

		for (int u = 0; u < node_num; u++)
		{
			for (int v = 0; v < node_num; v++)
			{ 
				// �������ڵ����̵ı߹����ڽӾ���
				int pos = -1;
				double min = double.MaxValue;
				foreach(int e in MG[u,v])
				{
					if (min >= weight_len[e] && weight_len[e] < double.MaxValue)
					{
						pos = e;
						min = weight_len[e];
					}
				}
				min_links[u,v] = pos;
			}
		}
		

		dist[s] = 0; // ����dist��ʼ��Ϊ0
		pre[s] = s;  // ����ǰ����ʼ��Ϊ�Լ�
		for (int v = 0; v < node_num; v++)
		{
			if (min_links[s,v] != -1 && !visited[v])
			{   // �������̵��ڽӱ�ѡΪ·������һ���ڵ�
				dist[v] = weight_len[min_links[s,v]];
				if (weight_node != null)
					dist[v] *= weight_node[v];
				pre[v] = s;
			}
		}

		visited[s] = true;
		for (int i = 0; i < node_num - 1; i++)
		{
			double value = double.MaxValue ;
			int pos = -1;
			for (int v = 0; v < node_num; v++)
			{
				if (!visited[v] && value > dist[v])
				{   // �ҳ�δ���ʵĽڵ��е������̵Ľڵ�
					value = dist[v];
					pos = v;
				}
			}

			if (pos == -1)  // �����������ˣ�˵����ǰͼ����ͨ
				break;

			visited[pos] = true;    // ����ǰ����Ϊ�ѷ���
			for (int v = 0; v < node_num; v++)
			{
				if (min_links[pos,v] != -1 && !visited[v])
				{
					double w = weight_len[min_links[pos,v]];
					if (weight_node != null)
						w *= weight_node[v];
					if (value + w < dist[v])    // �õ��õ�����·������µ�����δ���ʵ����̾���
					{
						dist[v] = value + w;
						pre[v] = pos;
					}
				}
			}
		}

		// ���ݹ������·��
		List<int> vpath=new List<int>(); // ��·��
		List<int> epath=new List<int>(); // ��·��
		if (pre[t] != -1 && dist[t] < double.MaxValue)  // ���ҵ��˵��յ�����·
		{
			vpath.Add(t); // ���Ȳ����յ�
			while (vpath.Last() != s)
			{   // ÿ�β���ǰ�����ǰ����
				epath.Add(min_links[vpath.Last(),pre[vpath.Last()]]);
				vpath.Add(pre[vpath.Last()]);
			}

			vpath.Reverse();
			epath.Reverse();
			//reverse(vpath.begin(), vpath.end());    // ���·����ת
			//reverse(epath.begin(), epath.end());
		}

		return new KeyValuePair<List<int>, List<int>>(vpath, epath);
	}
};

public class StreamIn
{
	
	private StreamReader sr = null;

	public StreamIn(Stream stream)
	{
		sr = new StreamReader(stream);
	}

	public void Dispose()
    {
		sr.Dispose();
    }

	/// <summary>
	/// Reads a string token from the console
	/// skipping any leading and trailing whitespace.
	/// </summary>
	public string NextToken()
	{
		StringBuilder tokenChars = new StringBuilder();
		bool tokenFinished = false;
		bool skipWhiteSpaceMode = true;
		while (!tokenFinished)
		{
			int nextChar = sr.Read();
			if (nextChar == -1)
			{
				// End of stream reached
				tokenFinished = true;
			}
			else
			{
				char ch = (char)nextChar;
				if (char.IsWhiteSpace(ch))
				{
					// Whitespace reached (' ', '\r', '\n', '\t') -->
					// skip it if it is a leading whitespace
					// or stop reading anymore if it is trailing
					if (!skipWhiteSpaceMode)
					{
						tokenFinished = true;
						if (ch == '\r' && (Environment.NewLine == "\r\n"))
						{
							// Reached '\r' in Windows --> skip the next '\n'
							Console.Read();
						}
					}
				}
				else
				{
					// Character reached --> append it to the output
					skipWhiteSpaceMode = false;
					tokenChars.Append(ch);
				}
			}
		}

		string token = tokenChars.ToString();
		return token;
	}

	/// <summary>
	/// Reads an integer number from the console
	/// skipping any leading and trailing whitespace.
	/// </summary>
	public int NextInt()
	{
		string token = NextToken();
		return int.Parse(token);
	}

	/// <summary>
	/// Reads a floating-point number from the console
	/// skipping any leading and trailing whitespace.
	/// </summary>
	/// <param name="acceptAnyDecimalSeparator">
	/// Specifies whether to accept any decimal separator
	/// ("." and ",") or the system's default separator only.
	/// </param>
	public double NextDouble(bool acceptAnyDecimalSeparator = true)
	{
		string token = NextToken();
		if (acceptAnyDecimalSeparator)
		{
			token = token.Replace(',', '.');
			double result = double.Parse(token, CultureInfo.InvariantCulture);
			return result;
		}
		else
		{
			double result = double.Parse(token);
			return result;
		}
	}

	/// <summary>
	/// Reads a decimal number from the console
	/// skipping any leading and trailing whitespace.
	/// </summary>
	/// <param name="acceptAnyDecimalSeparator">
	/// Specifies whether to accept any decimal separator
	/// ("." and ",") or the system's default separator only.
	/// </param>
	public decimal NextDecimal(bool acceptAnyDecimalSeparator = true)
	{
		string token = NextToken();
		if (acceptAnyDecimalSeparator)
		{
			token = token.Replace(',', '.');
			decimal result = decimal.Parse(token, CultureInfo.InvariantCulture);
			return result;
		}
		else
		{
			decimal result = decimal.Parse(token);
			return result;
		}
	}
}

public class VisGraph : MonoBehaviour
{

	public int node_num;      // վ����
	public int link_num;      // ������
	public int demand_num;    // ҵ����
	public int wave_num;      // ������
	public int optical_reach; // ��οɴ����
	public int dimension_num; // ����ά������

	public Graph graph;             // ����ͼ
	public Demand[] demands;          // ҵ��
	public List<Backup> backups;         //����
	public List<int> fail_edges;
	public List<int>[,] break_down; // �ܹ���Ӱ���ҵ�񼯺ϣ�[e1][e2]
	public bool[,] link_avl_wave;     // ����·�п�ʹ�õĲ�����δ������·��ռ�ã�, [e][w]
	public int[] wave_remain;         // ����·�л�ʣ�µĿɷ��䲨������, [e]

	public List<List<int>> path_upper;  // ��ҵ����м̵㰲װ����, [d]
	public List<List<int>> epath_lower; // ��ҵ��������ߣ���·����, [d]
	public int[,,] sce_lower_id;              // ÿ�����ϳ���ʹ�õ��м̰�װid, [e1][e2][d]
	public int[,,] sce_lower_wave;            // ÿ�����ϳ�����ҵ��ʹ�õĲ���, [e1][e2][d]
	public int[,,] sce_upper_id;              // ÿ�����ϳ���ʹ�õ��м̰�װid, [e1][e2][d]

	public bool[,,] node_avl_wave; // ���ڵ㱾��ά���п�ʹ�õĲ�����δ������·��ռ�ã�, [v][l][w]
	public int[,] node_wave_cnt;   // ���ڵ�̶�ռ�õĲ�����,  [v][w]

	public List<int2> reg_dimension; // ÿ���ڵ��ϵ��м̰�װ�ı���ά��
	public int[,,,] sce_regs_id;                   // ÿ�����ϳ�����ҵ����ʹ�õ��м�id, [e1][e2][d][r]

	public static VisGraph Instance = null;

    private void OnEnable()
    {
		Instance = this;
	}

    private void OnDisable()
    {
		Instance = null;
    }

	// Start is called before the first frame update
	void Awake()
	{
		//{
		//	TextAsset ta = Resources.Load<TextAsset>("40NODE_dim");
		//	MemoryStream ms = new MemoryStream(ta.bytes);
		//	read_file_dim(ms);
		//	ms.Close();
		//}

		//{
		//	TextAsset ta = Resources.Load<TextAsset>("40NODE");
		//	MemoryStream ms = new MemoryStream(ta.bytes);
		//	readNodeInfoFromExcel(ms);
		//	ms.Close();
		//}

		{
			TextAsset ta = Resources.Load<TextAsset>("RWA_instance_20");
			MemoryStream ms = new MemoryStream(ta.bytes);
			readAllInfoFromExcel(ms);
			ms.Close();
		}

		Visual();
	}

	void Start()
	{
		ShowAllDemands();
	}

	private Vector2 textureoffset = Vector2.zero;
    // Update is called once per frame
    void Update()
    {
		textureoffset.x -= Time.deltaTime * 0.5f;
		//textureoffset.y += Time.deltaTime * 0.5f;

		for (int d=0;d<demand_num;d++)
        {
			Vector2 scale = demandRenders[d].material.GetTextureScale("_MainTex");
			demandRenders[d].material.SetTextureOffset("_MainTex", textureoffset * scale);
		}

		PassedLength += Time.deltaTime * 75.0f;
	}

	//const int nSample = 100;
	const int n = 4;
	public class PathSegment
	{
		public Vector3[] Q;
		public Vector3[] Pts;
		public float[] Us;
		public float Length;
		public float StartLocation;
		public float EndLocation;
		public int nSample = 100;

		public PathSegment(int _nSample)
		{
			nSample = Math.Min(Math.Max(_nSample,4),1000);
			Q = new Vector3[4];
			Pts = new Vector3[nSample + 1];
			Us = new float[nSample + 1];
		}
	};

	private static Vector3 CatmullRomPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
	{
		return p1 + (0.5f * (p2 - p0) * t) + 0.5f * (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
				0.5f * (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t;
	}
	private Vector3 GetPositionFromPathByParameter(float srcu, int i)
	{
		float u = srcu;
		Vector3 result = CatmullRomPoint(Segments[i].Q[0], Segments[i].Q[1], Segments[i].Q[2], Segments[i].Q[3], u);
		//Vector3 result = Vector3.Lerp(Segments[i].Q[1], Segments[i].Q[2],u);
		return result;
	}
	public Vector3 GetPositionFromPathByChrod(float u, int i)
	{
		float fValue = (u * Segments[i].nSample);
		float fFloorValue = Mathf.Floor(fValue);
		int nIndexA = (int)fFloorValue;
		int nIndexB = nIndexA + 1;
		if (nIndexB > Segments[i].nSample)
			nIndexB = Segments[i].nSample;
		float fPercentage = fValue - fFloorValue;
		Vector3 v1 = Segments[i].Pts[nIndexA];
		Vector3 v2 = Segments[i].Pts[nIndexB];
		Vector3 v = v1 * (1.0f - fPercentage) + v2 * fPercentage;
		return v;
	}

	public Vector3 GetPositionFromPathByLocation(float location, float u)
	{
		if (location + u < 0.0f)
			location = 0.0f - u;
		if (location + u > LengthOfCurve)
			location = LengthOfCurve - u;
		float fCount = 0.0f;
		for (int i = 0; i < Segments.Count; i++)
		{
			if (location + u >= fCount && location + u < fCount + Segments[i].Length)
			{
				return GetPositionFromPathByChrod((location + u - fCount) / Segments[i].Length, i);
			}
			fCount += Segments[i].Length;
		}
		return GetPositionFromPathByChrod(0.9999f, Segments.Count - 1);
	}

	public Vector3 GetCurrentPosition()
	{
		Vector3 vFishPos = GetPositionFromPathByLocation(PassedLength, 0.0f);
		return vFishPos;
	}

	public void CopmputePathLength()
    {
		LengthOfCurve = 0.0f;
		for (int i = 0; i < Segments.Count; i++)
		{
			Segments[i].StartLocation = LengthOfCurve;
			Segments[i].EndLocation = LengthOfCurve + Segments[i].Length;
			LengthOfCurve += Segments[i].Length;
		}
	}

	public struct WaveInfo
	{
		public Color col;
	};

	public List<GameObject> nodeobjs = new List<GameObject>();
	public List<GameObject> linkobjs = new List<GameObject>();
	private List<PathSegment> Segments = new List<PathSegment>();
	public List<LineRenderer> demandRenders = new List<LineRenderer>();
	public List<PathSegment[]> demandPaths = new List<PathSegment[]>();
	public List<LineRenderer> backupRenders = new List<LineRenderer>();
	public List<PathSegment[]> backupPaths = new List<PathSegment[]>();
	public List<WaveInfo> waveInfos = new List<WaveInfo>();
	public float LengthOfCurve = 0.0f;
	public float PassedLength = 0.0f;

	public GameObject nodeprefab = null;

	private Coroutine CurrentShowAllDemandCoroutine = null;
	public Material matTemplateSelfIllumin = null;
	public Material matTemplateTransparent = null;
	private Material matSelfIllumin = null;
	private Material matTransparent = null;
	private Material matSolid = null;

	private void SetSharedMaterialForGameObject(GameObject go, Material newsharedmaterial)
    {
        Renderer[] mrs = go.GetComponentsInChildren<Renderer>(true);
        if (mrs != null)
        {
            foreach (var mr in mrs)
            {
                if (mr.sharedMaterials != null)
                {
                    Material[] sharedmats = new Material[mr.sharedMaterials.Length];
                    for (int k = 0; k < sharedmats.Length; k++)
                        sharedmats[k] = newsharedmaterial;
                    mr.sharedMaterials = sharedmats;
                }
                else
                {
                    mr.sharedMaterial = newsharedmaterial;
                }
            }
        }
    }

	private void SetMaterialForGameObject(GameObject go, Material newmaterial)
	{
		Renderer[] mrs = go.GetComponentsInChildren<Renderer>(true);
		if (mrs != null)
		{
			foreach (var mr in mrs)
			{
				if (mr.materials != null)
				{
					Material[] mats = new Material[mr.materials.Length];
					for (int k = 0; k < mats.Length; k++)
						mats[k] = newmaterial;
					mr.materials = mats;
				}
				else
				{
					mr.material = newmaterial;
				}
			}
		}
	}


	/// <summary>
	/// ����תRGBɫ
	/// </summary>
	/// <param name="lambda"></param>
	/// <returns></returns>
	public static Color WaveLengthToRGB(int lambda)
	{
		float r = 0;
		float g = 0;
		float b = 0;
		if ((lambda >= 380.0) && (lambda < 440.0))
		{
			r = -1.0f * (lambda - 440.0f) / (440.0f - 380.0f);
			g = 0.0f;
			b = 1.0f;
		}

		else if ((lambda >= 440.0) && (lambda < 490.0))
		{
			r = 0.0f;
			g = (lambda - 440.0f) / (490.0f - 440.0f);
			b = 1.0f;
		}
		else if ((lambda >= 490.0) && (lambda < 510.0))
		{

			r = 0.0f;
			g = 1.0f;
			b = -1.0f * (lambda - 510.0f) / (510.0f - 490.0f);
		}
		else if ((lambda >= 510.0) && (lambda < 580.0))
		{
			r = (lambda - 510.0f) / (580.0f - 510.0f);
			g = 1.0f;
			b = 0.0f;
		}

		else if ((lambda >= 580.0) && (lambda < 645.0))
		{
			r = 1.0f;
			g = -1.0f * (lambda - 645.0f) / (645.0f - 580.0f);
			b = 0.0f;
		}
		else if ((lambda >= 645.0) && (lambda <= 780.0))
		{

			r = 1.0f;
			g = 0.0f;
			b = 0.0f;
		}
		if ((lambda >= 380.0) && (lambda < 420.0))
		{
			float attenuation = 0.3f + 0.7f * (lambda - 380) / (420 - 380);
			r = r * attenuation;
			g = 0.0f;
			b = 1.0f * attenuation;
		}
		else if ((lambda >= 701.0) && (lambda < 780.0))
		{
			float attenuation = 0.30f + 0.70f * (780.0f - lambda) / (780.0f - 700.0f);
			r = r * attenuation;
			g = 0.0f;
			b = 0.0f;
		}
		if (lambda < 380)
		{//����
			r = 1;
			g = 1;
			b = 1;
		}
		else if (lambda > 780)
		{//����
			r = 1;
			g = 0;
			b = 0;
		}
		return new Color(r, g, b);//��ɫ
	}


	public void Visual()
    {
		waveInfos.Clear();
		for(int i=0;i<wave_num;i++)
        {
			WaveInfo wi = new WaveInfo();
			//Color col = Color.white;
			//col.r = UnityEngine.Random.Range(0.2f, 0.8f);
			//col.g = UnityEngine.Random.Range(0.2f, 0.8f);
			//col.b = UnityEngine.Random.Range(0.2f, 0.8f);
			Color col = WaveLengthToRGB(400 + i * 30);
			wi.col = col;
			waveInfos.Add(wi);
        }

		if (matSelfIllumin != null)
			matSelfIllumin = new Material(matTemplateSelfIllumin);
		if (matTemplateTransparent != null)
			matTransparent = new Material(matTemplateTransparent);

		if (matSelfIllumin == null)
		{
			Shader s = Shader.Find("Legacy Shaders/Self-Illumin/Diffuse");
			matSelfIllumin = new Material(s);
		}
		if (matTransparent == null)
		{
			Shader s = Shader.Find("Legacy Shaders/Transparent/Diffuse");
			matTransparent = new Material(s);
		}
		if (matSolid == null)
		{
			Shader s = Shader.Find("Legacy Shaders/Diffuse");
			matSolid = new Material(s);
			matSolid.color = new Color(0.7f,0.7f,0.7f,1.0f);
		}

		Vector2 boundmin = Vector2.positiveInfinity;
		Vector2 boundmax = Vector2.negativeInfinity;
		for (int i = 0; i < node_num; i++)
		{
			Vector2 pos = graph.nodes[i];
			if (pos.x > boundmax.x)
				boundmax.x = pos.x;
			if (pos.y > boundmax.y)
				boundmax.y = pos.y;
			if (pos.x < boundmin.x)
				boundmin.x = pos.x;
			if (pos.y < boundmin.y)
				boundmin.y = pos.y;
		}
		Vector2 boundcenter = (boundmax + boundmin) * 0.5f;
		Vector2 boundsize = boundmax - boundmin;

		
		GameObject nodegroup = new GameObject("nodegroup");
		nodegroup.transform.parent = transform;
		nodegroup.transform.position = Vector3.zero;
		for(int i=0;i<node_num;i++)
        {
			GameObject go = null;
			if (nodeprefab != null)
				go = GameObject.Instantiate(nodeprefab);
			else
				go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			go.name = "node:" + i.ToString();
			Vector3 pos = Vector3.zero;
			pos.x = (graph.nodes[i].x - boundcenter.x);
			pos.y = 0;
			pos.z = (graph.nodes[i].y - boundcenter.y);
			go.transform.parent = nodegroup.transform;
			go.transform.localScale = Vector3.one * 50.0f;
			go.transform.localPosition = pos;
			SetSharedMaterialForGameObject(go, matSolid);

			nodeobjs.Add(go);
		}

		
		GameObject linkgroup = new GameObject("linkgroup");
		linkgroup.transform.parent = transform;
		linkgroup.transform.position = Vector3.zero;
		//for (int e = 0; e < link_num; e++)
		//{
		//    int u = graph.links[e].u;
		//    int v = graph.links[e].v;
		//    Vector3 pos = (nodeobjs[u].transform.position + nodeobjs[v].transform.position) * 0.5f;
		//    Vector3 vec = nodeobjs[u].transform.position - nodeobjs[v].transform.position;
		//    Vector3 dir = vec.normalized;

		//    GameObject golink = new GameObject();
		//    golink.name = "link:" + e.ToString();
		//    golink.transform.parent = linkgroup.transform;
		//    golink.transform.localPosition = pos;
		//    golink.transform.forward = dir;
		//    golink.transform.localScale = Vector3.one;

		//    linkobjs.Add(golink);
		//}

		List<int> LinkUseCount = new List<int>(link_num);
		for(int i=0;i<link_num;i++)
			LinkUseCount.Add(0);

		List<int> NodeRegCount = new List<int>(node_num);
		for (int i = 0; i < node_num; i++)
			NodeRegCount.Add(0);

        GameObject demandgroup = new GameObject("demandgroup");
		demandgroup.transform.parent = transform;
		demandgroup.transform.position = Vector3.zero;
		for (int d=0;d<demand_num;d++)
        {
			int w = demands[d].wave;

            GameObject godemand = new GameObject();
			godemand.name = "demand:" + d.ToString();
			godemand.transform.parent = demandgroup.transform;
			godemand.transform.localPosition = Vector3.zero;
			godemand.transform.localEulerAngles = Vector3.zero;
			godemand.transform.localScale = Vector3.one;
			
			LineRenderer lr = godemand.AddComponent<LineRenderer>();
			lr.alignment = LineAlignment.View;
			Color col = waveInfos[w].col;
			lr.startColor = col;
			lr.endColor = col;
			lr.textureMode = LineTextureMode.RepeatPerSegment;
			lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			lr.receiveShadows = false;
			lr.allowOcclusionWhenDynamic = false;
			lr.alignment = LineAlignment.View;
			List<Vector3> pathControlPoints = new List<Vector3>();
			foreach(var v in demands[d].vpath)
			{
				Vector3 pos = nodeobjs[v].transform.localPosition;
				pos.y = 130.0f - (float)w * 0.2f;
				pathControlPoints.Add(pos);
			}

			Segments.Clear();
			Vector3 startdir = (pathControlPoints[0] - pathControlPoints[1]).normalized;
			Vector3 enddir = (pathControlPoints[pathControlPoints.Count-1] - pathControlPoints[pathControlPoints.Count - 2]).normalized;

			List<Vector3> newPathControlPoints = new List<Vector3>();
			newPathControlPoints.Add(pathControlPoints[0] + startdir * 2.0f);
			for (int i=0;i<pathControlPoints.Count;i++)
            {
				newPathControlPoints.Add(pathControlPoints[i]);
            }
			newPathControlPoints.Add(pathControlPoints[pathControlPoints.Count - 1] + enddir * 2.0f);

			pathControlPoints.Clear();
			pathControlPoints.AddRange(newPathControlPoints.ToArray());


			for (int i = 0; i < pathControlPoints.Count - 3; i++)
			{
				float len = Vector3.Distance(pathControlPoints[i + 1], pathControlPoints[i + 2]) /12.0f;
				Segments.Add(new PathSegment(Mathf.RoundToInt(len)));
			}

			for (int i = 0; i < Segments.Count; i++)
			{
				Segments[i].Q[0] = pathControlPoints[i + 0];
				Segments[i].Q[1] = pathControlPoints[i + 1];
				Segments[i].Q[2] = pathControlPoints[i + 2];
				Segments[i].Q[3] = pathControlPoints[i + 3];
				float fCurSegmentLen = Vector3.Distance(Segments[i].Q[2], Segments[i].Q[1]);
				if (fCurSegmentLen > 1.0f)
				{
					Vector3 vDir1 = Vector3.Normalize(Segments[i].Q[0] - Segments[i].Q[1]);
					Vector3 vDir2 = Vector3.Normalize(Segments[i].Q[3] - Segments[i].Q[2]);
					Segments[i].Q[0] = Segments[i].Q[1] + fCurSegmentLen * vDir1;
					Segments[i].Q[3] = Segments[i].Q[2] + fCurSegmentLen * vDir2;
				}
			}

			//����ÿ��·������
			for (int i = 0; i < Segments.Count; i++)
			{
				int nSample = Segments[i].nSample;
				float fSample = (float)nSample;
				float fInvSample = 1.0f / fSample;
				float fu = 0.0f;
				//�����ò������ּ����ܻ�������֤��ȷ�ȣ�ƫ��
				Segments[i].Length = 0.0f;
				float[] fChordsByU = new float[nSample + 1];
				Vector3[] vPositionsByU = new Vector3[nSample + 1];
				float[] fLens = new float[nSample + 1];
				fu = 0.0f;
				for (int k = 0; k < nSample; k++)
				{
					Vector3 v1 = GetPositionFromPathByParameter(fu, i);
					Vector3 v2 = GetPositionFromPathByParameter(fu + fInvSample, i);
					fLens[k] = Vector3.Distance(v2, v1);
					fChordsByU[k] = Segments[i].Length;
					vPositionsByU[k] = v1;
					Segments[i].Length += fLens[k];
					fu += fInvSample;
				}
				fChordsByU[nSample] = Segments[i].Length;
				vPositionsByU[nSample] = GetPositionFromPathByParameter(1.0f, i);
				Debug.Assert(Segments[i].Length >= 0.0f && Segments[i].Length <= 99999.0f);

				fu = 0.0f;
				float fCurChord = 0.0f;
				float fDeltaChord = Segments[i].Length / fSample;
				for (int j = 0, k = 0; k < nSample;)
				{
					if (fCurChord >= fChordsByU[j] && fCurChord <= fChordsByU[j + 1])
					{
						float fAlpha = 0.0f;
						if (fChordsByU[j + 1] - fChordsByU[j] > 0.0f)
							fAlpha = (fCurChord - fChordsByU[j]) / (fChordsByU[j + 1] - fChordsByU[j]);
						Segments[i].Pts[k] = vPositionsByU[j] * (1.0f - fAlpha) + vPositionsByU[j + 1] * fAlpha;
						float fu1 = (float)j / fSample;
						float fu2 = (float)(j + 1) / fSample;
						Segments[i].Us[k] = fu1 * (1.0f - fAlpha) + fu2 * fAlpha;
						k++;
						fCurChord += fDeltaChord;
					}
					else
					{
						j++;
					}
				}
				Segments[i].Pts[nSample] = vPositionsByU[nSample];
				Segments[i].Us[nSample] = 1.0f;

				float fOldSegmentLen = Segments[i].Length;
				Segments[i].Length = 0.0f;
				for (int k = 0; k < nSample; k++)
				{
					Vector3 v1 = Segments[i].Pts[k];
					Vector3 v2 = Segments[i].Pts[k + 1];
					float fLen = Vector3.Distance(v2, v1);
					fLens[k] = fLen;
					Segments[i].Length += fLen;
				}
				float fNewSegmentLegnth = Segments[i].Length;
				float fDeltaSegmentLength = Mathf.Abs(fNewSegmentLegnth - fOldSegmentLen);
				Debug.Assert(fDeltaSegmentLength < 100.0f);

			}

			//�����ܵ�·������
			CopmputePathLength();

			List<Vector3> curvePositions = new List<Vector3>();
			for (int i = 0; i < Segments.Count; i++)
			{
				int e = demands[d].epath[i];
				LinkUseCount[e]++;

				int nSample = Segments[i].nSample;
				float fSample = (float)nSample;
				float fInvSample = 1.0f / fSample;
				float fu = 0.0f;

				for (int k = 0; k < Segments[i].Pts.Length; k++)
				{
					Vector3 v1 = Vector3.Lerp(Segments[i].Q[1], Segments[i].Q[2], fu);
					Vector3 v2 = Vector3.Lerp(Segments[i].Q[1], Segments[i].Q[2], fu + fInvSample);
					Vector3 forward = (v2 - v1).normalized;
					Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
					float angle = fu * 360.0f * fSample / 8.0f + LinkUseCount[e] * 90.0f;
					float radius = 5;
					float xx = Mathf.Cos(angle) * radius;
					float yy = Mathf.Sin(angle) * radius;
					Vector3 v = v1 + xx * right + yy * Vector3.up;
					fu += fInvSample;

					if (!(i>=1 && k==0))
						curvePositions.Add(v);
				}
			}

            lr.positionCount = curvePositions.Count;
            lr.SetPositions(curvePositions.ToArray());
            lr.Simplify(1.0f);

			foreach(var v in demands[d].reg)
            {
				Transform nodetran = nodeobjs[v].transform;
				int index = NodeRegCount[v];

				GameObject goReg = GameObject.CreatePrimitive(PrimitiveType.Cube);
				
				goReg.name = "Reg" + godemand.transform.childCount.ToString()+"_on_v"+ v.ToString()+"_as_" +index.ToString();
				goReg.transform.parent = godemand.transform;
				goReg.transform.localScale = Vector3.one * 15.0f;
				float deltaangle = 30.0f;
				goReg.transform.localEulerAngles = new Vector3(0, index * deltaangle, 0);
				float xx = Mathf.Cos(index * deltaangle) * 10.0f - 95.0f;
				float zz = Mathf.Sin(index * deltaangle) * 10.0f - 10.0f;
				goReg.transform.localPosition = nodetran.localPosition + new Vector3(xx, 115 - NodeRegCount[v] * 3.0f, zz);
				NodeRegCount[v]++;
            }


			demandPaths.Add(Segments.ToArray());
			demandRenders.Add(lr);
		}


		GameObject backupgroup = new GameObject("backupgroup");
		backupgroup.transform.parent = transform;
		backupgroup.transform.position = Vector3.zero;
		for (int b = 0; b < backups.Count; b++)
		{
			int w = backups[b].wavelength;

			GameObject gobackup = new GameObject();
			gobackup.name = "backup:" + b.ToString();
			gobackup.transform.parent = backupgroup.transform;
			gobackup.transform.localPosition = Vector3.zero;
			gobackup.transform.localEulerAngles = Vector3.zero;
			gobackup.transform.localScale = Vector3.one;

			//{
			//	int v1 = graph.links[backups[b].failed_edge].u;
			//	int v2 = graph.links[backups[b].failed_edge].v;
			//	Vector3 pos1 = nodeobjs[v1].transform.localPosition;
			//	pos1.y = 130.0f - (float)w * 0.2f;
			//	Vector3 pos2 = nodeobjs[v2].transform.localPosition;
			//	pos2.y = 130.0f - (float)w * 0.2f;
			//}

			LineRenderer lr = gobackup.AddComponent<LineRenderer>();
			lr.alignment = LineAlignment.View;
			Color col = waveInfos[w].col;
			lr.startColor = col;
			lr.endColor = col;
			lr.textureMode = LineTextureMode.RepeatPerSegment;
			lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			lr.receiveShadows = false;
			lr.allowOcclusionWhenDynamic = false;
			lr.alignment = LineAlignment.View;
			List<Vector3> pathControlPoints = new List<Vector3>();
			foreach (var v in backups[b].backuppath)
			{
				Vector3 pos = nodeobjs[v].transform.localPosition;
				pos.y = 130.0f - (float)w * 0.2f;
				pathControlPoints.Add(pos);
			}

			Segments.Clear();
			Vector3 startdir = (pathControlPoints[0] - pathControlPoints[1]).normalized;
			Vector3 enddir = (pathControlPoints[pathControlPoints.Count - 1] - pathControlPoints[pathControlPoints.Count - 2]).normalized;

			List<Vector3> newPathControlPoints = new List<Vector3>();
			newPathControlPoints.Add(pathControlPoints[0] + startdir * 2.0f);
			for (int i = 0; i < pathControlPoints.Count; i++)
			{
				newPathControlPoints.Add(pathControlPoints[i]);
			}
			newPathControlPoints.Add(pathControlPoints[pathControlPoints.Count - 1] + enddir * 2.0f);

			pathControlPoints.Clear();
			pathControlPoints.AddRange(newPathControlPoints.ToArray());


			for (int i = 0; i < pathControlPoints.Count - 3; i++)
			{
				float len = Vector3.Distance(pathControlPoints[i + 1], pathControlPoints[i + 2]) / 12.0f;
				Segments.Add(new PathSegment(Mathf.RoundToInt(len)));
			}

			for (int i = 0; i < Segments.Count; i++)
			{
				Segments[i].Q[0] = pathControlPoints[i + 0];
				Segments[i].Q[1] = pathControlPoints[i + 1];
				Segments[i].Q[2] = pathControlPoints[i + 2];
				Segments[i].Q[3] = pathControlPoints[i + 3];
				float fCurSegmentLen = Vector3.Distance(Segments[i].Q[2], Segments[i].Q[1]);
				if (fCurSegmentLen > 1.0f)
				{
					Vector3 vDir1 = Vector3.Normalize(Segments[i].Q[0] - Segments[i].Q[1]);
					Vector3 vDir2 = Vector3.Normalize(Segments[i].Q[3] - Segments[i].Q[2]);
					Segments[i].Q[0] = Segments[i].Q[1] + fCurSegmentLen * vDir1;
					Segments[i].Q[3] = Segments[i].Q[2] + fCurSegmentLen * vDir2;
				}
			}

			//����ÿ��·������
			for (int i = 0; i < Segments.Count; i++)
			{
				int nSample = Segments[i].nSample;
				float fSample = (float)nSample;
				float fInvSample = 1.0f / fSample;
				float fu = 0.0f;
				//�����ò������ּ����ܻ�������֤��ȷ�ȣ�ƫ��
				Segments[i].Length = 0.0f;
				float[] fChordsByU = new float[nSample + 1];
				Vector3[] vPositionsByU = new Vector3[nSample + 1];
				float[] fLens = new float[nSample + 1];
				fu = 0.0f;
				for (int k = 0; k < nSample; k++)
				{
					Vector3 v1 = GetPositionFromPathByParameter(fu, i);
					Vector3 v2 = GetPositionFromPathByParameter(fu + fInvSample, i);
					fLens[k] = Vector3.Distance(v2, v1);
					fChordsByU[k] = Segments[i].Length;
					vPositionsByU[k] = v1;
					Segments[i].Length += fLens[k];
					fu += fInvSample;
				}
				fChordsByU[nSample] = Segments[i].Length;
				vPositionsByU[nSample] = GetPositionFromPathByParameter(1.0f, i);
				Debug.Assert(Segments[i].Length >= 0.0f && Segments[i].Length <= 99999.0f);

				fu = 0.0f;
				float fCurChord = 0.0f;
				float fDeltaChord = Segments[i].Length / fSample;
				for (int j = 0, k = 0; k < nSample;)
				{
					if (fCurChord >= fChordsByU[j] && fCurChord <= fChordsByU[j + 1])
					{
						float fAlpha = 0.0f;
						if (fChordsByU[j + 1] - fChordsByU[j] > 0.0f)
							fAlpha = (fCurChord - fChordsByU[j]) / (fChordsByU[j + 1] - fChordsByU[j]);
						Segments[i].Pts[k] = vPositionsByU[j] * (1.0f - fAlpha) + vPositionsByU[j + 1] * fAlpha;
						float fu1 = (float)j / fSample;
						float fu2 = (float)(j + 1) / fSample;
						Segments[i].Us[k] = fu1 * (1.0f - fAlpha) + fu2 * fAlpha;
						k++;
						fCurChord += fDeltaChord;
					}
					else
					{
						j++;
					}
				}
				Segments[i].Pts[nSample] = vPositionsByU[nSample];
				Segments[i].Us[nSample] = 1.0f;

				float fOldSegmentLen = Segments[i].Length;
				Segments[i].Length = 0.0f;
				for (int k = 0; k < nSample; k++)
				{
					Vector3 v1 = Segments[i].Pts[k];
					Vector3 v2 = Segments[i].Pts[k + 1];
					float fLen = Vector3.Distance(v2, v1);
					fLens[k] = fLen;
					Segments[i].Length += fLen;
				}
				float fNewSegmentLegnth = Segments[i].Length;
				float fDeltaSegmentLength = Mathf.Abs(fNewSegmentLegnth - fOldSegmentLen);
				Debug.Assert(fDeltaSegmentLength < 100.0f);

			}

			//�����ܵ�·������
			CopmputePathLength();

			List<Vector3> curvePositions = new List<Vector3>();
			for (int i = 0; i < Segments.Count; i++)
			{
				//int e = backups[b].epath[i];
				//LinkUseCount[e]++;

				int nSample = Segments[i].nSample;
				float fSample = (float)nSample;
				float fInvSample = 1.0f / fSample;
				float fu = 0.0f;

				for (int k = 0; k < Segments[i].Pts.Length; k++)
				{
					Vector3 v1 = Vector3.Lerp(Segments[i].Q[1], Segments[i].Q[2], fu);
					Vector3 v2 = Vector3.Lerp(Segments[i].Q[1], Segments[i].Q[2], fu + fInvSample);
					Vector3 forward = (v2 - v1).normalized;
					Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
					float angle = fu * 360.0f * fSample / 8.0f + 0 * 90.0f;
					//float angle = fu * 360.0f * fSample / 8.0f + LinkUseCount[e] * 90.0f;
					float radius = 5;
					float xx = Mathf.Cos(angle) * radius;
					float yy = Mathf.Sin(angle) * radius;
					Vector3 v = v1 + xx * right + yy * Vector3.up;
					fu += fInvSample;

					if (!(i >= 1 && k == 0))
						curvePositions.Add(v);
				}
			}

			lr.positionCount = curvePositions.Count;
			lr.SetPositions(curvePositions.ToArray());
			lr.Simplify(1.0f);
			lr.enabled = false;

			backupPaths.Add(Segments.ToArray());
			backupRenders.Add(lr);
		}


		transform.localScale = Vector3.one;
		transform.position = new Vector3(100, 10, 10);

    }

	public void HighlightAllDemand(bool vis)
	{
		for (int d = 0; d < demand_num; d++)
			HighlightDemand(d, vis);
	}

	public void HighlightDemand(int index, bool vis)
    {
		var element = demandRenders[index].gameObject.GetComponent<UnityEngine.Rendering.PostProcessing.GlowOutlineElement>();
		if (vis)
		{
			Color c = demandRenders[index].startColor;
			if (!element)
				element = demandRenders[index].gameObject.AddComponent<UnityEngine.Rendering.PostProcessing.GlowOutlineElement>();
			if (element)
				element.color = c;
		}
		else
        {
			if (element)
				DestroyImmediate(element);
		}
	}

	public void HighlightAllBackup(bool vis)
	{
		for (int b = 0; b < backups.Count; b++)
			HighlightBackup(b, vis);
	}

	public void HighlightBackup(int index, bool vis)
	{
		backupRenders[index].enabled = vis;
		var element = backupRenders[index].gameObject.GetComponent<UnityEngine.Rendering.PostProcessing.GlowOutlineElement>();
		if (vis)
		{
			Color c = backupRenders[index].startColor;
			if (!element)
				element = backupRenders[index].gameObject.AddComponent<UnityEngine.Rendering.PostProcessing.GlowOutlineElement>();
			if (element)
				element.color = c;
		}
		else
		{
			if (element)
				DestroyImmediate(element);
		}
	}

	[System.NonSerialized]
	public int CurrentDemand = -1;

	public void ShowDemand(int index)
	{
		CurrentDemand = index;

		Segments.Clear();
		Segments.AddRange(demandPaths[index]);
		CopmputePathLength();
		PassedLength = 0.0f;

        foreach (var v in demands[index].vpath)
        {
			SetSharedMaterialForGameObject(nodeobjs[v], matSolid);
		}

        {
			Color c = demandRenders[index].startColor;
			c.a = 1.0f;
			Material newmat = new Material(matSelfIllumin);
			newmat.color = c;
			demandRenders[index].material = newmat;
			demandRenders[index].startWidth = 10;
			demandRenders[index].endWidth = 10;
			demandRenders[index].startColor = c;
			demandRenders[index].endColor = c;

			for (int i = 0; i < demandRenders[index].transform.childCount; i++)
			{
				var child = demandRenders[index].transform.GetChild(i);
				SetSharedMaterialForGameObject(child.gameObject, newmat);
			}
		}

		HighlightDemand(index, true);

	}

	public void HideDemand(int index)
	{
		HighlightDemand(index, false);

		foreach (var v in demands[index].vpath)
        {
			SetSharedMaterialForGameObject(nodeobjs[v], matTransparent);
		}

        {
			Color c = demandRenders[index].startColor;
			c.a = 0.1f;
			Material newmat = new Material(matTransparent);
			newmat.color = c;
			demandRenders[index].material = newmat;
			demandRenders[index].startWidth = 4.5f;
			demandRenders[index].endWidth = 4.5f;
			demandRenders[index].startColor = c;
			demandRenders[index].endColor = c;

			for (int i = 0; i < demandRenders[index].transform.childCount; i++)
			{
				var child = demandRenders[index].transform.GetChild(i);
				SetSharedMaterialForGameObject(child.gameObject, newmat);
			}
		}

		CurrentDemand = -1;
	}

	public void SwitchBackupVisbility(int index, bool vis)
    {
		backupRenders[index].enabled = vis;
    }


	public void ShowAllDemands()
	{
		CurrentDemand = -1;

		Segments.Clear();
		Segments.AddRange(demandPaths[0]);
		CopmputePathLength();
		PassedLength = 0.0f;

        for (int v = 0; v < node_num; v++)
        {
			SetSharedMaterialForGameObject(nodeobjs[v], matSolid);
        }

        for (int index=0;index<demand_num;index++)
        {
			Color c = demandRenders[index].startColor;
			c.a = 1.0f;
			Material newmat = new Material(matSelfIllumin);
			newmat.color = c;
			demandRenders[index].material = newmat;
			demandRenders[index].startWidth = 8;
			demandRenders[index].endWidth = 8;
			demandRenders[index].startColor = c;
			demandRenders[index].endColor = c;

			for (int i = 0; i < demandRenders[index].transform.childCount; i++)
			{
				var child = demandRenders[index].transform.GetChild(i);
				SetSharedMaterialForGameObject(child.gameObject, newmat);
			}
		}
	}

	public void HideAllDemands()
	{
		CurrentDemand = -1;

        for (int v = 0; v < node_num; v++)
        {
			SetSharedMaterialForGameObject(nodeobjs[v], matTransparent);
		}

        for (int index = 0; index < demand_num; index++)
		{
			HighlightDemand(index, false);

			Color c = demandRenders[index].startColor;
			c.a = 0.1f;
			Material newmat = new Material(matTransparent);
			newmat.color = c;
			demandRenders[index].material = newmat;
			demandRenders[index].startWidth = 8;
			demandRenders[index].endWidth = 8;
			demandRenders[index].startColor = c;
			demandRenders[index].endColor = c;

			for (int i = 0; i < demandRenders[index].transform.childCount; i++)
			{
				var child = demandRenders[index].transform.GetChild(i);
				SetSharedMaterialForGameObject(child.gameObject, newmat);
			}
		}
	}

	public void StopAllDemandEnumerator()
    {
		if (CurrentShowAllDemandCoroutine!=null)
			StopCoroutine(CurrentShowAllDemandCoroutine);
    }

	IEnumerator ShowAllDemandEnumerator()
    {
        for (int i = 0; i < demand_num; i++)
        {
			ShowDemand(i);

			yield return new WaitForSeconds(10.0f);

			HideDemand(i);
		}

        yield return 0;
    }

	public bool IGNORE_LOCAL_DIMENSION = false;


	void readNodeInfoFromExcel(Stream stream)
	{
		graph.nodes = new List<Vector2>();
        var excelReader = MiniExcelLibs.ExcelReaderFactory.GetProvider(stream, MiniExcelLibs.Utils.ExcelTypeHelper.GetExcelType(stream, MiniExcelLibs.ExcelType.XLSX), null);
        var table = excelReader.Query(false, "node", "A2");
        foreach (var item in table)
        {
            Vector2 pos = Vector2.zero;
            object strx = string.Empty;
            item.TryGetValue("B", out strx);
            object stry = string.Empty;
            item.TryGetValue("C", out stry);
            float.TryParse(strx.ToString(), out pos.x);
            float.TryParse(stry.ToString(), out pos.y);
            graph.nodes.Add(pos);
        }
        excelReader.Dispose();

    }

	public struct DemandInExcel
	{
		public int wavelength;
		public List<int> workingpath;
		public int regenerator;
	}



	void readAllInfoFromExcel(Stream stream)
	{
		var excelReader = MiniExcelLibs.ExcelReaderFactory.GetProvider(stream, MiniExcelLibs.Utils.ExcelTypeHelper.GetExcelType(stream, MiniExcelLibs.ExcelType.XLSX), null);

		int reg_num = 0;
		List<int> regs = new List<int>();
		List<Vector2> vertices = new List<Vector2>();
		{
			var table = excelReader.Query(false, "vertices", "A2");
			int v = 0;
			foreach (var item in table)
			{
				Vector2 pos = Vector2.zero;
				int solution = 0;
				object strx = string.Empty;
				item.TryGetValue("B", out strx);
				object stry = string.Empty;
				item.TryGetValue("C", out stry);
				object strsolution = string.Empty;
				item.TryGetValue("D", out strsolution);
				if (strx == null || stry == null)
					break;
				float.TryParse(strx.ToString(), out pos.x);
				float.TryParse(stry.ToString(), out pos.y);
				pos.x *= 70;
				pos.y = 95 - pos.y;
				pos.y *= 50;
				int.TryParse(strsolution.ToString(), out solution);
				for(int s=0;s<solution;s++)
                {
					regs.Add(v);
					reg_num++;
				}
				vertices.Add(pos);
				v++;
			}
		}

		List<int3> edges = new List<int3>();
		{
			var table = excelReader.Query(false, "edges", "A2");
			foreach (var item in table)
			{
				int3 edge = int3.zero;
				object strsrc = string.Empty;
				item.TryGetValue("B", out strsrc);
				object strdest = string.Empty;
				item.TryGetValue("C", out strdest);
				object strweight = string.Empty;
				item.TryGetValue("D", out strweight);
				if (strsrc == null || strdest == null)
					break;
				int.TryParse(strsrc.ToString(), out edge.x);
				int.TryParse(strdest.ToString(), out edge.y);
				int.TryParse(strweight.ToString(), out edge.z);
				edges.Add(edge);
			}
		}

		List<DemandInExcel> demandinexcels = new List<DemandInExcel>();
		{
			var table = excelReader.Query(false, "demands", "A2");
			foreach (var item in table)
			{
				DemandInExcel demand = new DemandInExcel();
				demand.workingpath = new List<int>();
				demand.regenerator = -1;
				object strwavelength = string.Empty;
				item.TryGetValue("B", out strwavelength);
				object strworkingpath = string.Empty;
				item.TryGetValue("C", out strworkingpath);
				object strregenerator = string.Empty;
				item.TryGetValue("D", out strregenerator);
				int.TryParse(strwavelength.ToString(), out demand.wavelength);
				if (strwavelength == null || strworkingpath == null)
					break;
				string[] path = ((string)strworkingpath).Split(' ');
				if (path!=null)
                {
					foreach(string s in path)
                    {
						int e = -1;
						bool ret = int.TryParse(s, out e);
						if (ret && e>=0)
							demand.workingpath.Add(e);
					}
                }
				if (strregenerator != null)
					int.TryParse(strregenerator.ToString(), out demand.regenerator);
				demandinexcels.Add(demand);
			}
		}

		backups = new List<Backup>();
		{
			var table = excelReader.Query(false, "backups", "A2");
			foreach (var item in table)
			{
				Backup backup = new Backup();
				backup.backuppath = new List<int>();
				object strfailededge = string.Empty;
				item.TryGetValue("A", out strfailededge);
				object strdemandid = string.Empty;
				item.TryGetValue("B", out strdemandid);
				object strwavelength = string.Empty;
				item.TryGetValue("C", out strwavelength);
				object strbackuppath = string.Empty;
				item.TryGetValue("D", out strbackuppath);
				object strbackupreg = string.Empty;
				item.TryGetValue("E", out strbackupreg);
				int.TryParse(strfailededge.ToString(), out backup.failed_edge);
				int.TryParse(strdemandid.ToString(), out backup.demandid);
				int.TryParse(strwavelength.ToString(), out backup.wavelength);
				if (strfailededge == null || strdemandid == null || strwavelength == null || strbackuppath == null)
					break;
				string[] path = (strbackuppath.ToString()).Split(' ');
				if (path != null)
				{
					foreach (string s in path)
					{
						int e = -1;
						bool ret = int.TryParse(s, out e);
						if (ret && e>=0)
							backup.backuppath.Add(e);
					}
				}
				if (strbackupreg != null)
				{
					string[] backupregs = (strbackupreg.ToString()).Split(' ');
					if (backupregs != null)
					{
						backup.backupregs = new List<int>();
						foreach (string s in backupregs)
						{
							int reg = -1;
							bool ret = int.TryParse(s, out reg);
							if (ret && reg >= 0)
								backup.backupregs.Add(reg);
						}
					}
				}
				backups.Add(backup);
			}
		}

		fail_edges = new List<int>();
		for (int b = 0; b<backups.Count;b++)
        {
			int e = backups[b].failed_edge;
			if (!fail_edges.Contains(e))
				fail_edges.Add(e);
		}

		node_num = vertices.Count;
		link_num = edges.Count;
		demand_num = demandinexcels.Count;
		{
			var table = excelReader.Query(false, "parameters", "A2");
			foreach (var item in table)
			{
				object strA = string.Empty;
				item.TryGetValue("A", out strA);
				object strB = string.Empty;
				item.TryGetValue("B", out strB);
				if (strA == null || strB == null)
					break;
				int.TryParse(strA.ToString(), out wave_num);
				int.TryParse(strB.ToString(), out optical_reach);
			}
		}
		//dimension_num = fin.NextInt();
		dimension_num = 6;

		excelReader.Dispose();

		graph = new Graph(node_num, link_num);
        demands = new Demand[demand_num];
        break_down = new List<int>[link_num, link_num];
        for (int j = 0; j < link_num; j++)
            for (int i = 0; i < link_num; i++)
                break_down[j, i] = new List<int>();

		graph.nodes = vertices;

		int[] max_reg = new int[node_num];

		// ��ȡ������Ϣ
		for (int e = 0; e < link_num; e++)
		{
			int u = edges[e].x, v = edges[e].y, len = edges[e].z;
			graph.add_edge(e, u, v, len);
		}

		// ��ȡҵ����Ϣ
		wave_remain = new int[link_num];
		for (int e = 0; e < link_num; e++)
		{
			wave_remain[e] = wave_num;
		}
		link_avl_wave = new bool[link_num, wave_num];
		for (int e = 0; e < link_num; e++)
		{
			for (int j = 0; j < wave_num; j++)
			{
				link_avl_wave[e, j] = true;
			}
		}

		if (!IGNORE_LOCAL_DIMENSION)
		{
			// ��ʼ������ά����ر���
			node_wave_cnt = new int[node_num, wave_num];
			node_avl_wave = new bool[node_num, dimension_num, wave_num];
			for (int v = 0; v < node_num; v++)
			{
				for (int j = 0; j < wave_num; j++)
					node_wave_cnt[v, j] = 0;
				for (int l = 0; l < dimension_num; l++)
				{
					for (int j = 0; j < wave_num; j++)
					{
						node_avl_wave[v, l, j] = true;
					}
				}
			}
		}

		for (int d = 0; d < demand_num; d++)
		{
			demands[d].reg = new List<int>();
			demands[d].epath = new List<int>();
			demands[d].vpath = new List<int>();
			demands[d].reg_dim = new Dictionary<int, int2>();
			demands[d].backups = new List<int>();

			// Դ�㡢�޵㡢����
			int s = demandinexcels[d].workingpath[0];
			int t = demandinexcels[d].workingpath[demandinexcels[d].workingpath.Count - 1];
			int w = demandinexcels[d].wavelength;
			demands[d].source = s;
			demands[d].target = t;
			demands[d].wave = w;

            //{
            //    int ls = fin.NextInt(), lt = fin.NextInt(); // Դ�㡢�޵�ʹ�õı���ά��
            //                                                //fin >> ls >> lt;
            //    demands[d].src_dim = ls;
            //    demands[d].tar_dim = lt;

            //    if (!IGNORE_LOCAL_DIMENSION)
            //    { // ���ڵ��ڱ���ά���Ƿ�ʹ�ó�ͻ
            //        if (!node_avl_wave[s, ls, w])
            //            Debug.LogError("Error working source dimension");
            //        if (!node_avl_wave[t, lt, w])
            //            Debug.LogError("Error working target dimension");

            //        // Դ�㡢�޵��ڶ�Ӧ����ά�ȵĲ����޷�ʹ��
            //        node_avl_wave[s, ls, w] = false;
            //        node_avl_wave[t, lt, w] = false;
            //        // �ò���ʹ�ô���+1
            //        node_wave_cnt[s, w]++;
            //        node_wave_cnt[t, w]++;
            //    }
            //}
            {
				node_wave_cnt[s, w]++;
				node_wave_cnt[t, w]++;
			}

			// ��㹤��·��
			int v_num = demandinexcels[d].workingpath.Count;
			demands[d].vpath = demandinexcels[d].workingpath;

            // �߹���·��
            demands[d].work_link = new bool[link_num];
            for (int j = 0; j < link_num; j++)
                demands[d].work_link[j] = false;

			for(int k=0;k< demandinexcels[d].workingpath.Count-1;k++)
            {
				int vs = demandinexcels[d].workingpath[k];
				int vt = demandinexcels[d].workingpath[k+1];
				int e = -1;
				for(int ee=0;ee<edges.Count;ee++)
                {
					if (edges[ee].x==vs && edges[ee].y==vt)
					{
						e = ee;
						break;
					}
					else if (edges[ee].y == vs && edges[ee].x == vt)
					{
						e = ee;
						break;
					}
				}
				if (e!=-1 && e<edges.Count)
                {
					demands[d].epath.Add(e);
					demands[d].work_link[e] = true;
					link_avl_wave[e, demands[d].wave] = false; // ��·�Ĳ�����ռ����
					wave_remain[e]--;
				}
			}
			
            // �Ѱ�װ�м�
            demands[d].installed = new bool[node_num];
			for (int j = 0; j < node_num; j++)
				demands[d].installed[j] = false;

			int reg = demandinexcels[d].regenerator;
			if (reg>=0)
            {
				int v = reg;
                demands[d].reg.Add(v);
                demands[d].installed[v] = true;
                //demands[d].reg_dim[v] = new int2(ls, lt);

                if (!IGNORE_LOCAL_DIMENSION)
                { // ���ڵ��ڱ���ά���Ƿ�ʹ�ó�ͻ
                    //if (!node_avl_wave[v, ls, demands[d].wave])
                    //    Debug.LogError("Error working reg dimension");
                    //if (!node_avl_wave[v, lt, demands[d].wave])
                    //    Debug.LogError("Error working reg dimension");

                    //// �����м̵���������ά�ȵĲ����޷�ʹ��
                    //node_avl_wave[v, ls, demands[d].wave] = false;
                    //node_avl_wave[v, lt, demands[d].wave] = false;
                    // �ò���ʹ�ô���+2
                    node_wave_cnt[v, demands[d].wave] += 2;
                }
            }
		}

		for(int i=0;i< backups.Count;i++)
        {
			int d = backups[i].demandid;
			demands[d].backups.Add(i);
        }

		// ���㵱ǰͼ�Ķ������������һ����
		int cnt = 0;
		for (int i = 0; i < node_num; i++)
			for (int j = 0; j < node_num; j++)
				if (graph.MG[i, j].Count() > 0)
					cnt++;

		double val = (double)(cnt + node_num) / (node_num * node_num);
		Debug.Log("Density of graph = " + val.ToString());
		//printf("Density of graph = %.2f\n", (double)(cnt + node_num) / (node_num * node_num));

		// ������Ӱ������󼯺�
		List<int>[] failure = new List<int>[link_num]; // һ�ι���Ӱ�����󼯺�
		for (int i = 0; i < link_num; i++)
			failure[i] = new List<int>();
		for (int d = 0; d < demand_num; d++)
			foreach (var e in demands[d].epath) // ����·������һ�߹��ϼ����յ�Ӱ��
				failure[e].Add(d);
		// ������һ�ι���Ӱ���ҵ��ϲ���ȥ��
		for (int e1 = 0; e1 < link_num; e1++)
			for (int e2 = 0; e2 < e1 + 1; e2++)
			{
				HashSet<int> s = new HashSet<int>();
				//set<int> s;
				foreach (int i in failure[e1])
					s.Add(i);
				foreach (int i in failure[e2])
					s.Add(i);
				break_down[e1, e2].AddRange(s);
			}
		// ���ϳ����Գ��Ը���
		for (int e1 = 0; e1 < link_num; e1++)
			for (int e2 = e1 + 1; e2 < link_num; e2++)
				break_down[e1, e2] = break_down[e2, e1];


	}

	/// @brief ��ȡ�����ļ�����ʼ��
	/// @param file_path �ļ�·��
	void read_file_dim(Stream stream)
	{
		StreamIn fin = new StreamIn(stream);
		
		node_num = fin.NextInt();
		link_num = fin.NextInt();
		demand_num = fin.NextInt();
		wave_num = fin.NextInt();
		optical_reach = fin.NextInt();
		dimension_num = fin.NextInt();
		//fin >> node_num >> link_num >> demand_num >>
		//	wave_num >> optical_reach >> dimension_num;

		Debug.Log("Node num = " + node_num.ToString());
		Debug.Log("Link num = " + link_num.ToString());
		Debug.Log("Demand num = " + demand_num.ToString());
		Debug.Log("Wave num = " + wave_num.ToString());
		Debug.Log("Max reachable length = " + optical_reach.ToString() + " KM");
		Debug.Log("Dimension num = " + dimension_num.ToString());

		graph = new Graph(node_num, link_num);
		demands = new Demand[demand_num];
		break_down = new List<int>[link_num,link_num];
		for (int j = 0; j < link_num; j++)
			for (int i = 0; i < link_num; i++)
				break_down[j, i] = new List<int>();


		int[] max_reg = new int[node_num];

		// ��ȡ������Ϣ
		for (int e = 0; e < link_num; e++)
		{
			int u = fin.NextInt(), v = fin.NextInt(), len = fin.NextInt();
			//fin >> u >> v >> len;
			graph.add_edge(e, u, v, len);
		}

		// ��ȡҵ����Ϣ
		wave_remain = new int[link_num];
		for (int e = 0; e < link_num; e++)
		{
			wave_remain[e] = wave_num;
		}
		link_avl_wave = new bool[link_num, wave_num];
		for (int e = 0; e < link_num; e++)
		{
			for(int j=0;j<wave_num;j++)
            {
				link_avl_wave[e, j] = true;
			}
		}

		if (!IGNORE_LOCAL_DIMENSION)
		{
			// ��ʼ������ά����ر���
			node_wave_cnt = new int[node_num, wave_num];
			node_avl_wave = new bool[node_num, dimension_num, wave_num];
			for (int v = 0; v < node_num; v++)
			{
				for (int j = 0; j < wave_num; j++)
					node_wave_cnt[v, j] = 0;
				for (int l = 0; l < dimension_num; l++)
				{
					for(int j=0;j<wave_num;j++)
                    {
						node_avl_wave[v, l, j] = true;
					}
				}
			}
		}

		for (int d = 0; d < demand_num; d++)
		{
			demands[d].reg = new List<int>();
			demands[d].epath = new List<int>();
			demands[d].vpath = new List<int>();
			demands[d].reg_dim = new Dictionary<int, int2>();
			demands[d].backups = new List<int>();

			// Դ�㡢�޵㡢����
			int s = fin.NextInt(), t = fin.NextInt(), w = fin.NextInt();
			//fin >> s >> t >> w;
			demands[d].source = s;
			demands[d].target = t;
			demands[d].wave = w;

			{
				int ls = fin.NextInt(), lt = fin.NextInt(); // Դ�㡢�޵�ʹ�õı���ά��
				//fin >> ls >> lt;
				demands[d].src_dim = ls;
				demands[d].tar_dim = lt;

				if (!IGNORE_LOCAL_DIMENSION)
				{ // ���ڵ��ڱ���ά���Ƿ�ʹ�ó�ͻ
					if (!node_avl_wave[s, ls, w])
						Debug.LogError("Error working source dimension");
					if (!node_avl_wave[t, lt, w])
						Debug.LogError("Error working target dimension");

					// Դ�㡢�޵��ڶ�Ӧ����ά�ȵĲ����޷�ʹ��
					node_avl_wave[s, ls, w] = false;
					node_avl_wave[t, lt, w] = false;
					// �ò���ʹ�ô���+1
					node_wave_cnt[s, w]++;
					node_wave_cnt[t, w]++;
				}
			}

			// ��㹤��·��
			int v_num = fin.NextInt();
			//fin >> v_num;
			while ((v_num--)!=0)
			{
				int v = fin.NextInt();
				//fin >> v;
				demands[d].vpath.Add(v);
			}

			// �߹���·��
			demands[d].work_link = new bool[link_num];
			for (int j = 0; j < link_num; j++)
				demands[d].work_link[j] = false;
			int e_num = fin.NextInt();
			//fin >> e_num;
			while ((e_num--)!=0)
			{
				int e = fin.NextInt();
				//fin >> e;
				demands[d].epath.Add(e);
				demands[d].work_link[e] = true;
				link_avl_wave[e,demands[d].wave] = false; // ��·�Ĳ�����ռ����
				wave_remain[e]--;                          // ��·ʣ����ò�����-1
			}

			// �Ѱ�װ�м�
			demands[d].installed = new bool[node_num];
			for (int j = 0; j < node_num; j++)
				demands[d].installed[j] = false;

			int reg_num = fin.NextInt();
			//fin >> reg_num;
			while ((reg_num--)!=0)
			{
				int v = fin.NextInt(), ls = fin.NextInt(), lt = fin.NextInt();
				//fin >> v >> ls >> lt; // ��ȡ�����м̽ڵ��뱾��ά��
				demands[d].reg.Add(v);
				demands[d].installed[v] = true;
				demands[d].reg_dim[v] = new int2(ls, lt);

				if (!IGNORE_LOCAL_DIMENSION)
				{ // ���ڵ��ڱ���ά���Ƿ�ʹ�ó�ͻ
					if (!node_avl_wave[v,ls,demands[d].wave])
						Debug.LogError("Error working reg dimension");
					if (!node_avl_wave[v,lt,demands[d].wave])
						Debug.LogError("Error working reg dimension");

					// �����м̵���������ά�ȵĲ����޷�ʹ��
					node_avl_wave[v,ls,demands[d].wave] = false;
					node_avl_wave[v,lt,demands[d].wave] = false;
					// �ò���ʹ�ô���+2
					node_wave_cnt[v,demands[d].wave] += 2;
				}
			}
		}
		//fin.close();
		fin.Dispose();

		// ���㵱ǰͼ�Ķ������������һ����
		int cnt = 0;
		for (int i = 0; i < node_num; i++)
			for (int j = 0; j < node_num; j++)
				if (graph.MG[i,j].Count() > 0)
					cnt++;

		double val = (double)(cnt + node_num) / (node_num * node_num);
		Debug.Log("Density of graph = " + val.ToString());
		//printf("Density of graph = %.2f\n", (double)(cnt + node_num) / (node_num * node_num));

		// ������Ӱ������󼯺�
		List<int>[] failure = new List<int>[link_num]; // һ�ι���Ӱ�����󼯺�
		for(int i=0;i<link_num;i++)
			failure[i] = new List<int>();
		for (int d = 0; d < demand_num; d++)
			foreach (var e in demands[d].epath) // ����·������һ�߹��ϼ����յ�Ӱ��
				failure[e].Add(d);
		// ������һ�ι���Ӱ���ҵ��ϲ���ȥ��
		for (int e1 = 0; e1 < link_num; e1++)
			for (int e2 = 0; e2 < e1 + 1; e2++)
			{
				HashSet<int> s = new HashSet<int>();
				//set<int> s;
				foreach (int i in failure[e1])
					s.Add(i);
				foreach (int i in failure[e2])
					s.Add(i);
				break_down[e1,e2].AddRange(s);
			}
		// ���ϳ����Գ��Ը���
		for (int e1 = 0; e1 < link_num; e1++)
			for (int e2 = e1 + 1; e2 < link_num; e2++)
				break_down[e1,e2] = break_down[e2,e1];
	}
}
