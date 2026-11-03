using System.Collections.Generic;
using ScenarioBuilder.Activation;
using ScenarioBuilder.Analysis;
using ScenarioBuilder.Core;
using ScenarioBuilder.Logging;
using ScenarioBuilder.Modes;
using ScenarioBuilder.Steps;
using UnityEditor;
using UnityEngine;

namespace ScenarioBuilder.Demo.Editor
{
    /// <summary>All demo assets the scene builder needs to wire.</summary>
    internal sealed class DemoAssets
    {
        public readonly Dictionary<string, ActionDefinition> Actions = new();
        public StateFlag ValveOpen;
        public ScenarioDefinition Scenario;
        public ScenarioMode TrainingMode;
        public List<ScenarioAnalyzer> Analyzers = new();
        public List<ScenarioLogger> Loggers = new();
    }

    /// <summary>Creates the demo scenario as regular assets: actions, state flag, 12 steps, two modes, analyzers, loggers.</summary>
    internal static class DemoAssetsBuilder
    {
        public const string DataFolder = "Assets/ScenarioBuilder/Demo/Data";

        private static GeneratedAssetFolder folder;

        public static DemoAssets Build()
        {
            folder = new GeneratedAssetFolder(DataFolder);
            var assets = new DemoAssets();
            CreateActions(assets);
            assets.ValveOpen = CreateAsset<StateFlag>("Flags", "ValveOpen");
            SetDisplayName(assets.ValveOpen, "Клапан открыт");

            List<StepDefinition> steps = CreateSteps(assets);
            CreateAnalyzersAndLoggers(assets);
            ScenarioMode training = CreateTraining(steps, assets.Analyzers[0]);
            ScenarioMode exam = CreateExam(steps, assets.Analyzers[1]);
            assets.TrainingMode = training;

            assets.Scenario = CreateAsset<ScenarioDefinition>("", "DemoScenario");
            assets.Scenario.Setup("Смена оператора насосной станции", steps, new[] { training, exam });
            folder.Finish();
            return assets;
        }

        private static void CreateActions(DemoAssets assets)
        {
            var actions = new (string Key, string Title)[]
            {
                ("PressPower", "Нажата кнопка «Питание»"),
                ("PressLights", "Нажата кнопка «Освещение»"),
                ("OpenValve", "Открыт клапан"),
                ("CloseValve", "Закрыт клапан"),
                ("PressureMax", "Давление поднято до 100%"),
                ("PressureZero", "Давление сброшено до 0%"),
                ("TouchRedPost", "Касание красной стойки"),
                ("TouchBluePost", "Касание синей стойки"),
                ("EnterZoneA", "Вход в зону A"),
                ("EnterZoneB", "Вход в зону B"),
                ("EnterExitZone", "Вход в зону выхода"),
                ("StartPump", "Нажата кнопка «Пуск насоса»"),
                ("PressSensors", "Нажата кнопка «Проверка датчиков»"),
                ("PressFinish", "Нажата кнопка «Завершить смену»"),
                ("PressEmergency", "Нажата кнопка «Аварийный стоп»"),
            };

            foreach ((string key, string title) in actions)
            {
                ActionDefinition action = CreateAsset<ActionDefinition>("Actions", key);
                SetDisplayName(action, title);
                assets.Actions.Add(key, action);
            }
        }

        private static List<StepDefinition> CreateSteps(DemoAssets assets)
        {
            Dictionary<string, ActionDefinition> a = assets.Actions;
            StateRequirement ValveMustBeOpen(string message) => new(assets.ValveOpen, true, message);

            return new List<StepDefinition>
            {
                Step("01_Power", "power", "Включите питание (кнопка «Питание»)", new[] { new RequiredAction(a["PressPower"]) },
                    new ForbiddenAction(a["PressEmergency"], "Аварийный стоп нажат без причины")),
                Step("02_Lights", "lights", "Включите освещение (кнопка «Освещение»)", new[] { new RequiredAction(a["PressLights"]) }),
                Step("03_OpenValve", "open-valve", "Откройте клапан", new[] { new RequiredAction(a["OpenValve"]) },
                    new ForbiddenAction(a["StartPump"], "Насос нельзя запускать, пока не открыт клапан")),
                Step("04_Pressure", "pressure-up", "Поднимите давление до 100% при открытом клапане",
                    new[] { new RequiredAction(a["PressureMax"],
                        ValveMustBeOpen("Клапан закрыт: откройте клапан и поднимите давление заново")) }),
                Step("05_RedPost", "red-post", "Подойдите к красной стойке и коснитесь её", new[] { new RequiredAction(a["TouchRedPost"]) }),
                Step("06_Zones", "zones-a-b", "Пройдите в зону A, затем в зону B",
                    new[] { new RequiredAction(a["EnterZoneA"]), new RequiredAction(a["EnterZoneB"]) }),
                Step("07_Pump", "pump", "Запустите насос (клапан должен быть открыт)",
                    new[] { new RequiredAction(a["StartPump"], ValveMustBeOpen("Клапан закрыт: насос нельзя запускать")) }),
                Step("08_ExitZone", "exit-zone", "Перейдите в зелёную зону выхода", new[] { new RequiredAction(a["EnterExitZone"]) }),
                Step("09_Sensors", "sensors", "Нажмите «Проверка датчиков»", new[] { new RequiredAction(a["PressSensors"]) }),
                Step("10_BluePost", "blue-post", "Коснитесь синей стойки", new[] { new RequiredAction(a["TouchBluePost"]) }),
                Step("11_PressureDown", "pressure-down", "Сбросьте давление до 0%", new[] { new RequiredAction(a["PressureZero"]) }),
                Step("12_Finish", "finish", "Нажмите «Завершить смену»", new[] { new RequiredAction(a["PressFinish"]) }),
            };
        }

        private static ActionStep Step(string assetName, string id, string instruction, RequiredAction[] required,
            params ForbiddenAction[] forbidden)
        {
            ActionStep step = CreateAsset<ActionStep>("Steps", assetName);
            step.Setup(id, instruction, required, forbidden);
            return step;
        }

        private static void CreateAnalyzersAndLoggers(DemoAssets assets)
        {
            AttemptScoreAnalyzer attempts = CreateAsset<AttemptScoreAnalyzer>("Analyzers", "TrainingScore");
            attempts.SetDisplayName("Оценка по попыткам");
            PenaltyScoreAnalyzer penalties = CreateAsset<PenaltyScoreAnalyzer>("Analyzers", "ExamScore");
            penalties.SetDisplayName("Оценка по штрафам");
            assets.Analyzers.Add(attempts);
            assets.Analyzers.Add(penalties);

            assets.Loggers.Add(CreateAsset<ConsoleScenarioLogger>("Loggers", "ConsoleLogger"));
            assets.Loggers.Add(CreateAsset<JsonFileScenarioLogger>("Loggers", "JsonFileLogger"));
        }

        /// <summary>Mixed activation from the specification: 1–4 together, 5, 6, 7, 8 one by one, 9+10, 11+12.</summary>
        private static ScenarioMode CreateTraining(List<StepDefinition> s, ScenarioAnalyzer analyzer)
        {
            TrainingMode mode = CreateAsset<TrainingMode>("Modes", "TrainingMode");
            mode.SetCommon("Тренировка", OutOfTurnPolicy.Violation, analyzer);
            mode.Setup(new[]
            {
                After(s[0]), With(s[1]), With(s[2]), With(s[3]),
                After(s[4]), After(s[5]), After(s[6]), After(s[7]),
                After(s[8]), With(s[9]),
                After(s[10]), With(s[11]),
            });
            return mode;
        }

        private static ScenarioMode CreateExam(List<StepDefinition> s, ScenarioAnalyzer analyzer)
        {
            ExamMode mode = CreateAsset<ExamMode>("Modes", "ExamMode");
            mode.SetCommon("Экзамен", OutOfTurnPolicy.Violation, analyzer, hints: false);
            mode.Setup(2, new[]
            {
                new StepGroup("Подготовка оборудования", new[] { After(s[0]), With(s[1]), With(s[2]), With(s[3]) }),
                new StepGroup("Обход площадки", new[] { After(s[4]), After(s[5]), After(s[6]), After(s[7]) }),
                new StepGroup("Завершение смены", new[] { After(s[8]), With(s[9]), After(s[10]), With(s[11]) }),
            });
            return mode;
        }

        private static ActivationEntry After(StepDefinition step) => new(step, ActivationMode.AfterPrevious);

        private static ActivationEntry With(StepDefinition step) => new(step, ActivationMode.WithPrevious);

        private static void SetDisplayName(Object asset, string displayName)
        {
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T CreateAsset<T>(string subfolder, string assetName) where T : ScriptableObject =>
            folder.Write(subfolder, assetName, ScriptableObject.CreateInstance<T>());
    }
}
