using ScenarioBuilder.Core;
using ScenarioBuilder.Feedback;
using ScenarioBuilder.Sources;
using ScenarioBuilder.UI;
using TMPro;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ScenarioBuilder.Demo.Editor
{
    /// <summary>Builds the demo overlay: top bar, task list, control panel, toast, results and journal windows.</summary>
    internal static class DemoUiBuilder
    {
        private const float TopBarHeight = 76f;
        private const float Margin = 20f;

        public static void Build(DemoAssets assets, ScenarioRunner runner, DemoWorld world)
        {
            Transform canvas = CreateCanvas();
            CreateEventSystem();

            TopBar topBar = CreateTopBar(canvas);
            TaskPanel taskPanel = CreateTaskPanel(canvas);
            CreateControlPanel(canvas, assets, world.Valve);
            MessageToast toast = CreateToast(canvas);
            ResultsPanel results = CreateResultsWindow(canvas, runner);
            JournalPanel journal = CreateJournalWindow(canvas, runner);
            SerializedWiring.Set(results, "journalPanel", journal);
            SerializedWiring.SetList(canvas.gameObject.AddComponent<EscapeKey>(), "panels", new Object[] { journal, results });

            var hud = canvas.gameObject.AddComponent<ScenarioHud>();
            SerializedWiring.Set(hud, "runner", runner);
            SerializedWiring.Set(hud, "scenarioTitle", topBar.Title);
            SerializedWiring.Set(hud, "modeButtonTemplate", topBar.ModeButtonTemplate);
            SerializedWiring.Set(hud, "restartButton", topBar.RestartButton);
            SerializedWiring.Set(hud, "journalButton", topBar.JournalButton);
            SerializedWiring.Set(hud, "quitButton", topBar.QuitButton);
            SerializedWiring.Set(hud, "modeText", taskPanel.ModeText);
            SerializedWiring.Set(hud, "groupText", taskPanel.GroupText);
            SerializedWiring.Set(hud, "progressText", taskPanel.ProgressText);
            SerializedWiring.Set(hud, "stepCardTemplate", taskPanel.StepCardTemplate);
            SerializedWiring.Set(hud, "groupHeaderTemplate", taskPanel.GroupHeaderTemplate);
            SerializedWiring.Set(hud, "stepList", taskPanel.StepList);
            SerializedWiring.Set(hud, "showResultsButton", taskPanel.ShowResultsButton);
            SerializedWiring.Set(hud, "toast", toast);
            SerializedWiring.Set(hud, "journalPanel", journal);
            SerializedWiring.Set(hud, "resultsPanel", results);
        }

        private static Transform CreateCanvas()
        {
            RectTransform root = UiFactory.Rect("Canvas", null);
            var canvas = root.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            root.gameObject.AddComponent<GraphicRaycaster>();
            return root;
        }

        private static void CreateEventSystem()
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private sealed class TopBar
        {
            public TMP_Text Title;
            public Button ModeButtonTemplate;
            public Button RestartButton;
            public Button JournalButton;
            public Button QuitButton;
        }

        private static TopBar CreateTopBar(Transform canvas)
        {
            Image bar = UiFactory.Panel("TopBar", canvas, UiFactory.PanelColor, rounded: false);
            UiFactory.Anchor(bar.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(0.5f, 1),
                new Vector2(0, -TopBarHeight), Vector2.zero);
            HorizontalLayoutGroup layout = UiFactory.HorizontalLayout(bar.gameObject, 12);
            layout.padding = new RectOffset(28, 20, 14, 14);

            UiFactory.Size(UiFactory.Panel("Mark", bar.transform, UiFactory.AccentColor).gameObject, 8, 40);
            TextMeshProUGUI title = UiFactory.Text("ScenarioTitle", bar.transform, "Сценарий", 26, UiFactory.TextColor, FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            UiFactory.Size(title.gameObject, flexibleWidth: 1);

            UiFactory.Size(UiFactory.Text("ModeCaption", bar.transform, "Режим:", 18, UiFactory.MutedTextColor, FontStyles.Normal,
                TextAlignmentOptions.MidlineRight).gameObject, 80);

            RectTransform modes = UiFactory.Rect("ModeButtons", bar.transform);
            UiFactory.HorizontalLayout(modes.gameObject, 8);
            UiFactory.Size(modes.gameObject, -1, 48);

            var topBar = new TopBar
            {
                Title = title,
                ModeButtonTemplate = ToggleStyleButton("ModeButtonTemplate", modes, "Режим", 170),
            };

            UiFactory.Size(UiFactory.Rect("Gap", bar.transform).gameObject, 24);
            topBar.RestartButton = UiFactory.Button("RestartButton", bar.transform, "Заново", UiFactory.AccentColor, 48, 150);
            topBar.JournalButton = UiFactory.Button("JournalButton", bar.transform, "Журнал", UiFactory.ButtonColor, 48, 150);
            topBar.QuitButton = UiFactory.Button("QuitButton", bar.transform, "Выход", UiFactory.ButtonColor, 48, 130);
            return topBar;
        }

        private sealed class TaskPanel
        {
            public TMP_Text ModeText;
            public TMP_Text GroupText;
            public TMP_Text ProgressText;
            public StepCardView StepCardTemplate;
            public GroupHeaderView GroupHeaderTemplate;
            public ScrollRect StepList;
            public Button ShowResultsButton;
        }

        private static TaskPanel CreateTaskPanel(Transform canvas)
        {
            Image panel = UiFactory.Panel("TasksPanel", canvas, UiFactory.PanelColor);
            UiFactory.Anchor(panel.rectTransform, Vector2.zero, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(Margin, Margin), new Vector2(Margin + 440, -TopBarHeight - Margin));
            UiFactory.VerticalLayout(panel.gameObject, 10, 22);

            SectionHeader("Header", panel.transform, "ЗАДАНИЯ");
            var result = new TaskPanel
            {
                ModeText = UiFactory.Text("ModeText", panel.transform, "Режим", 30, UiFactory.TextColor, FontStyles.Bold),
                GroupText = UiFactory.Text("GroupText", panel.transform, "Группа", 18, UiFactory.MutedTextColor),
                ProgressText = UiFactory.Text("ProgressText", panel.transform, "Завершено шагов", 18, UiFactory.MutedTextColor),
            };
            UiFactory.Size(UiFactory.Panel("Separator", panel.transform, UiFactory.CardColor, rounded: false).gameObject, -1, 2);

            result.StepList = UiFactory.ScrollView("StepList", panel.transform, out RectTransform steps, 6f);
            result.StepList.GetComponent<Image>().color = Color.clear;
            result.StepList.viewport.offsetMin = Vector2.zero;
            result.StepList.viewport.offsetMax = Vector2.zero;
            UiFactory.Size(result.StepList.gameObject, flexibleHeight: 1);
            result.GroupHeaderTemplate = GroupHeader(steps);
            result.StepCardTemplate = StepCard(steps);

            result.ShowResultsButton = UiFactory.Button("ShowResultsButton", panel.transform, "Показать результаты", UiFactory.AccentColor, 52);
            UiFactory.Text("ControlsHint", panel.transform,
                "Движение: WASD или стрелки. Кнопки и ползунок — на пульте справа.",
                15, UiFactory.MutedTextColor);
            return result;
        }

        private static StepCardView StepCard(Transform parent)
        {
            Image card = UiFactory.Panel("StepCardTemplate", parent, UiFactory.CardColor);
            HorizontalLayoutGroup layout = UiFactory.HorizontalLayout(card.gameObject, 12);
            layout.padding = new RectOffset(10, 12, 9, 9);
            layout.childForceExpandHeight = true;

            Image accent = UiFactory.Panel("Accent", card.transform, UiFactory.AccentColor);
            UiFactory.Size(accent.gameObject, 5);

            RectTransform texts = UiFactory.Rect("Texts", card.transform);
            UiFactory.VerticalLayout(texts.gameObject, 3);
            UiFactory.Size(texts.gameObject, flexibleWidth: 1);
            TextMeshProUGUI title = UiFactory.Text("Title", texts, "Задание", 18, UiFactory.TextColor);
            TextMeshProUGUI details = UiFactory.Text("Details", texts, "Сейчас", 13, UiFactory.AccentColor, FontStyles.Bold);
            details.characterSpacing = 4;

            var view = card.gameObject.AddComponent<StepCardView>();
            SerializedWiring.Set(view, "title", title);
            SerializedWiring.Set(view, "details", details);
            SerializedWiring.Set(view, "accent", accent);
            SerializedWiring.Set(view, "background", card);
            return view;
        }

        private static GroupHeaderView GroupHeader(Transform parent)
        {
            Image background = UiFactory.Panel("GroupHeaderTemplate", parent, Color.clear);
            HorizontalLayoutGroup layout = UiFactory.HorizontalLayout(background.gameObject, 0);
            layout.padding = new RectOffset(10, 10, 7, 7);
            layout.childForceExpandWidth = true;
            TextMeshProUGUI label = UiFactory.Text("Label", background.transform, "Группа", 16, UiFactory.MutedTextColor);

            var view = background.gameObject.AddComponent<GroupHeaderView>();
            SerializedWiring.Set(view, "label", label);
            SerializedWiring.Set(view, "background", background);
            return view;
        }

        private static void CreateControlPanel(Transform canvas, DemoAssets assets, StateFlagSource valve)
        {
            Image panel = UiFactory.Panel("ControlPanel", canvas, UiFactory.PanelColor);
            UiFactory.Anchor(panel.rectTransform, new Vector2(1, 0), Vector2.one, Vector2.one,
                new Vector2(-Margin - 400, Margin), new Vector2(-Margin, -TopBarHeight - Margin));
            UiFactory.VerticalLayout(panel.gameObject, 10, 22);
            Transform root = panel.transform;

            SectionHeader("Header", root, "ПУЛЬТ УПРАВЛЕНИЯ");
            ActionButton(root, "PowerButton", "Питание", assets.Actions["PressPower"]);
            ActionButton(root, "LightsButton", "Освещение", assets.Actions["PressLights"]);

            Button openValve = ActionButton(root, "OpenValveButton", "Открыть клапан", assets.Actions["OpenValve"]);
            UnityEventTools.AddVoidPersistentListener(openValve.onClick, valve.SetOn);
            Button closeValve = ActionButton(root, "CloseValveButton", "Закрыть клапан", assets.Actions["CloseValve"]);
            UnityEventTools.AddVoidPersistentListener(closeValve.onClick, valve.SetOff);

            ActionButton(root, "StartPumpButton", "Пуск насоса", assets.Actions["StartPump"]);
            ActionButton(root, "SensorsButton", "Проверка датчиков", assets.Actions["PressSensors"]);
            CreatePressureControl(root, assets);

            UiFactory.Size(UiFactory.Rect("Spacer", root).gameObject, flexibleHeight: 1);
            ActionButton(root, "FinishButton", "Завершить смену", assets.Actions["PressFinish"], UiFactory.AccentColor);
            ActionButton(root, "EmergencyButton", "Аварийный стоп", assets.Actions["PressEmergency"], UiFactory.DangerColor);
        }

        private static void CreatePressureControl(Transform parent, DemoAssets assets)
        {
            RectTransform header = UiFactory.Rect("PressureHeader", parent);
            UiFactory.HorizontalLayout(header.gameObject, 8);
            UiFactory.Size(UiFactory.Text("Caption", header, "Давление", 20, UiFactory.TextColor, FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft).gameObject, flexibleWidth: 1);
            TextMeshProUGUI value = UiFactory.Text("Value", header, "0%", 20, UiFactory.AccentColor, FontStyles.Bold,
                TextAlignmentOptions.MidlineRight);
            UiFactory.Size(value.gameObject, 80);

            Slider slider = UiFactory.Slider("PressureSlider", parent);
            var feedback = slider.gameObject.AddComponent<ActionFeedback>();
            SerializedWiring.Set(feedback, "graphic", slider.transform.Find("Track").GetComponent<Image>());
            var pressure = slider.gameObject.AddComponent<PressureControl>();
            SerializedWiring.Set(pressure, "slider", slider);
            SerializedWiring.Set(pressure, "valueLabel", value);

            var maxSource = slider.gameObject.AddComponent<UnityEventActionSource>();
            SerializedWiring.Set(maxSource, "action", assets.Actions["PressureMax"]);
            UnityEventTools.AddVoidPersistentListener(pressure.ReachedMax, maxSource.RaiseAction);

            var minSource = slider.gameObject.AddComponent<UnityEventActionSource>();
            SerializedWiring.Set(minSource, "action", assets.Actions["PressureZero"]);
            UnityEventTools.AddVoidPersistentListener(pressure.ReachedMin, minSource.RaiseAction);

            UiFactory.Text("PressureHint", parent, "100% — поднять давление, 0% — сбросить", 14, UiFactory.MutedTextColor);
        }

        private static Button ActionButton(Transform parent, string name, string label, ActionDefinition action,
            Color? color = null)
        {
            Button button = UiFactory.Button(name, parent, label, color ?? UiFactory.ButtonColor, 50);
            SerializedWiring.Set(button.gameObject.AddComponent<ButtonActionSource>(), "action", action);
            SerializedWiring.Set(button.gameObject.AddComponent<ActionFeedback>(), "graphic", button.targetGraphic);
            return button;
        }

        private static MessageToast CreateToast(Transform canvas)
        {
            Image background = UiFactory.Panel("Toast", canvas, UiFactory.CardColor);
            UiFactory.Anchor(background.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(-380, -TopBarHeight - Margin - 100), new Vector2(380, -TopBarHeight - Margin));
            var group = background.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            TextMeshProUGUI text = UiFactory.Text("Message", background.transform, "", 20, UiFactory.TextColor, FontStyles.Bold,
                TextAlignmentOptions.Center);
            UiFactory.Stretch(text.rectTransform, 20, 20, 10, 10);
            text.enableAutoSizing = true;
            text.fontSizeMin = 14;
            text.fontSizeMax = 20;

            var toast = background.gameObject.AddComponent<MessageToast>();
            SerializedWiring.Set(toast, "text", text);
            SerializedWiring.Set(toast, "background", background);
            return toast;
        }

        private static ResultsPanel CreateResultsWindow(Transform canvas, ScenarioRunner runner)
        {
            Modal modal = ModalWindow("ResultsPanel", canvas, new Vector2(1000, 820));
            var panel = modal.Host.AddComponent<ResultsPanel>();
            Transform window = modal.Window;

            SectionHeader("Title", window, "РЕЗУЛЬТАТЫ ПРОХОЖДЕНИЯ");
            TextMeshProUGUI score = UiFactory.Text("Score", window, "0 / 0", 64, UiFactory.TextColor, FontStyles.Bold);
            TextMeshProUGUI analyzer = UiFactory.Text("Analyzer", window, "Анализатор", 18, UiFactory.MutedTextColor);
            TextMeshProUGUI warnings = UiFactory.Text("Warnings", window, "", 16, UiFactory.Hex("F5B841"));
            UiFactory.Text("ColumnsHeader", window, "Шаг<pos=72%>Статус<pos=88%>Баллы", 14, UiFactory.MutedTextColor,
                FontStyles.Bold).margin = new Vector4(10, 0, 10, 0);

            ScrollRect lines = UiFactory.ScrollView("Lines", window, out RectTransform content);
            UiFactory.Size(lines.gameObject, flexibleHeight: 1);
            TextMeshProUGUI lineTemplate = UiFactory.Text("LineTemplate", content, "Шаг", 18, UiFactory.TextColor);

            UiFactory.Text("ReanalyzeCaption", window, "Оценить это же прохождение другим анализатором:", 16, UiFactory.MutedTextColor);
            RectTransform analyzers = UiFactory.Rect("AnalyzerButtons", window);
            UiFactory.HorizontalLayout(analyzers.gameObject, 10);
            UiFactory.Size(analyzers.gameObject, -1, 48);
            Button analyzerTemplate = ToggleStyleButton("AnalyzerButtonTemplate", analyzers, "Анализатор", 280);

            RectTransform actions = UiFactory.Rect("Actions", window);
            UiFactory.HorizontalLayout(actions.gameObject, 10);
            UiFactory.Size(actions.gameObject, -1, 52);
            UiFactory.Size(UiFactory.Rect("Spacer", actions).gameObject, flexibleWidth: 1);
            Button journalButton = UiFactory.Button("JournalButton", actions, "Журнал", UiFactory.ButtonColor, 52, 170);
            Button restartButton = UiFactory.Button("RestartButton", actions, "Пройти заново", UiFactory.AccentColor, 52, 220);
            Button closeButton = UiFactory.Button("CloseButton", actions, "Закрыть", UiFactory.ButtonColor, 52, 150);

            SerializedWiring.Set(panel, "runner", runner);
            SerializedWiring.Set(panel, "root", modal.Overlay);
            SerializedWiring.Set(panel, "scoreText", score);
            SerializedWiring.Set(panel, "analyzerText", analyzer);
            SerializedWiring.Set(panel, "warningsText", warnings);
            SerializedWiring.Set(panel, "lineTemplate", lineTemplate);
            SerializedWiring.Set(panel, "analyzerButtonTemplate", analyzerTemplate);
            SerializedWiring.Set(panel, "journalButton", journalButton);
            SerializedWiring.Set(panel, "restartButton", restartButton);
            SerializedWiring.Set(panel, "closeButton", closeButton);
            return panel;
        }

        private static JournalPanel CreateJournalWindow(Transform canvas, ScenarioRunner runner)
        {
            Modal modal = ModalWindow("JournalPanel", canvas, new Vector2(1180, 860));
            var panel = modal.Host.AddComponent<JournalPanel>();
            Transform window = modal.Window;

            SectionHeader("Title", window, "ЖУРНАЛ СОБЫТИЙ");
            TextMeshProUGUI summary = UiFactory.Text("Summary", window, "", 18, UiFactory.MutedTextColor);
            ScrollRect scroll = UiFactory.ScrollView("Entries", window, out RectTransform content, 0);
            UiFactory.Size(scroll.gameObject, flexibleHeight: 1);
            TextMeshProUGUI entries = UiFactory.Text("EntriesText", content, "", 15, UiFactory.TextColor);

            RectTransform actions = UiFactory.Rect("Actions", window);
            UiFactory.HorizontalLayout(actions.gameObject, 10);
            UiFactory.Size(actions.gameObject, -1, 52);
            UiFactory.Size(UiFactory.Rect("Spacer", actions).gameObject, flexibleWidth: 1);
            Button closeButton = UiFactory.Button("CloseButton", actions, "Закрыть", UiFactory.ButtonColor, 52, 150);

            SerializedWiring.Set(panel, "runner", runner);
            SerializedWiring.Set(panel, "root", modal.Overlay);
            SerializedWiring.Set(panel, "content", entries);
            SerializedWiring.Set(panel, "summary", summary);
            SerializedWiring.Set(panel, "scroll", scroll);
            SerializedWiring.Set(panel, "closeButton", closeButton);
            return panel;
        }

        /// <summary>Host (always active, holds the controller) → Overlay (dim, toggled) → Window (content).</summary>
        private readonly struct Modal
        {
            public Modal(GameObject host, GameObject overlay, Transform window)
            {
                Host = host;
                Overlay = overlay;
                Window = window;
            }

            public GameObject Host { get; }
            public GameObject Overlay { get; }
            public Transform Window { get; }
        }

        private static Modal ModalWindow(string name, Transform canvas, Vector2 size)
        {
            RectTransform host = UiFactory.Stretch(UiFactory.Rect(name, canvas));
            Image dim = UiFactory.Panel("Overlay", host, new Color(0f, 0f, 0f, 0.62f), rounded: false);
            UiFactory.Stretch(dim.rectTransform);
            dim.raycastTarget = true;

            Image window = UiFactory.Panel("Window", dim.transform, UiFactory.PanelColor);
            Vector2 half = size / 2f;
            UiFactory.Anchor(window.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                -half, half);
            UiFactory.VerticalLayout(window.gameObject, 12, 32);
            return new Modal(host.gameObject, dim.gameObject, window.transform);
        }

        private static void SectionHeader(string name, Transform parent, string text)
        {
            TextMeshProUGUI header = UiFactory.Text(name, parent, text, 15, UiFactory.AccentColor, FontStyles.Bold);
            header.characterSpacing = 8;
        }

        /// <summary>A button whose disabled state is highlighted: used for "current mode / current analyzer" choices.</summary>
        private static Button ToggleStyleButton(string name, Transform parent, string label, float width)
        {
            Button button = UiFactory.Button(name, parent, label, Color.white, 48, width);
            ColorBlock colors = button.colors;
            colors.normalColor = UiFactory.ButtonColor;
            colors.highlightedColor = Color.Lerp(UiFactory.ButtonColor, Color.white, 0.15f);
            colors.pressedColor = Color.Lerp(UiFactory.ButtonColor, Color.black, 0.2f);
            colors.selectedColor = UiFactory.ButtonColor;
            colors.disabledColor = UiFactory.AccentColor;
            colors.colorMultiplier = 1f;
            button.colors = colors;
            return button;
        }
    }
}
