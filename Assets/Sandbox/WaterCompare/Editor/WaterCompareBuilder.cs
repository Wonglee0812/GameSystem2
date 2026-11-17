using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DamRush.WaterCompare.EditorTools
{
    /// <summary>Menu "Dam Rush/Build Water Compare Scene": makes Scenes/WaterCompare.unity with four lanes (A, B, C, S).</summary>
    public static class WaterCompareBuilder
    {
        const string Root = "Assets/Sandbox";

        [MenuItem("Dam Rush/Build Water Compare Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play mode first"); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var waterMat = AssetDatabase.LoadAssetAtPath<Material>(Root + "/WaterCompare/M_WC_Water.mat");
            if (waterMat == null)
            {
                waterMat = new Material(Shader.Find("DamRush/CompareWater"));
                AssetDatabase.CreateAsset(waterMat, Root + "/WaterCompare/M_WC_Water.mat");
            }
            var groundMat = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/M_DR_LabGround.mat");
            var stoneMat = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/M_DR_Stone.mat");
            var compute = AssetDatabase.LoadAssetAtPath<ComputeShader>(Root + "/WaterCompare/PipeWater.compute");
            var sweCompute = AssetDatabase.LoadAssetAtPath<ComputeShader>(Root + "/WaterCompare/SweWater.compute");

            string[] labels =
            {
                "A  CPU, C# (1 m cells) + new look",
                "B  CPU, Burst jobs (0.5 m cells)",
                "C  GPU, compute shader (0.25 m cells)",
                "S  Sandcastle-style: GPU, momentum + eddies (0.25 m)",
            };
            string[] web =
            {
                "<color=#8fe08f>Web: yes (WebGL 2 and WebGPU)</color>",
                "<color=#ffd080>Web: yes, but slower (no Burst and one thread on the web)</color>",
                "<color=#ff9a70>Web: only WebGPU (not WebGL 2); needs compute shaders</color>",
                "<color=#ff9a70>Web: only WebGPU (not WebGL 2); needs compute shaders</color>",
            };
            float[] cells = { 1f, 0.5f, 0.25f, 0.25f };
            int[] maxSteps = { 6, 12, 40, 40 };
            int lanesCount = cells.Length;

            var sims = new WaterSimBase[lanesCount];
            var grounds = new LaneGround[lanesCount];
            var lanes = new Transform[lanesCount];
            var surfaces = new SurfaceCpu[lanesCount];
            for (int k = 0; k < lanesCount; k++)
            {
                var lane = new GameObject($"Lane {(char)('A' + k)}").transform;
                lane.position = new Vector3(0f, 0f, k * (CompareShape.WidthZ + 10f));
                lanes[k] = lane;

                var g = new GameObject("Ground", typeof(MeshFilter), typeof(MeshRenderer), typeof(LaneGround));
                g.transform.SetParent(lane, false);
                g.GetComponent<MeshRenderer>().sharedMaterial = groundMat;
                grounds[k] = g.GetComponent<LaneGround>();
                grounds[k].wallMaterial = stoneMat;

                var simGo = new GameObject("Water sim");
                simGo.transform.SetParent(lane, false);
                WaterSimBase sim;
                if (k == 0) sim = simGo.AddComponent<SimCpu>();
                else if (k == 1) sim = simGo.AddComponent<SimBurst>();
                else if (k == 2) { var gpu = simGo.AddComponent<SimGpu>(); gpu.shader = compute; sim = gpu; }
                else { var swe = simGo.AddComponent<SimSwe>(); swe.shader = sweCompute; swe.friction = 0.05f; sim = swe; }
                sim.label = labels[k]; sim.cellSize = cells[k]; sim.maxStepsPerFrame = maxSteps[k];
                sims[k] = sim;

                var surf = new GameObject("Water surface", typeof(MeshFilter), typeof(MeshRenderer));
                surf.transform.SetParent(lane, false);
                surf.GetComponent<MeshRenderer>().sharedMaterial = waterMat;
                surf.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (sim is CpuReadableSim cpu) { var sc = surf.AddComponent<SurfaceCpu>(); sc.sim = cpu; surfaces[k] = sc; }
                else surf.AddComponent<SurfaceGpu>().sim = (GpuWaterSim)sim;
            }

            var camGo = Camera.main.gameObject;
            var cam = camGo.AddComponent<CompareCamera>();
            cam.target = new Vector3(CompareShape.LengthX * 0.5f, 0f, (lanesCount * CompareShape.WidthZ + (lanesCount - 1) * 10f) * 0.5f);
            cam.distance = 165f; cam.yaw = -90f; cam.pitch = 55f;
            camGo.GetComponent<Camera>().farClipPlane = 600f;

            var ctrl = new GameObject("Water compare").AddComponent<CompareController>();
            ctrl.sims = sims; ctrl.grounds = grounds; ctrl.lanes = lanes; ctrl.cam = cam; ctrl.webNotes = web; ctrl.surfaces = surfaces; ctrl.blockMaterial = stoneMat;

            EditorSceneManager.SaveScene(scene, Root + "/Scenes/WaterCompare.unity");
            Debug.Log("Water compare scene built: " + Root + "/Scenes/WaterCompare.unity");
        }
    }
}
