using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ScenarioBuilder.Activation;
using ScenarioBuilder.Analysis;
using ScenarioBuilder.Core;
using ScenarioBuilder.Modes;
using ScenarioBuilder.Steps;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ScenarioBuilder.Editor
{
    internal static class ScenarioEditorGUI
    {
        private static readonly Color[] StageTints =
        {
            new(0.31f, 0.66f, 1f), new(0.36f, 0.78f, 0.47f), new(1f, 0.67f, 0.25f), new(0.75f, 0.5f, 1f),
        };

        public static Color StageTint(int stage) => StageTints[stage % StageTints.Length];

        public static void DrawIssues(IReadOnlyCollection<string> issues)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Проверка настроек", EditorStyles.boldLabel);
            if (issues.Count == 0)
            {
                EditorGUILayout.HelpBox("Ошибок не найдено.", MessageType.Info);
                return;
            }

            foreach (string issue in issues)
            {
                EditorGUILayout.HelpBox(issue, MessageType.Warning);
            }
        }

        public static void Section(string title)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        public static List<T> FindAssets<T>() where T : Object =>
            AssetDatabase.FindAssets($"t:{typeof(T).Name}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(asset => asset != null)
                .OrderBy(asset => asset.name)
                .ToList();

        public static string Label(Object asset) => asset switch
        {
            null => "(нет)",
            ScenarioAnalyzer analyzer when analyzer.DisplayName != analyzer.name => $"{analyzer.DisplayName} ({analyzer.name})",
            ScenarioMode mode when mode.DisplayName != mode.name => $"{mode.DisplayName} ({mode.name})",
            _ => asset.name,
        };

        /// <summary>An object field drawn as a dropdown of the matching assets in the project.</summary>
        public static void AssetPopup<T>(SerializedProperty property, GUIContent label, string noneLabel,
            IReadOnlyList<T> choices = null) where T : Object
        {
            List<T> assets = (choices ?? FindAssets<T>()).Where(asset => asset != null).ToList();
            var current = property.objectReferenceValue as T;
            if (current != null && !assets.Contains(current))
            {
                assets.Add(current);
            }

            var options = new List<GUIContent> { new(noneLabel) };
            options.AddRange(assets.Select(asset => new GUIContent(Label(asset))));

            int index = current == null ? 0 : assets.IndexOf(current) + 1;
            int chosen = EditorGUILayout.Popup(label, index, options.ToArray());
            if (chosen != index)
            {
                property.objectReferenceValue = chosen == 0 ? null : assets[chosen - 1];
            }
        }

        /// <summary>
        /// "Add" button for a list of asset references: offers the project assets not in the list yet
        /// and every concrete type that can be created as a new asset next to <paramref name="owner"/>.
        /// </summary>
        public static void AddAssetButton<T>(SerializedProperty list, string buttonText, Object owner) where T : ScriptableObject
        {
            if (!GUILayout.Button(buttonText, EditorStyles.miniPullDown, GUILayout.ExpandWidth(false)))
            {
                return;
            }

            var present = new HashSet<Object>(Enumerable.Range(0, list.arraySize)
                .Select(i => list.GetArrayElementAtIndex(i).objectReferenceValue));
            var menu = new GenericMenu();

            foreach (T asset in FindAssets<T>().Where(asset => !present.Contains(asset)))
            {
                menu.AddItem(new GUIContent(Label(asset)), false, () => Append(list, asset));
            }

            if (menu.GetItemCount() > 0)
            {
                menu.AddSeparator(string.Empty);
            }

            foreach (Type type in TypeCache.GetTypesDerivedFrom<T>().Where(type => !type.IsAbstract).OrderBy(type => type.Name))
            {
                menu.AddItem(new GUIContent($"Создать новый/{CreateMenuName(type)}"), false,
                    () => Append(list, CreateAssetNextTo(type, owner)));
            }

            menu.ShowAsContext();
        }

        public static ScriptableObject CreateAssetNextTo(Type type, Object owner, string subfolder = null)
        {
            string ownerPath = owner != null ? AssetDatabase.GetAssetPath(owner) : string.Empty;
            string folder = string.IsNullOrEmpty(ownerPath) ? "Assets" : Path.GetDirectoryName(ownerPath).Replace('\\', '/');
            if (!string.IsNullOrEmpty(subfolder))
            {
                if (!AssetDatabase.IsValidFolder($"{folder}/{subfolder}"))
                {
                    AssetDatabase.CreateFolder(folder, subfolder);
                }

                folder = $"{folder}/{subfolder}";
            }

            ScriptableObject asset = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath($"{folder}/{type.Name}.asset"));
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(asset);
            return asset;
        }

        public static void Append(SerializedProperty list, Object asset)
        {
            list.serializedObject.Update();
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = asset;
            list.serializedObject.ApplyModifiedProperties();
        }

        private static string CreateMenuName(Type type)
        {
            var attribute = (CreateAssetMenuAttribute)Attribute.GetCustomAttribute(type, typeof(CreateAssetMenuAttribute));
            string menuName = attribute?.menuName;
            return string.IsNullOrEmpty(menuName) ? ObjectNames.NicifyVariableName(type.Name) : menuName.Split('/').Last();
        }

        /// <summary>The scenario whose library holds the step, used to number steps the way the scenario lists them.</summary>
        public static ScenarioDefinition FindScenarioOf(ScenarioMode mode) =>
            FindAssets<ScenarioDefinition>().FirstOrDefault(scenario => scenario.Modes.Contains(mode));

        public static string StepNumber(StepDefinition step, ScenarioDefinition scenario)
        {
            int index = scenario == null ? -1 : scenario.Steps.ToList().IndexOf(step);
            return index >= 0 ? (index + 1).ToString() : step.Id;
        }

        /// <summary>"1, 2, 3, 4 → 5 → 6, 7, 8": stages joined by arrows, steps of one stage by commas.</summary>
        public static string StageChain(IEnumerable<ActivationEntry> entries, ScenarioDefinition scenario) =>
            string.Join("  →  ", ActivationPlan.BuildStages(entries)
                .Select(stage => string.Join(", ", stage.Select(step => StepNumber(step, scenario)))));

        public static string OrderKind(IEnumerable<ActivationEntry> entries) =>
            OrderKind(ActivationPlan.BuildStages(entries).Select(stage => stage.Count).ToList());

        /// <summary>"все вместе", "строго по очереди" or "смешанный порядок" for the given stage sizes.</summary>
        public static string OrderKind(IReadOnlyList<int> stageSizes) => stageSizes.Count switch
        {
            0 => "нет шагов",
            1 => stageSizes[0] == 1 ? "один шаг" : "все вместе",
            _ => stageSizes.All(size => size == 1) ? "строго по очереди" : "смешанный порядок",
        };

        /// <summary>Stage index (from 0) of the element at <paramref name="index"/>, or -1 for an empty row.</summary>
        public static int StageOf(SerializedProperty entries, int index)
        {
            int stage = -1;
            for (int i = 0; i <= index && i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("step").objectReferenceValue == null)
                {
                    if (i == index)
                    {
                        return -1;
                    }

                    continue;
                }

                if (stage < 0 || entry.FindPropertyRelative("activation").enumValueIndex == (int)ActivationMode.AfterPrevious)
                {
                    stage++;
                }
            }

            return stage;
        }

        /// <summary>"Add step" button for an activation list: offers the scenario steps that the list does not use yet.</summary>
        public static void AddStepButton(SerializedProperty entries, ScenarioDefinition scenario, string buttonText)
        {
            if (!GUILayout.Button(buttonText, EditorStyles.miniPullDown, GUILayout.ExpandWidth(false)))
            {
                return;
            }

            var used = new HashSet<Object>(Enumerable.Range(0, entries.arraySize)
                .Select(i => entries.GetArrayElementAtIndex(i).FindPropertyRelative("step").objectReferenceValue));
            IEnumerable<StepDefinition> library = scenario != null
                ? scenario.Steps.Where(step => step != null)
                : FindAssets<StepDefinition>();

            var menu = new GenericMenu();
            foreach (StepDefinition step in library.Where(step => !used.Contains(step)))
            {
                string number = scenario != null ? $"{StepNumber(step, scenario)}. " : string.Empty;
                menu.AddItem(new GUIContent($"{number}{step.Id}"), false, () => AppendStep(entries, step));
            }

            if (menu.GetItemCount() == 0)
            {
                menu.AddDisabledItem(new GUIContent("Все шаги сценария уже добавлены"));
            }

            menu.ShowAsContext();
        }

        private static void AppendStep(SerializedProperty entries, StepDefinition step)
        {
            entries.serializedObject.Update();
            entries.arraySize++;
            SerializedProperty entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            entry.FindPropertyRelative("step").objectReferenceValue = step;
            entry.FindPropertyRelative("activation").enumValueIndex = (int)ActivationMode.AfterPrevious;
            entries.serializedObject.ApplyModifiedProperties();
        }
    }
}
