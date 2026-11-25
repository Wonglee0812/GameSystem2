using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BrickPanic.EditorTools
{
    /// <summary>Sets up the Kenney character for Brick Panic: import scale (1.8 m tall), looping clips,
    /// skin materials, an Animator Controller (idle/run blend + jump) and the Person prefab. Safe to run twice:
    /// existing materials and the controller are kept; the prefab is rebuilt.</summary>
    public static class BrickPanicPeopleBuilder
    {
        const string Art = "Assets/BrickPanic/Art/Characters";
        const string Model = Art + "/characterMedium.fbx";
        const string ControllerPath = "Assets/BrickPanic/Data/AC_Person.controller";
        const string PrefabPath = "Assets/BrickPanic/Prefabs/Person.prefab";
        const float Scale = 0.5f;   // the file is about 3.6 m tall

        [MenuItem("Brick Panic/Build Person Prefab")]
        public static Person EnsurePerson()
        {
            SetupImport(Model, null);
            SetupImport(Art + "/Animations/idle.fbx", true);
            SetupImport(Art + "/Animations/run.fbx", true);
            SetupImport(Art + "/Animations/jump.fbx", false);

            var skins = new System.Collections.Generic.List<Material>();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Art + "/Skins" }))
            {
                string texPath = AssetDatabase.GUIDToAssetPath(guid);
                string matPath = Path.ChangeExtension(texPath, null) + "_Mat.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (m == null)
                {
                    m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
                    m.SetFloat("_Smoothness", 0.2f);
                    AssetDatabase.CreateAsset(m, matPath);
                }
                skins.Add(m);
            }

            var controller = Controller();

            // The person root holds physics + AI. The model is a child, because the Kenney clips also animate the
            // transform the Animator sits on; on the root that would fight the Rigidbody (teleport to 0,0,0).
            var go = new GameObject("Person");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model));
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.name = "Model";
            model.transform.SetParent(go.transform, false);
            // The Kenney clips are recorded from the "Root" bone (their paths start there), so the Animator goes on Root.
            var oldAnim = model.GetComponent<Animator>();
            if (oldAnim != null) Object.DestroyImmediate(oldAnim);
            var rootBone = model.transform.Find("Root");
            var anim = rootBone.gameObject.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;   // the mesh is not under Root, so do not cull by renderers
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                smr.updateWhenOffscreen = true;
                if (skins.Count > 0) smr.sharedMaterial = skins[0];
            }
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 70f;
            var cap = go.AddComponent<CapsuleCollider>();
            cap.center = new Vector3(0f, 0.9f, 0f);
            cap.height = 1.8f;
            cap.radius = 0.3f;
            cap.sharedMaterial = new PhysicsMaterial("PM_Person") { staticFriction = 0.5f, dynamicFriction = 0.4f, frictionCombine = PhysicsMaterialCombine.Minimum };
            var person = go.AddComponent<Person>();
            person.skins = skins.ToArray();

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);
            Debug.Log("[BrickPanic] person prefab: " + PrefabPath);
            return prefab.GetComponent<Person>();
        }

        /// <param name="loop">null = model (no clips), true/false = clip file and whether it loops.</param>
        static void SetupImport(string path, bool? loop)
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(path);
            bool changed = false;
            if (!Mathf.Approximately(imp.globalScale, Scale)) { imp.globalScale = Scale; changed = true; }
            if (imp.animationType != ModelImporterAnimationType.Generic) { imp.animationType = ModelImporterAnimationType.Generic; changed = true; }
            if (imp.materialImportMode != ModelImporterMaterialImportMode.None) { imp.materialImportMode = ModelImporterMaterialImportMode.None; changed = true; }
            if (loop.HasValue)
            {
                var clips = imp.clipAnimations.Length > 0 ? imp.clipAnimations : imp.defaultClipAnimations;
                foreach (var c in clips)
                {
                    bool want = loop.Value && !c.name.Contains("Targeting");
                    if (c.loopTime != want) { c.loopTime = want; changed = true; }
                }
                imp.clipAnimations = clips;
            }
            if (changed) imp.SaveAndReimport();
        }

        static AnimationClip Clip(string file, string name)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(Art + "/Animations/" + file))
                if (o is AnimationClip c && c.name.Contains(name)) return c;
            return null;
        }

        static AnimatorController Controller()
        {
            var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (ac != null) return ac;
            ac = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ac.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            var sm = ac.layers[0].stateMachine;
            var move = ac.CreateBlendTreeInController("Move", out var tree, 0);
            tree.blendParameter = "Speed";
            tree.AddChild(Clip("idle.fbx", "Idle"), 0f);
            tree.AddChild(Clip("run.fbx", "Run"), 1f);
            var jump = sm.AddState("Jump");
            jump.motion = Clip("jump.fbx", "Jump");
            var toJump = sm.AddAnyStateTransition(jump);
            toJump.AddCondition(AnimatorConditionMode.If, 0f, "Jump");
            toJump.duration = 0.05f;
            toJump.canTransitionToSelf = false;
            var back = jump.AddTransition(move);
            back.hasExitTime = true;
            back.exitTime = 0.9f;
            back.duration = 0.1f;
            sm.defaultState = move;
            return ac;
        }
    }
}
