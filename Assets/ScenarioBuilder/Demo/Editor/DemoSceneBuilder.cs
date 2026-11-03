using System.Linq;
using ScenarioBuilder.Core;
using ScenarioBuilder.Sources;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ScenarioBuilder.Demo.Editor
{
    /// <summary>
    /// Generates the demo scenario assets and the demo scene (world, runner, UI).
    /// Menu: Tools → Scenario Builder. CLI: -executeMethod ScenarioBuilder.Demo.Editor.DemoSceneBuilder.Build
    /// </summary>
    public static class DemoSceneBuilder
    {
        public const string ScenePath = "Assets/ScenarioBuilder/Demo/Scenes/DemoScenario.unity";
        private const string MaterialsFolder = "Assets/ScenarioBuilder/Demo/Materials";
        private const string SampleScenesFolder = "Assets/Scenes";

        [MenuItem("Tools/Scenario Builder/Пересобрать демо-сцену и ассеты")]
        public static void BuildFromMenu()
        {
            bool confirmed = EditorUtility.DisplayDialog("Пересобрать демо",
                "Демо-сцена и ассеты в папке Demo/Data будут созданы заново. Ваши изменения в них пропадут.",
                "Пересобрать", "Отмена");
            if (confirmed)
            {
                Build();
            }
        }

        public static void Build()
        {
            if (TMP_Settings.defaultFontAsset == null)
            {
                throw new System.InvalidOperationException(
                    "TextMesh Pro Essentials не импортированы: Window → TextMeshPro → Import TMP Essential Resources.");
            }

            // The scene goes first: opening a new scene unloads unreferenced assets created before it.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            DemoAssets assets = DemoAssetsBuilder.Build();

            CreateLightingAndCamera();
            ScenarioRunner runner = CreateRunner(assets);
            StateFlagSource valve = CreateWorld(assets);
            DemoUiBuilder.Build(assets, runner, valve);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            if (AssetDatabase.IsValidFolder(SampleScenesFolder))
            {
                AssetDatabase.DeleteAsset(SampleScenesFolder);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Демо-сцена собрана: {ScenePath}");
        }

        private static void CreateLightingAndCamera()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.45f, 0.52f);

            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(52f, -30f, 0f);

            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UiFactory.Hex("0E121A");
            camera.fieldOfView = 42f;
            camera.transform.SetPositionAndRotation(new Vector3(0f, 17f, -11.2f), Quaternion.Euler(57f, 0f, 0f));
            camera.gameObject.AddComponent<AudioListener>();
        }

        private static ScenarioRunner CreateRunner(DemoAssets assets)
        {
            var runner = new GameObject("ScenarioRunner").AddComponent<ScenarioRunner>();
            SerializedWiring.Set(runner, "scenario", assets.Scenario);
            SerializedWiring.Set(runner, "startMode", assets.TrainingMode);
            SerializedWiring.SetList(runner, "analyzers", assets.Analyzers.Cast<Object>().ToList());
            SerializedWiring.SetList(runner, "loggers", assets.Loggers.Cast<Object>().ToList());
            return runner;
        }

        /// <returns>The valve state source, wired to the control panel buttons by the UI builder.</returns>
        private static StateFlagSource CreateWorld(DemoAssets assets)
        {
            if (AssetDatabase.IsValidFolder(MaterialsFolder))
            {
                AssetDatabase.DeleteAsset(MaterialsFolder);
            }

            AssetDatabase.CreateFolder("Assets/ScenarioBuilder/Demo", "Materials");
            Transform world = new GameObject("World").transform;

            Primitive(PrimitiveType.Cube, "Floor", world, new Vector3(0f, -0.1f, 0f), new Vector3(18f, 0.2f, 12f),
                CreateMaterial("Floor", "2A3242"));
            Material wall = CreateMaterial("Wall", "3A4458");
            Primitive(PrimitiveType.Cube, "WallNorth", world, new Vector3(0f, 0.4f, 6.2f), new Vector3(18f, 0.8f, 0.4f), wall);
            Primitive(PrimitiveType.Cube, "WallSouth", world, new Vector3(0f, 0.4f, -6.2f), new Vector3(18f, 0.8f, 0.4f), wall);
            Primitive(PrimitiveType.Cube, "WallWest", world, new Vector3(-9.2f, 0.4f, 0f), new Vector3(0.4f, 0.8f, 12.8f), wall);
            Primitive(PrimitiveType.Cube, "WallEast", world, new Vector3(9.2f, 0.4f, 0f), new Vector3(0.4f, 0.8f, 12.8f), wall);

            Zone("ZoneA", "Зона A", new Vector3(-4.4f, 0f, -2.2f), "F5B841", assets.Actions["EnterZoneA"], world);
            Zone("ZoneB", "Зона B", new Vector3(4.4f, 0f, -2.2f), "F28C38", assets.Actions["EnterZoneB"], world);
            Zone("ExitZone", "Зона выхода", new Vector3(0f, 0f, 3.6f), "3DDC97", assets.Actions["EnterExitZone"], world);
            Post("RedPost", "Красная стойка", new Vector3(-4.6f, 0f, 3.4f), "FF5C5C", assets.Actions["TouchRedPost"], world);
            Post("BluePost", "Синяя стойка", new Vector3(4.6f, 0f, 3.4f), "4FA8FF", assets.Actions["TouchBluePost"], world);
            StateFlagSource valve = Valve(assets, world);
            CreatePlayer(world);
            return valve;
        }

        private static void Zone(string name, string label, Vector3 position, string colorHex, ActionDefinition onEnter,
            Transform parent)
        {
            GameObject zone = Primitive(PrimitiveType.Cube, name, parent, position + new Vector3(0f, 0.02f, 0f),
                new Vector3(3f, 0.04f, 3f), CreateMaterial(name, colorHex));
            var trigger = zone.GetComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 30f, 0f);
            trigger.size = new Vector3(1f, 60f, 1f);

            SerializedWiring.Set(zone.AddComponent<TriggerActionSource>(), "onEnter", onEnter);
            WorldLabel(label, position + new Vector3(0f, 0.1f, 0f), parent, flat: true);
        }

        private static void Post(string name, string label, Vector3 position, string colorHex, ActionDefinition onCollision,
            Transform parent)
        {
            GameObject post = Primitive(PrimitiveType.Cylinder, name, parent, position + new Vector3(0f, 1.1f, 0f),
                new Vector3(0.9f, 1.1f, 0.9f), CreateMaterial(name, colorHex));
            SerializedWiring.Set(post.AddComponent<CollisionActionSource>(), "onCollision", onCollision);
            WorldLabel(label, position + new Vector3(0f, 2.9f, 0f), parent, flat: false);
        }

        private static StateFlagSource Valve(DemoAssets assets, Transform parent)
        {
            var position = new Vector3(0f, 0f, 0.6f);
            Primitive(PrimitiveType.Cube, "ValvePipe", parent, position + new Vector3(0f, 0.35f, 0f),
                new Vector3(0.35f, 0.7f, 0.35f), CreateMaterial("ValvePipe", "8A94A8"));
            GameObject wheel = Primitive(PrimitiveType.Cylinder, "Valve", parent, position + new Vector3(0f, 0.9f, 0f),
                new Vector3(1.3f, 0.12f, 1.3f), CreateMaterial("Valve", "FFFFFF"));

            var source = wheel.AddComponent<StateFlagSource>();
            SerializedWiring.Set(source, "flag", assets.ValveOpen);

            TextMeshPro label = WorldLabel("Клапан: закрыт", position + new Vector3(0f, 0.05f, -1.3f), parent, flat: true);
            var indicator = wheel.AddComponent<StateFlagIndicator>();
            SerializedWiring.Set(indicator, "flag", assets.ValveOpen);
            SerializedWiring.Set(indicator, "targetRenderer", wheel.GetComponent<Renderer>());
            SerializedWiring.Set(indicator, "label", label);
            SerializedWiring.Set(indicator, "onText", "Клапан: открыт");
            SerializedWiring.Set(indicator, "offText", "Клапан: закрыт");
            return source;
        }

        private static void CreatePlayer(Transform parent)
        {
            GameObject player = Primitive(PrimitiveType.Capsule, "Player", parent, new Vector3(0f, 1f, -3.8f), Vector3.one,
                CreateMaterial("Player", "E8ECF4"));
            player.tag = "Player";

            var body = player.AddComponent<Rigidbody>();
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            player.AddComponent<PlayerMover>();

            WorldLabel("Вы", new Vector3(0f, 2.7f, -3.8f), player.transform, flat: false, size: 3f);
        }

        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position,
            Vector3 scale, Material material)
        {
            GameObject primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = parent.InverseTransformPoint(position);
            primitive.transform.localScale = scale;
            primitive.GetComponent<Renderer>().sharedMaterial = material;
            return primitive;
        }

        private static TextMeshPro WorldLabel(string text, Vector3 position, Transform parent, bool flat, float size = 4f)
        {
            var label = new GameObject($"Label ({text})").AddComponent<TextMeshPro>();
            label.transform.SetParent(parent, true);
            label.transform.SetPositionAndRotation(position, Quaternion.Euler(flat ? 90f : 57f, 0f, 0f));
            label.rectTransform.sizeDelta = new Vector2(8f, 2f);
            label.font = TMP_Settings.defaultFontAsset;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = flat ? new Color(0.06f, 0.08f, 0.12f) : UiFactory.TextColor;
            return label;
        }

        private static Material CreateMaterial(string name, string colorHex)
        {
            var material = new Material(Shader.Find("Standard")) { color = UiFactory.Hex(colorHex) };
            material.SetFloat("_Glossiness", 0.25f);
            AssetDatabase.CreateAsset(material, $"{MaterialsFolder}/{name}.mat");
            return material;
        }
    }
}
