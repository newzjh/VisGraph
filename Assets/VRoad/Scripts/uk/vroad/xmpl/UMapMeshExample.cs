using System.Collections.Generic;
using uk.vroad.api;
using uk.vroad.api.enums;
using uk.vroad.api.geom;
using uk.vroad.api.map;
using uk.vroad.api.xmpl;
using uk.vroad.pac;
using uk.vroad.ucm;
using uk.vroad.api.edit;
using uk.vroad.api.etc;
using uk.vroad.api.events;
using uk.vroad.api.str;
using uk.vroad.apk;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace uk.vroad.xmpl
{
#if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(UMapMeshExample))]
    public class UMapMeshExampleEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            UMapMeshExample example = (UMapMeshExample)target;

            base.OnInspectorGUI();

            if (GUILayout.Button("Save"))
            {
                int FolderIndex = 0;
                for (int i = 0; i < 30; i++)
                {
                    if (!System.IO.Directory.Exists(Application.dataPath + "/Maps/" + i.ToString()))
                    {
                        FolderIndex = i;
                        break;
                    }
                }
                string folder= Application.dataPath + "/Maps/" + FolderIndex.ToString();
                System.IO.Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
                example.SaveToFolder("Assets/Maps/" + FolderIndex.ToString());
            }
        }
    }
#endif
    public class UMapMeshExample : UaMapMesh
    {
        private ExampleApp app;

        public Material[] pitchedRoofMaterials;
        
        public static UMapMeshExample MostRecentInstance { get; private set;  }

        protected override App App() { return app; }

        protected override void Awake()
        {
            MostRecentInstance = this;
            
            app = ExampleApp.AwakeInstance();
            base.Awake();
        }


        protected override void OnMeshCreationFinish()
        {
            base.OnMeshCreationFinish();

            SetupMeshColliders();
        }

        // This is an example of how to create buildings with pitched roofs
        protected override void CreateSolidBuildings(int progress)
        {
            // Leave roof materials array empty for all roofs to be flat
            bool pitchedRoof = pitchedRoofMaterials.Length > 0;

            if (VRoad.GotPro() && parameters.randomizeBuildings && pitchedRoof)
            {
                CreateSolidBuildingsWithPitchedRoofs(progress);
            }
           
            else base.CreateSolidBuildings(progress);
        }
        
        private void CreateSolidBuildingsWithPitchedRoofs(int progress)
        {
            IOutline[] ola = SolidBuildings();

            List<NamedSubMesh> nsml = new List<NamedSubMesh>();
            List<Material> ml = new List<Material>();

            Material[] wallMaterials = buildingMaterials;
            
            // Leave roof materials array empty for all roofs to be flat
            bool pitchedRoof = pitchedRoofMaterials.Length > 0;

            foreach (IOutline ol in ola)
            {
                Material wallMat = RandomWallMaterial(ol, wallMaterials);
                TriMesh walls = ol.WallsTriMesh(wallMat.mainTexture.width / wallMat.mainTexture.height);

                // When pitchedRoof is set, this will return a pitched roof shape if the outline is a rectangle
                TriMesh roof = ol.RoofTriMesh(pitchedRoof);
                    
                if (roof.GetMaterialHint() == MaterialHint.PitchedRoof)
                {
                    TriMesh gables = ol.GableTriMesh();

                    walls = TriMesh.Combine(new TriMesh[] { walls, gables,});
                    
                    nsml.Add(TriangleNamedSubMesh(ol, walls));
                    ml.Add(wallMat);

                    
                    Material roofMat = RandomRoofMaterial(ol, pitchedRoofMaterials);
                    RoofWrapper rw = new RoofWrapper(ol);
                    
                    nsml.Add(TriangleNamedSubMesh(rw, roof));
                    ml.Add(roofMat);
                }
                else
                {
                    TriMesh building = TriMesh.Combine(new TriMesh[] { walls, roof, });
                    
                    nsml.Add(TriangleNamedSubMesh(ol, building));
                    ml.Add(wallMat);
                }
                
               
            }
            
            CreateNamedMeshObjects(progress, goMeshSolidBuildings, nsml.ToArray(), ml.ToArray(), null);
        }

#if UNITY_EDITOR
        public void SaveToFolder(string folder)
        {
            GameObject mapGO = goMeshLanes.transform.parent.gameObject;
            string name = App().Map().GetSuburb();

            string prefabStem = MeshTools.VRoadRoot() + SC.FS + SA.PREFAB_GEN_DIR + SC.FS + name;
            string prefabPath = prefabStem + SA.SUFFIX_PREFAB;
            KFile file = new KFile(prefabPath);
            int suffix = 1;

            while (file.Exists())
            {
                suffix++;
                prefabPath = prefabStem + suffix + SA.SUFFIX_PREFAB;
                file = new KFile(prefabPath);
            }

            MeshFilter[] mfs= mapGO.GetComponentsInChildren<MeshFilter>(true);
            foreach(var mf in mfs)
            {
                if (mf.sharedMesh == null)
                    continue;
                if (mf.sharedMesh.name != null && mf.sharedMesh.name.Length > 0)
                {
                    Debug.Log(folder + "/" + mf.sharedMesh.name + ".asset");
                    AssetDatabase.CreateAsset(mf.sharedMesh, folder + "/" + mf.sharedMesh.name + ".asset");
                }
                else if (mf.sharedMesh.vertexCount > 0)
                {
                    Debug.Log(folder + "/" + mf.name + ".asset");
                    AssetDatabase.CreateAsset(mf.sharedMesh, folder + "/" + mf.name + ".asset");
                }
            }

            UnityEditor.PrefabUtility.SaveAsPrefabAsset(mapGO, folder+"/"+name+".prefab");

        }



#endif
    }
}
