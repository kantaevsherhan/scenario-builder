using System.Collections.Generic;
using System.Linq;
using ScenarioBuilder.Core;
using ScenarioBuilder.Sources;
using ScenarioBuilder.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ScenarioBuilder.Feedback
{
    /// <summary>
    /// Colors a 3D object or a UI element by what happens with its actions: the step is waiting for it,
    /// done or failed, the action has just fired or caused a violation. 3D objects also react to the mouse
    /// (the camera needs a PhysicsRaycaster).
    /// </summary>
    public sealed class ActionFeedback : ScenarioRunnerLink,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private enum StepLink
        {
            None,
            Expected,
            Completed,
            Failed,
        }

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField, Tooltip("Действия этого объекта. Действия источников событий на этом же объекте добавляются сами.")]
        private List<ActionDefinition> actions = new();

        [SerializeField, Tooltip("3D-объекты, которые светятся (Emission материала должен быть включён).")]
        private List<Renderer> renderers = new();

        [SerializeField, Tooltip("UI-элемент, который окрашивается.")]
        private UnityEngine.UI.Graphic graphic;

        [SerializeField, Tooltip("Объект, который показывается при наведении курсора. Можно оставить пустым.")]
        private GameObject hoverHint;

        [SerializeField, Min(0.05f)] private float flashSeconds = 0.45f;

        private readonly HashSet<ActionDefinition> ownActions = new();
        private MaterialPropertyBlock block;
        private Color graphicColor;
        private ScenarioSession session;
        private StepLink link;
        private Color flashColor;
        private float flashStart = float.NegativeInfinity;
        private bool isHovered;
        private bool isPressed;

        private void Awake()
        {
            foreach (ActionDefinition action in actions.Concat(GetComponents<ScenarioActionSource>().SelectMany(source => source.Actions)))
            {
                if (action != null)
                {
                    ownActions.Add(action);
                }
            }

            if (graphic != null)
            {
                graphicColor = graphic.color;
            }

            if (hoverHint != null)
            {
                hoverHint.SetActive(false);
            }
        }

        private void OnEnable()
        {
            ScenarioPreferences.Changed += UpdateLink;
            if (Runner == null)
            {
                return;
            }

            Runner.SessionStarted += OnSessionStarted;
            Runner.ActionRaised += OnActionRaised;
            if (Runner.Session != null)
            {
                OnSessionStarted(Runner.Session);
            }
        }

        private void OnDisable()
        {
            ScenarioPreferences.Changed -= UpdateLink;
            if (Runner != null)
            {
                Runner.SessionStarted -= OnSessionStarted;
                Runner.ActionRaised -= OnActionRaised;
            }

            SetSession(null);
        }

        private void Update()
        {
            float flash = 1f - Mathf.Clamp01((Time.unscaledTime - flashStart) / flashSeconds);
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);

            if (graphic != null)
            {
                Color color = link switch
                {
                    StepLink.Expected => Color.Lerp(graphicColor, UiPalette.Accent, Mathf.Lerp(0.3f, 0.75f, pulse)),
                    StepLink.Completed => Color.Lerp(graphicColor, UiPalette.Success, 0.4f),
                    StepLink.Failed => Color.Lerp(graphicColor, UiPalette.Error, 0.5f),
                    _ => graphicColor,
                };
                color = Color.Lerp(color, flashColor, flash);
                color.a = graphicColor.a;
                graphic.color = color;
            }

            if (renderers.Count > 0)
            {
                Color emission = link switch
                {
                    StepLink.Expected => UiPalette.Accent * Mathf.Lerp(0.15f, 0.6f, pulse),
                    StepLink.Completed => UiPalette.Success * 0.3f,
                    StepLink.Failed => UiPalette.Error * 0.4f,
                    _ => Color.black,
                };
                emission += Color.white * ((isHovered ? 0.15f : 0f) + (isPressed ? 0.2f : 0f));
                emission = Color.Lerp(emission, flashColor * 0.9f, flash);
                ApplyEmission(emission);
            }
        }

        public void OnPointerEnter(PointerEventData eventData) => SetHovered(true);

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHovered(false);
            isPressed = false;
        }

        public void OnPointerDown(PointerEventData eventData) => isPressed = true;

        public void OnPointerUp(PointerEventData eventData) => isPressed = false;

        private void SetHovered(bool value)
        {
            isHovered = value;
            if (hoverHint != null)
            {
                hoverHint.SetActive(value);
            }
        }

        private void OnSessionStarted(ScenarioSession newSession)
        {
            flashStart = float.NegativeInfinity;
            SetSession(newSession);
        }

        private void SetSession(ScenarioSession newSession)
        {
            if (session != null)
            {
                session.StepChanged -= OnStepChanged;
            }

            session = newSession;
            if (session != null)
            {
                session.StepChanged += OnStepChanged;
            }

            UpdateLink();
        }

        private void OnStepChanged(StepState step) => UpdateLink();

        private void OnActionRaised(ActionDefinition action, bool causedViolation)
        {
            if (!ownActions.Contains(action))
            {
                return;
            }

            flashColor = causedViolation ? UiPalette.Error : Color.white;
            flashStart = Time.unscaledTime;
        }

        /// <summary>The object shows the step that needs it now, or else the outcome of the last step it belonged to.</summary>
        private void UpdateLink()
        {
            link = StepLink.None;
            if (session == null || ownActions.Count == 0)
            {
                return;
            }

            bool hintsAllowed = session.Mode.ShowHints && ScenarioPreferences.HighlightTargets;
            if (hintsAllowed && session.ActiveSteps.Any(step => step.ExpectedActions.Any(ownActions.Contains)))
            {
                link = StepLink.Expected;
                return;
            }

            StepState lastFinished = session.Steps.LastOrDefault(step =>
                step.Status is StepStatus.Completed or StepStatus.Failed && ownActions.Any(step.Definition.Involves));
            link = lastFinished?.Status switch
            {
                StepStatus.Completed => StepLink.Completed,
                StepStatus.Failed => StepLink.Failed,
                _ => StepLink.None,
            };
        }

        private void ApplyEmission(Color emission)
        {
            block ??= new MaterialPropertyBlock();
            foreach (Renderer target in renderers.Where(target => target != null))
            {
                target.GetPropertyBlock(block);
                block.SetColor(EmissionColorId, emission);
                target.SetPropertyBlock(block);
            }
        }
    }
}
