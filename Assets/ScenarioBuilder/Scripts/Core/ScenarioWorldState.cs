using System;
using System.Collections.Generic;

namespace ScenarioBuilder.Core
{
    /// <summary>Current values of the scene state flags. Lives as long as the scene, not a single run.</summary>
    public sealed class ScenarioWorldState
    {
        private readonly HashSet<StateFlag> setFlags = new();

        public event Action<StateFlag, bool> Changed;

        public bool IsSet(StateFlag flag) => flag != null && setFlags.Contains(flag);

        public void Set(StateFlag flag, bool value)
        {
            if (flag == null)
            {
                return;
            }

            bool changed = value ? setFlags.Add(flag) : setFlags.Remove(flag);
            if (changed)
            {
                Changed?.Invoke(flag, value);
            }
        }
    }
}
