using ScenarioBuilder.Core;
using UnityEngine;

namespace ScenarioBuilder.Sources
{
    /// <summary>
    /// Base for components that turn scene events into scenario actions.
    /// To add a new source, derive from this class and call <see cref="Raise"/>.
    /// </summary>
    public abstract class ScenarioActionSource : ScenarioRunnerLink
    {
        protected void Raise(ActionDefinition action)
        {
            if (action == null)
            {
                return;
            }

            if (Runner == null)
            {
                Debug.LogWarning($"{name}: в сцене нет ScenarioRunner, действие «{action.DisplayName}» потеряно.", this);
                return;
            }

            Runner.RaiseAction(action);
        }
    }
}
