using System.Collections.Generic;
using System.Linq;
using ScenarioBuilder.Core;
using ScenarioBuilder.Feedback;
using ScenarioBuilder.Sources;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
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
        private const int IgnoreRaycastLayer = 2;

        private static GeneratedAssetFolder materials;

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
            materials = new GeneratedAssetFolder(MaterialsFolder);

            CreateLightingAndCamera();
            ScenarioRunner runner = CreateRunner(assets);
            DemoWorld world = CreateWorld(assets);
            DemoEffectsBuilder.Build(assets, world);
            DemoUiBuilder.Build(assets, runner, world);

            materials.Finish();
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

            // Hover and press on 3D objects. Trigger zones sit on Ignore Raycast so they do not catch the mouse.
            camera.gameObject.AddComponent<PhysicsRaycaster>().eventMask = ~(1 << IgnoreRaycastLayer);
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

        private static DemoWorld CreateWorld(DemoAssets assets)
        {
            Transform root = new GameObject("World").transform;
            var world = new DemoWorld { Root = root };

            Primitive(PrimitiveType.Cube, "Floor", root, new Vector3(0f, -0.1f, 0f), new Vector3(18f, 0.2f, 12f),
                CreateMaterial("Floor", "2A3242"));
            Material wall = CreateMaterial("Wall", "3A4458");
            Primitive(PrimitiveType.Cube, "WallNorth", root, new Vector3(0f, 0.4f, 6.2f), new Vector3(18f, 0.8f, 0.4f), wall);
            Primitive(PrimitiveType.Cube, "WallSouth", root, new Vector3(0f, 0.4f, -6.2f), new Vector3(18f, 0.8f, 0.4f), wall);
            Primitive(PrimitiveType.Cube, "WallWest", root, new Vector3(-9.2f, 0.4f, 0f), new Vector3(0.4f, 0.8f, 12.8f), wall);
            Primitive(PrimitiveType.Cube, "WallEast", root, new Vector3(9.2f, 0.4f, 0f), new Vector3(0.4f, 0.8f, 12.8f), wall);

            world.Zones.Add(Zone("ZoneA", "Зона A", new Vector3(-4.4f, 0f, -2.2f), "F5B841", assets.Actions["EnterZoneA"], root));
            world.Zones.Add(Zone("ZoneB", "Зона B", new Vector3(4.4f, 0f, -2.2f), "F28C38", assets.Actions["EnterZoneB"], root));
            world.Zones.Add(Zone("ExitZone", "Зона выхода", new Vector3(0f, 0f, 3.6f), "3DDC97", assets.Actions["EnterExitZone"], root));
            world.Posts.Add(Post("RedPost", "Красная стойка", new Vector3(-4.6f, 0f, 3.4f), "FF5C5C", assets.Actions["TouchRedPost"], root));
            world.Posts.Add(Post("BluePost", "Синяя стойка", new Vector3(4.6f, 0f, 3.4f), "4FA8FF", assets.Actions["TouchBluePost"], root));
            Valve(assets, world);
            PowerCabinet(assets, world);
            Pump(assets, world);
            CreatePlayer(root);
            return world;
        }

        private static GameObject Zone(string name, string label, Vector3 position, string colorHex, ActionDefinition onEnter,
            Transform parent)
        {
            GameObject zone = Primitive(PrimitiveType.Cube, name, parent, position + new Vector3(0f, 0.02f, 0f),
                new Vector3(3f, 0.04f, 3f), CreateMaterial(name, colorHex));
            zone.layer = IgnoreRaycastLayer;
            var trigger = zone.GetComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 30f, 0f);
            trigger.size = new Vector3(1f, 60f, 1f);

            SerializedWiring.Set(zone.AddComponent<TriggerActionSource>(), "onEnter", onEnter);
            AddFeedback(zone, zone.GetComponent<Renderer>());
            WorldLabel(label, position + new Vector3(0f, 0.1f, 0f), parent, flat: true);
            return zone;
        }

        private static GameObject Post(string name, string label, Vector3 position, string colorHex, ActionDefinition onCollision,
            Transform parent)
        {
            GameObject post = Primitive(PrimitiveType.Cylinder, name, parent, position + new Vector3(0f, 1.1f, 0f),
                new Vector3(0.9f, 1.1f, 0.9f), CreateMaterial(name, colorHex));
            SerializedWiring.Set(post.AddComponent<CollisionActionSource>(), "onCollision", onCollision);
            WorldLabel(label, position + new Vector3(0f, 2.9f, 0f), parent, flat: false);
            ActionFeedback feedback = AddFeedback(post, post.GetComponent<Renderer>());
            SetHoverHint(feedback, "Подойдите и коснитесь", position + new Vector3(0f, 3.5f, 0f));
            return post;
        }

        private static void Valve(DemoAssets assets, DemoWorld world)
        {
            var position = new Vector3(0f, 0f, 0.6f);
            world.ValvePipe = Primitive(PrimitiveType.Cube, "ValvePipe", world.Root, position + new Vector3(0f, 0.35f, 0f),
                new Vector3(0.35f, 0.7f, 0.35f), CreateMaterial("ValvePipe", "8A94A8"));
            GameObject wheel = Primitive(PrimitiveType.Cylinder, "Valve", world.Root, position + new Vector3(0f, 0.9f, 0f),
                new Vector3(1.3f, 0.12f, 1.3f), CreateMaterial("Valve", "FFFFFF"));

            world.Valve = wheel.AddComponent<StateFlagSource>();
            SerializedWiring.Set(world.Valve, "flag", assets.ValveOpen);

            TextMeshPro label = WorldLabel("Клапан: закрыт", position + new Vector3(0f, 0.05f, -1.3f), world.Root, flat: true);
            var indicator = wheel.AddComponent<StateFlagIndicator>();
            SerializedWiring.Set(indicator, "flag", assets.ValveOpen);
            SerializedWiring.Set(indicator, "targetRenderer", wheel.GetComponent<Renderer>());
            SerializedWiring.Set(indicator, "label", label);
            SerializedWiring.Set(indicator, "onText", "Клапан: открыт");
            SerializedWiring.Set(indicator, "offText", "Клапан: закрыт");

            ActionFeedback feedback = AddFeedback(wheel, wheel.GetComponent<Renderer>(),
                assets.Actions["OpenValve"], assets.Actions["CloseValve"]);
            SetHoverHint(feedback, "Клапан: кнопки «Открыть / Закрыть клапан»", position + new Vector3(0f, 2.2f, 0f));
        }

        private static void PowerCabinet(DemoAssets assets, DemoWorld world)
        {
            var position = new Vector3(-2.6f, 0f, 0.8f);
            Transform cabinet = new GameObject("PowerCabinet").transform;
            cabinet.SetParent(world.Root, false);
            cabinet.position = position;

            GameObject body = Primitive(PrimitiveType.Cube, "Body", cabinet, position + new Vector3(0f, 0.7f, 0f),
                new Vector3(1f, 1.4f, 0.6f), CreateMaterial("Cabinet", "566179"));
            world.Lamp = Primitive(PrimitiveType.Sphere, "Lamp", cabinet, position + new Vector3(0f, 1.6f, 0f),
                Vector3.one * 0.38f, CreateMaterial("Lamp", "C9CED8"));

            ActionFeedback feedback = AddFeedback(cabinet.gameObject, body.GetComponent<Renderer>(),
                assets.Actions["PressPower"], assets.Actions["PressLights"], assets.Actions["PressEmergency"]);
            SerializedWiring.SetList(feedback, "renderers", new Object[] { body.GetComponent<Renderer>(), world.Lamp.GetComponent<Renderer>() });
            SetHoverHint(feedback, "Щит: «Питание» и «Освещение»", position + new Vector3(0f, 2.6f, 0f));
            WorldLabel("Щит питания", position + new Vector3(0f, 0.05f, -1f), world.Root, flat: true, size: 3f, onFloor: true);
            world.Cabinet = cabinet;
        }

        private static void Pump(DemoAssets assets, DemoWorld world)
        {
            var position = new Vector3(2.6f, 0f, 0.8f);
            Transform pump = new GameObject("Pump").transform;
            pump.SetParent(world.Root, false);
            pump.position = position;

            Material metal = CreateMaterial("Pump", "4E7FA8");
            GameObject frame = Primitive(PrimitiveType.Cube, "Frame", pump, position + new Vector3(0f, 0.15f, 0f),
                new Vector3(1.3f, 0.3f, 0.8f), CreateMaterial("PumpFrame", "3A4458"));
            GameObject body = Primitive(PrimitiveType.Cylinder, "Body", pump, position + new Vector3(0f, 0.65f, 0f),
                new Vector3(0.7f, 0.55f, 0.7f), metal);
            body.transform.rotation = Quaternion.Euler(0f, 0f, 90f);

            ActionFeedback feedback = AddFeedback(pump.gameObject, body.GetComponent<Renderer>(), assets.Actions["StartPump"]);
            SerializedWiring.SetList(feedback, "renderers", new Object[] { body.GetComponent<Renderer>(), frame.GetComponent<Renderer>() });
            SetHoverHint(feedback, "Насос: кнопка «Пуск насоса»", position + new Vector3(0f, 2.2f, 0f));
            WorldLabel("Насос", position + new Vector3(0f, 0.05f, -1f), world.Root, flat: true, size: 3f, onFloor: true);
            world.Pump = pump;
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

        private static ActionFeedback AddFeedback(GameObject target, Renderer renderer, params ActionDefinition[] actions)
        {
            var feedback = target.AddComponent<ActionFeedback>();
            SerializedWiring.SetList(feedback, "actions", actions.Cast<Object>().ToList());
            SerializedWiring.SetList(feedback, "renderers", new Object[] { renderer });
            return feedback;
        }

        private static void SetHoverHint(ActionFeedback feedback, string text, Vector3 position)
        {
            TextMeshPro hint = WorldLabel(text, position, feedback.transform, flat: false, size: 2.6f);
            hint.name = "HoverHint";
            hint.color = UiFactory.Hex("FFD27A");
            hint.gameObject.SetActive(false);
            SerializedWiring.Set(feedback, "hoverHint", hint.gameObject);
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

        private static TextMeshPro WorldLabel(string text, Vector3 position, Transform parent, bool flat, float size = 4f,
            bool onFloor = false)
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
            label.color = onFloor ? UiFactory.MutedTextColor : flat ? new Color(0.06f, 0.08f, 0.12f) : UiFactory.TextColor;
            return label;
        }

        /// <summary>Standard material with emission switched on, so feedback can make it glow.</summary>
        private static Material CreateMaterial(string name, string colorHex)
        {
            var material = new Material(Shader.Find("Standard")) { color = UiFactory.Hex(colorHex) };
            material.SetFloat("_Glossiness", 0.25f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.black);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            return materials.Write(string.Empty, name, material, "mat");
        }
    }

    /// <summary>Scene objects the UI builder and effects need to reference.</summary>
    internal sealed class DemoWorld
    {
        public Transform Root;
        public readonly List<GameObject> Zones = new();
        public readonly List<GameObject> Posts = new();
        public StateFlagSource Valve;
        public GameObject ValvePipe;
        public Transform Cabinet;
        public GameObject Lamp;
        public Transform Pump;
    }
}
