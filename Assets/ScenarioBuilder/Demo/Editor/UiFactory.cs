using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ScenarioBuilder.Demo.Editor
{
    internal static class UiFactory
    {
        public static readonly Color PanelColor = Hex("161B26", 0.94f);
        public static readonly Color WindowColor = Hex("161B26");
        public static readonly Color CardColor = Hex("222A3A");
        public static readonly Color ButtonColor = Hex("2C3650");
        public static readonly Color TextColor = Hex("E8ECF4");
        public static readonly Color MutedTextColor = Hex("8A94A8");
        public static readonly Color AccentColor = Hex("4FA8FF");
        public static readonly Color DangerColor = Hex("B83A3A");
        public static readonly Color PlayColor = Hex("2E9E5B");

        private static Sprite roundedSprite;
        private static Sprite RoundedSprite =>
            roundedSprite ??= AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        public static Color Hex(string hex, float alpha = 1f)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            color.a = alpha;
            return color;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static RectTransform Stretch(RectTransform rect, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        public static RectTransform Anchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        public static Image Panel(string name, Transform parent, Color color, bool rounded = true)
        {
            Image image = Rect(name, parent).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (rounded)
            {
                image.sprite = RoundedSprite;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 0.6f;
            }

            return image;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, string value, float size, Color color,
            FontStyles style = FontStyles.Normal, TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
        {
            TextMeshProUGUI text = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = style;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        public static Button Button(string name, Transform parent, string label, Color color, float height = 48f,
            float preferredWidth = -1f)
        {
            Image background = Panel(name, parent, color);
            background.raycastTarget = true;

            Button button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.88f, 0.88f, 0.88f);
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
            colors.selectedColor = new Color(0.88f, 0.88f, 0.88f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            TextMeshProUGUI text = Text("Label", background.transform, label, 20, TextColor, FontStyles.Bold,
                TextAlignmentOptions.Center);
            Stretch(text.rectTransform, 12, 12);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;

            Size(background.gameObject, preferredWidth, height);
            return button;
        }

        public static Toggle Toggle(string name, Transform parent, string label)
        {
            RectTransform root = Rect(name, parent);
            HorizontalLayout(root.gameObject, 14);
            Size(root.gameObject, -1, 40);

            Image box = Panel("Box", root, ButtonColor);
            box.raycastTarget = true;
            Size(box.gameObject, 32, 32);
            Image check = Panel("Checkmark", box.transform, AccentColor, rounded: false);
            check.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
            Stretch(check.rectTransform, 4, 4, 4, 4);

            TextMeshProUGUI text = Text("Label", root, label, 18, TextColor, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            Size(text.gameObject, flexibleWidth: 1);

            Toggle toggle = root.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = box;
            toggle.graphic = check;
            toggle.isOn = true;
            return toggle;
        }

        public static Slider Slider(string name, Transform parent)
        {
            RectTransform root = Rect(name, parent);
            Size(root.gameObject, -1, 28);

            Image track = Panel("Track", root, CardColor);
            Stretch(track.rectTransform, 0, 0, 9, 9);
            track.raycastTarget = true;

            RectTransform fillArea = Stretch(Rect("FillArea", root), 0, 0, 9, 9);
            Image fill = Panel("Fill", fillArea, AccentColor);
            fill.rectTransform.sizeDelta = Vector2.zero;

            RectTransform handleArea = Stretch(Rect("HandleArea", root), 14, 14);
            Image handle = Panel("Handle", handleArea, TextColor);
            handle.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            handle.type = Image.Type.Simple;
            handle.raycastTarget = true;
            handle.rectTransform.sizeDelta = new Vector2(28, 0);

            Slider slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.wholeNumbers = true;
            return slider;
        }

        /// <summary>ScrollRect → Viewport (mask) → Content (vertical layout + content size fitter).</summary>
        public static ScrollRect ScrollView(string name, Transform parent, out RectTransform content, float spacing = 6f)
        {
            Image background = Panel(name, parent, Hex("10141D", 0.9f));
            ScrollRect scroll = background.gameObject.AddComponent<ScrollRect>();

            Image viewportImage = Panel("Viewport", background.transform, Color.white);
            RectTransform viewport = Stretch(viewportImage.rectTransform, 10, 10, 10, 10);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewportImage.color = new Color(1, 1, 1, 0);

            content = Rect("Content", viewport);
            Anchor(content, new Vector2(0, 1), Vector2.one, new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            VerticalLayoutGroup layout = VerticalLayout(content.gameObject, spacing);
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            return scroll;
        }

        public static VerticalLayoutGroup VerticalLayout(GameObject target, float spacing, int padding = 0)
        {
            VerticalLayoutGroup layout = target.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static HorizontalLayoutGroup HorizontalLayout(GameObject target, float spacing, int padding = 0)
        {
            HorizontalLayoutGroup layout = target.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static LayoutElement Size(GameObject target, float preferredWidth = -1, float preferredHeight = -1,
            float flexibleWidth = -1, float flexibleHeight = -1)
        {
            if (!target.TryGetComponent(out LayoutElement element))
            {
                element = target.AddComponent<LayoutElement>();
            }

            element.preferredWidth = preferredWidth;
            element.preferredHeight = preferredHeight;
            element.flexibleWidth = flexibleWidth;
            element.flexibleHeight = flexibleHeight;
            return element;
        }
    }
}
