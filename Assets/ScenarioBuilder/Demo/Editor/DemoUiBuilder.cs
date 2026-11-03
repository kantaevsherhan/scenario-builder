using ScenarioBuilder.Core;
using ScenarioBuilder.Feedback;
using ScenarioBuilder.Sources;
using ScenarioBuilder.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ScenarioBuilder.Demo.Editor
{
    internal static class DemoUiBuilder
    {
        private const float TopBarHeight = 76f;
        private const float Margin = 20f;
        private const int Pad = 24;
        private const float Gap = 12f;

        public static void Build(DemoAssets assets, ScenarioRunner runner, DemoWorld world)
        {
            Transform canvas = CreateCanvas();
            CreateEventSystem();

            TopBar topBar = CreateTopBar(canvas);
            TaskPanel taskPanel = CreateTaskPanel(canvas);
            GameObject controlPanel = CreateControlPanel(canvas, assets, world.Valve);
            MessageToast toast = CreateToast(canvas);
            ResultsPanel results = CreateResultsWindow(canvas, runner);
            JournalPanel journal = CreateJournalWindow(canvas, runner);
            SerializedWiring.Set(results, "journalPanel", journal);
            GuidedTour tour = CreateTour(canvas, runner, new[]
            {
                Stop(taskPanel.Root, "Список заданий",
                    "Здесь все шаги смены. Синим выделены те, что нужно сделать сейчас, зелёным — выполненные, красным — ошибки."),
                Stop(controlPanel, "Пульт управления",
                    "Кнопки и ползунок отправляют действия. Кнопка, которую ждёт текущий шаг, мигает синим."),
                Stop(world.Player, "Это вы",
                    "Двигайтесь клавишами WASD или стрелками: заходите в зоны и касайтесь стоек."),
                Stop(topBar.ModeButtons, "Режимы",
                    "В тренировке есть подсказки и шаг можно повторить. На экзамене подсказок нет, а ошибки стоят баллов."),
                Stop(topBar.SettingsButton.gameObject, "Настройки",
                    "Здесь подсказки и этот тур можно выключить или пройти тур заново. Esc закрывает окна и подсказки."),
            });
            SettingsPanel settings = CreateSettingsWindow(canvas, tour);
            UnityEventTools.AddVoidPersistentListener(topBar.SettingsButton.onClick, settings.Show);
            SerializedWiring.SetList(canvas.gameObject.AddComponent<EscapeKey>(), "panels",
                new Object[] { tour, settings, journal, results });

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
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
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
            public Button SettingsButton;
            public GameObject ModeButtons;
        }

        private static TopBar CreateTopBar(Transform canvas)
        {
            Image bar = UiFactory.Panel("TopBar", canvas, UiFactory.PanelColor, rounded: false);
            UiFactory.Anchor(bar.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(0.5f, 1),
                new Vector2(0, -TopBarHeight), Vector2.zero);
            HorizontalLayoutGroup layout = UiFactory.HorizontalLayout(bar.gameObject, Gap);
            int inset = (int)Margin + Pad;
            layout.padding = new RectOffset(inset, inset, 14, 14);

            TextMeshProUGUI title = UiFactory.Text("ScenarioTitle", bar.transform, "Сценарий", 26, UiFactory.TextColor, FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft);
            title.enableAutoSizing = true;
            title.fontSizeMin = 16;
            title.fontSizeMax = 26;
            UiFactory.Size(title.gameObject, -1, 48, flexibleWidth: 1).minWidth = 200;

            UiFactory.Size(UiFactory.Text("ModeCaption", bar.transform, "Режим:", 16, UiFactory.MutedTextColor, FontStyles.Normal,
                TextAlignmentOptions.MidlineRight).gameObject, 70);

            RectTransform modes = UiFactory.Rect("ModeButtons", bar.transform);
            UiFactory.HorizontalLayout(modes.gameObject, 8);
            UiFactory.Size(modes.gameObject, -1, 48);

            var topBar = new TopBar
            {
                Title = title,
                ModeButtonTemplate = ToggleStyleButton("ModeButtonTemplate", modes, "Режим", 170),
                ModeButtons = modes.gameObject,
            };

            UiFactory.Size(UiFactory.Rect("Gap", bar.transform).gameObject, Pad);
            topBar.RestartButton = UiFactory.Button("RestartButton", bar.transform, "Заново", UiFactory.AccentColor, 48, 150);
            topBar.JournalButton = UiFactory.Button("JournalButton", bar.transform, "Журнал", UiFactory.ButtonColor, 48, 150);
            topBar.SettingsButton = UiFactory.Button("SettingsButton", bar.transform, "Настройки", UiFactory.ButtonColor, 48, 160);
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
            public GameObject Root;
            public Button ShowResultsButton;
        }

        private static TaskPanel CreateTaskPanel(Transform canvas)
        {
            Image panel = UiFactory.Panel("TasksPanel", canvas, UiFactory.PanelColor);
            UiFactory.Anchor(panel.rectTransform, Vector2.zero, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(Margin, Margin), new Vector2(Margin + 440, -TopBarHeight - Margin));
            UiFactory.VerticalLayout(panel.gameObject, Gap, Pad);

            SectionHeader("Header", panel.transform, "ЗАДАНИЯ");
            var result = new TaskPanel
            {
                Root = panel.gameObject,
                ModeText = UiFactory.Text("ModeText", panel.transform, "Режим", 28, UiFactory.TextColor, FontStyles.Bold),
                GroupText = UiFactory.Text("GroupText", panel.transform, "Группа", 16, UiFactory.MutedTextColor),
                ProgressText = UiFactory.Text("ProgressText", panel.transform, "Завершено шагов", 16, UiFactory.MutedTextColor),
            };
            UiFactory.Size(UiFactory.Panel("Separator", panel.transform, UiFactory.CardColor, rounded: false).gameObject, -1, 2);

            result.StepList = UiFactory.ScrollView("StepList", panel.transform, out RectTransform steps, 8f);
            result.StepList.GetComponent<Image>().color = Color.clear;
            result.StepList.viewport.offsetMin = Vector2.zero;
            result.StepList.viewport.offsetMax = Vector2.zero;
            UiFactory.Size(result.StepList.gameObject, flexibleHeight: 1);
            result.GroupHeaderTemplate = GroupHeader(steps);
            result.StepCardTemplate = StepCard(steps);

            result.ShowResultsButton = UiFactory.Button("ShowResultsButton", panel.transform, "Показать результаты", UiFactory.AccentColor, 48);
            UiFactory.Text("ControlsHint", panel.transform,
                "Движение: WASD или стрелки. Кнопки и ползунок — на пульте справа.",
                14, UiFactory.MutedTextColor);
            return result;
        }

        private static StepCardView StepCard(Transform parent)
        {
            Image card = UiFactory.Panel("StepCardTemplate", parent, UiFactory.CardColor);
            HorizontalLayoutGroup layout = UiFactory.HorizontalLayout(card.gameObject, Gap);
            layout.padding = new RectOffset(12, 14, 10, 10);
            layout.childForceExpandHeight = true;

            Image accent = UiFactory.Panel("Accent", card.transform, UiFactory.AccentColor);
            UiFactory.Size(accent.gameObject, 4);

            RectTransform texts = UiFactory.Rect("Texts", card.transform);
            UiFactory.VerticalLayout(texts.gameObject, 4);
            UiFactory.Size(texts.gameObject, flexibleWidth: 1);
            TextMeshProUGUI title = UiFactory.Text("Title", texts, "Задание", 17, UiFactory.TextColor);
            TextMeshProUGUI details = UiFactory.Text("Details", texts, "Сейчас", 12, UiFactory.AccentColor, FontStyles.Bold);
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
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.childForceExpandWidth = true;
            TextMeshProUGUI label = UiFactory.Text("Label", background.transform, "Группа", 15, UiFactory.MutedTextColor);

            var view = background.gameObject.AddComponent<GroupHeaderView>();
            SerializedWiring.Set(view, "label", label);
            SerializedWiring.Set(view, "background", background);
            return view;
        }

        private static GameObject CreateControlPanel(Transform canvas, DemoAssets assets, StateFlagSource valve)
        {
            Image panel = UiFactory.Panel("ControlPanel", canvas, UiFactory.PanelColor);
            UiFactory.Anchor(panel.rectTransform, new Vector2(1, 0), Vector2.one, Vector2.one,
                new Vector2(-Margin - 400, Margin), new Vector2(-Margin, -TopBarHeight - Margin));
            UiFactory.VerticalLayout(panel.gameObject, Gap, Pad);
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
            return panel.gameObject;
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
            SerializedWiring.Set(maxSource, "hint", "Передвиньте ползунок «Давление» вправо до 100%.");
            UnityEventTools.AddVoidPersistentListener(pressure.ReachedMax, maxSource.RaiseAction);

            var minSource = slider.gameObject.AddComponent<UnityEventActionSource>();
            SerializedWiring.Set(minSource, "action", assets.Actions["PressureZero"]);
            SerializedWiring.Set(minSource, "hint", "Передвиньте ползунок «Давление» влево до 0%.");
            UnityEventTools.AddVoidPersistentListener(pressure.ReachedMin, minSource.RaiseAction);

            UiFactory.Text("PressureHint", parent, "100% — поднять давление, 0% — сбросить", 14, UiFactory.MutedTextColor);
        }

        private static Button ActionButton(Transform parent, string name, string label, ActionDefinition action,
            Color? color = null)
        {
            Button button = UiFactory.Button(name, parent, label, color ?? UiFactory.ButtonColor, 48);
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
            UiFactory.Stretch(text.rectTransform, Pad, Pad, 10, 10);
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
            TextMeshProUGUI analyzer = UiFactory.Text("Analyzer", window, "Анализатор", 16, UiFactory.MutedTextColor);
            TextMeshProUGUI warnings = UiFactory.Text("Warnings", window, "", 16, UiFactory.Hex("F5B841"));
            UiFactory.Text("ColumnsHeader", window, "Шаг<pos=72%>Статус<pos=88%>Баллы", 14, UiFactory.MutedTextColor,
                FontStyles.Bold).margin = new Vector4(10, 0, 10, 0);

            ScrollRect lines = UiFactory.ScrollView("Lines", window, out RectTransform content);
            UiFactory.Size(lines.gameObject, flexibleHeight: 1);
            TextMeshProUGUI lineTemplate = UiFactory.Text("LineTemplate", content, "Шаг", 17, UiFactory.TextColor);

            UiFactory.Text("ReanalyzeCaption", window, "Оценить это же прохождение другим анализатором:", 15, UiFactory.MutedTextColor);
            RectTransform analyzers = UiFactory.Rect("AnalyzerButtons", window);
            UiFactory.HorizontalLayout(analyzers.gameObject, Gap);
            UiFactory.Size(analyzers.gameObject, -1, 48);
            Button analyzerTemplate = ToggleStyleButton("AnalyzerButtonTemplate", analyzers, "Анализатор", 280);

            RectTransform actions = UiFactory.Rect("Actions", window);
            UiFactory.HorizontalLayout(actions.gameObject, Gap);
            UiFactory.Size(actions.gameObject, -1, 48);
            UiFactory.Size(UiFactory.Rect("Spacer", actions).gameObject, flexibleWidth: 1);
            Button journalButton = UiFactory.Button("JournalButton", actions, "Журнал", UiFactory.ButtonColor, 48, 170);
            Button restartButton = UiFactory.Button("RestartButton", actions, "Пройти заново", UiFactory.AccentColor, 48, 220);
            Button closeButton = UiFactory.Button("CloseButton", actions, "Закрыть", UiFactory.ButtonColor, 48, 150);

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
            TextMeshProUGUI summary = UiFactory.Text("Summary", window, "", 16, UiFactory.MutedTextColor);
            ScrollRect scroll = UiFactory.ScrollView("Entries", window, out RectTransform content, 0);
            UiFactory.Size(scroll.gameObject, flexibleHeight: 1);
            TextMeshProUGUI entries = UiFactory.Text("EntriesText", content, "", 15, UiFactory.TextColor);

            RectTransform actions = UiFactory.Rect("Actions", window);
            UiFactory.HorizontalLayout(actions.gameObject, Gap);
            UiFactory.Size(actions.gameObject, -1, 48);
            UiFactory.Size(UiFactory.Rect("Spacer", actions).gameObject, flexibleWidth: 1);
            Button closeButton = UiFactory.Button("CloseButton", actions, "Закрыть", UiFactory.ButtonColor, 48, 150);

            SerializedWiring.Set(panel, "runner", runner);
            SerializedWiring.Set(panel, "root", modal.Overlay);
            SerializedWiring.Set(panel, "content", entries);
            SerializedWiring.Set(panel, "summary", summary);
            SerializedWiring.Set(panel, "scroll", scroll);
            SerializedWiring.Set(panel, "closeButton", closeButton);
            return panel;
        }

        private static (GameObject Target, string Title, string Text) Stop(GameObject target, string title, string text) =>
            (target, title, text);

        private static GuidedTour CreateTour(Transform canvas, ScenarioRunner runner,
            (GameObject Target, string Title, string Text)[] stops)
        {
            RectTransform host = UiFactory.Stretch(UiFactory.Rect("GuidedTour", canvas));
            RectTransform root = UiFactory.Stretch(UiFactory.Rect("Spotlight", host));
            var dim = new Color(0f, 0f, 0f, 0.66f);
            RectTransform DimPanel(string name)
            {
                Image image = UiFactory.Panel(name, root, dim, rounded: false);
                image.raycastTarget = true;
                return image.rectTransform;
            }

            RectTransform top = DimPanel("DimTop");
            RectTransform bottom = DimPanel("DimBottom");
            RectTransform left = DimPanel("DimLeft");
            RectTransform right = DimPanel("DimRight");

            RectTransform frame = UiFactory.Rect("Frame", root);
            foreach ((string name, Vector2 min, Vector2 max) in new[]
            {
                ("Top", new Vector2(0, 1), Vector2.one), ("Bottom", Vector2.zero, new Vector2(1, 0)),
                ("Left", Vector2.zero, new Vector2(0, 1)), ("Right", new Vector2(1, 0), Vector2.one),
            })
            {
                Image line = UiFactory.Panel(name, frame, UiFactory.AccentColor, rounded: false);
                UiFactory.Anchor(line.rectTransform, min, max, new Vector2(0.5f, 0.5f), new Vector2(-1.5f, -1.5f), new Vector2(1.5f, 1.5f));
            }

            Image callout = UiFactory.Panel("Callout", root, UiFactory.WindowColor);
            callout.rectTransform.sizeDelta = new Vector2(480, 0);
            UiFactory.VerticalLayout(callout.gameObject, Gap, Pad);
            callout.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            TextMeshProUGUI title = UiFactory.Text("Title", callout.transform, "Подсказка", 22, UiFactory.TextColor, FontStyles.Bold);
            TextMeshProUGUI body = UiFactory.Text("Body", callout.transform, "", 17, UiFactory.TextColor);

            RectTransform footer = UiFactory.Rect("Footer", callout.transform);
            UiFactory.HorizontalLayout(footer.gameObject, Gap);
            UiFactory.Size(footer.gameObject, -1, 44);
            TextMeshProUGUI counter = UiFactory.Text("Counter", footer, "1 / 1", 14, UiFactory.MutedTextColor, FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft);
            UiFactory.Size(counter.gameObject, flexibleWidth: 1);
            Button skip = UiFactory.Button("SkipButton", footer, "Пропустить", UiFactory.ButtonColor, 44, 150);
            Button next = UiFactory.Button("NextButton", footer, "Далее", UiFactory.AccentColor, 44, 140);
            skip.GetComponent<LayoutElement>().minWidth = 150;
            next.GetComponent<LayoutElement>().minWidth = 140;

            var view = host.gameObject.AddComponent<SpotlightView>();
            SerializedWiring.Set(view, "root", root);
            SerializedWiring.Set(view, "top", top);
            SerializedWiring.Set(view, "bottom", bottom);
            SerializedWiring.Set(view, "left", left);
            SerializedWiring.Set(view, "right", right);
            SerializedWiring.Set(view, "frame", frame);
            SerializedWiring.Set(view, "callout", callout.rectTransform);
            SerializedWiring.Set(view, "title", title);
            SerializedWiring.Set(view, "body", body);
            SerializedWiring.Set(view, "counter", counter);
            SerializedWiring.Set(view, "nextButton", next);
            SerializedWiring.Set(view, "nextLabel", next.GetComponentInChildren<TMP_Text>());
            SerializedWiring.Set(view, "skipButton", skip);

            var tour = host.gameObject.AddComponent<GuidedTour>();
            SerializedWiring.Set(tour, "runner", runner);
            SerializedWiring.Set(tour, "view", view);
            var serialized = new SerializedObject(tour);
            SerializedProperty intro = serialized.FindProperty("intro");
            intro.arraySize = stops.Length;
            for (int i = 0; i < stops.Length; i++)
            {
                SerializedProperty stop = intro.GetArrayElementAtIndex(i);
                stop.FindPropertyRelative("target").objectReferenceValue = stops[i].Target;
                stop.FindPropertyRelative("title").stringValue = stops[i].Title;
                stop.FindPropertyRelative("text").stringValue = stops[i].Text;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return tour;
        }

        private static SettingsPanel CreateSettingsWindow(Transform canvas, GuidedTour tour)
        {
            Modal modal = ModalWindow("SettingsPanel", canvas, new Vector2(720, 360));
            var panel = modal.Host.AddComponent<SettingsPanel>();
            Transform window = modal.Window;

            SectionHeader("Title", window, "НАСТРОЙКИ");
            Toggle tourToggle = UiFactory.Toggle("TourToggle", window, "Обучающий тур и подсказки по шагам");
            Toggle highlightToggle = UiFactory.Toggle("HighlightToggle", window, "Подсветка объектов текущего шага");
            UiFactory.Text("Note", window, "Подсказки работают только в режимах, где они включены (в демо — в тренировке).",
                14, UiFactory.MutedTextColor);
            UiFactory.Size(UiFactory.Rect("Spacer", window).gameObject, flexibleHeight: 1);

            RectTransform actions = UiFactory.Rect("Actions", window);
            UiFactory.HorizontalLayout(actions.gameObject, Gap);
            UiFactory.Size(actions.gameObject, -1, 48);
            UiFactory.Size(UiFactory.Rect("Spacer", actions).gameObject, flexibleWidth: 1);
            Button replay = UiFactory.Button("ReplayTourButton", actions, "Пройти тур заново", UiFactory.AccentColor, 48, 260);
            Button close = UiFactory.Button("CloseButton", actions, "Закрыть", UiFactory.ButtonColor, 48, 150);

            SerializedWiring.Set(panel, "root", modal.Overlay);
            SerializedWiring.Set(panel, "tourToggle", tourToggle);
            SerializedWiring.Set(panel, "highlightToggle", highlightToggle);
            SerializedWiring.Set(panel, "replayTourButton", replay);
            SerializedWiring.Set(panel, "closeButton", close);
            SerializedWiring.Set(panel, "tour", tour);
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

            Image window = UiFactory.Panel("Window", dim.transform, UiFactory.WindowColor);
            Vector2 half = size / 2f;
            UiFactory.Anchor(window.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                -half, half);
            UiFactory.VerticalLayout(window.gameObject, Gap, Pad + 8);
            return new Modal(host.gameObject, dim.gameObject, window.transform);
        }

        private static void SectionHeader(string name, Transform parent, string text)
        {
            TextMeshProUGUI header = UiFactory.Text(name, parent, text, 14, UiFactory.AccentColor, FontStyles.Bold);
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
