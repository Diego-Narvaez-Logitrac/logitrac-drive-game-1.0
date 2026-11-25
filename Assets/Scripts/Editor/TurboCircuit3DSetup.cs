using System.IO;
using LogitracDrive.TurboCircuit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace LogitracDrive.EditorTools
{
    /// <summary>
    /// Builds Assets/Scenes/TurboCircuit3D.unity: adds a Universal (3D) Renderer to the existing URP asset
    /// (the 2D Renderer stays the default for every other camera), creates the Lit/Unlit base materials,
    /// a no-bake lighting settings asset, a low warm sun, dusk ambient and fog, and one RaceController in 3D mode.
    /// Batch: -executeMethod LogitracDrive.EditorTools.TurboCircuit3DSetup.BuildFromCommandLine
    /// </summary>
    public static class TurboCircuit3DSetup
    {
        public const string ScenePath = "Assets/Scenes/TurboCircuit3D.unity";
        public const string RendererPath = "Assets/Settings/UniversalRenderer3D.asset";
        public const string LitPath = ProjectSetup.DataFolder + "/Turbo3DLit.mat";
        public const string UnlitPath = ProjectSetup.DataFolder + "/Turbo3DUnlit.mat";
        public const string LightingPath = ProjectSetup.DataFolder + "/TurboCircuit3D.lighting";
        const float ShadowDistance = 20f;

        [MenuItem("Logitrac Drive/Build Turbo Circuit 3D Scene")]
        public static void BuildMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build(ProjectSetup.FindBodyFont(), ProjectSetup.FindDisplayFont());
        }

        /// <summary>Builds only the 3D scene (plus renderer, materials and Build Settings), leaving the other scenes untouched.</summary>
        public static void BuildFromCommandLine()
        {
            Build(ProjectSetup.FindBodyFont(), ProjectSetup.FindDisplayFont());
            ProjectSetup.ApplyBuildScenes();
            AssetDatabase.SaveAssets();
        }

        public static void Build(Font bodyFont, Font displayFont)
        {
            if (!AssetDatabase.IsValidFolder(ProjectSetup.DataFolder)) AssetDatabase.CreateFolder("Assets", "Data");
            int rendererIndex = EnsureRenderer();

            var lit = EnsureMaterial(LitPath, "Universal Render Pipeline/Lit");
            var unlit = EnsureMaterial(UnlitPath, "Universal Render Pipeline/Unlit");
            var tuning = AssetDatabase.LoadAssetAtPath<TurboTuning>(ProjectSetup.TuningPath);
            var sprite = AssetDatabase.LoadAssetAtPath<Material>(ProjectSetup.UnlitMaterialPath);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Lightmapping.lightingSettings = EnsureLightingSettings();
            Race3DLook.ApplyAmbient(); // saved with the scene's RenderSettings

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = false;
            cam.fieldOfView = CameraFollow3D.FieldOfView;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 80f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Race3DLook.Sky;
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.SetRenderer(rendererIndex);
            camData.renderShadows = true;

            var sunGo = new GameObject("Sun");
            sunGo.transform.rotation = Quaternion.Euler(Race3DLook.SunEuler);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = Race3DLook.SunColor;
            sun.intensity = Race3DLook.SunIntensity;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;
            sun.lightmapBakeType = LightmapBakeType.Realtime;
            RenderSettings.sun = sun;

            var race = new GameObject("TurboCircuit3D").AddComponent<RaceController>();
            race.Configure(tuning, sprite, bodyFont, displayFont);
            race.Configure3D(lit, unlit);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[LogitracDrive] Built {ScenePath} (renderer index {rendererIndex}).");
        }

        /// <summary>Adds a Universal Renderer to the project's URP asset once; returns its index. Default renderer is unchanged.</summary>
        static int EnsureRenderer()
        {
            var pipeline = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline == null) pipeline = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            if (pipeline == null) throw new System.InvalidOperationException("No URP asset assigned.");

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.renderingMode = RenderingMode.Forward;
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }

            var so = new SerializedObject(pipeline);
            var list = so.FindProperty("m_RendererDataList");
            int index = -1;
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == renderer) index = i;
            if (index < 0)
            {
                index = list.arraySize;
                list.InsertArrayElementAtIndex(index);
                list.GetArrayElementAtIndex(index).objectReferenceValue = renderer;
            }
            // Shadow settings only matter to the 3D renderer: crisp, soft car shadows near the camera.
            so.FindProperty("m_ShadowDistance").floatValue = ShadowDistance;
            so.FindProperty("m_SoftShadowsSupported").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();
            return index;
        }

        static Material EnsureMaterial(string path, string shaderName)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            mat = new Material(Shader.Find(shaderName));
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static LightingSettings EnsureLightingSettings()
        {
            var ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingPath);
            if (ls == null)
            {
                ls = new LightingSettings { name = "TurboCircuit3D" };
                AssetDatabase.CreateAsset(ls, LightingPath);
            }
            ls.bakedGI = false;    // no baked lighting
            ls.realtimeGI = false;
            EditorUtility.SetDirty(ls);
            return ls;
        }
    }
}
