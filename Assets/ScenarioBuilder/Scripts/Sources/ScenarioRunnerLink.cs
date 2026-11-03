using ScenarioBuilder.Core;
using UnityEngine;

namespace ScenarioBuilder.Sources
{
    /// <summary>Base for scene components that talk to the scenario runner. The runner is found automatically if not set.</summary>
    public abstract class ScenarioRunnerLink : MonoBehaviour
    {
        [SerializeField, Tooltip("Раннер сценария. Если пусто, он найдётся в сцене сам.")]
        private ScenarioRunner runner;

        protected ScenarioRunner Runner
        {
            get
            {
                if (runner == null)
                {
                    runner = FindAnyObjectByType<ScenarioRunner>();
                }

                return runner;
            }
        }
    }
}
