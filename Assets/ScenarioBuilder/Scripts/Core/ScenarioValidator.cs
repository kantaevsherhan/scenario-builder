using System.Collections.Generic;
using System.Linq;
using ScenarioBuilder.Activation;
using ScenarioBuilder.Modes;
using ScenarioBuilder.Steps;

namespace ScenarioBuilder.Core
{
    public static class ScenarioValidator
    {
        public static List<string> Validate(ScenarioDefinition scenario)
        {
            var issues = new List<string>();
            List<StepDefinition> library = scenario.Steps.Where(step => step != null).ToList();

            if (library.Count < scenario.Steps.Count)
            {
                issues.Add("В списке шагов есть пустые ячейки.");
            }

            foreach (IGrouping<string, StepDefinition> duplicate in library.GroupBy(step => step.Id).Where(group => group.Count() > 1))
            {
                issues.Add($"Идентификатор «{duplicate.Key}» используется несколькими шагами.");
            }

            foreach (StepDefinition step in library)
            {
                issues.AddRange(step.Validate().Select(issue => $"Шаг «{step.Id}»: {issue}"));
            }

            if (scenario.Modes.Count == 0)
            {
                issues.Add("Не задан ни один режим прохождения.");
            }

            foreach (ScenarioMode mode in scenario.Modes)
            {
                if (mode == null)
                {
                    issues.Add("В списке режимов есть пустые ячейки.");
                    continue;
                }

                issues.AddRange(Validate(mode, library));
            }

            return issues;
        }

        /// <param name="library">When given, also checks that the mode uses exactly the steps of the scenario.</param>
        public static List<string> Validate(ScenarioMode mode, IReadOnlyCollection<StepDefinition> library = null)
        {
            var issues = new List<string>();
            string prefix = $"Режим «{mode.DisplayName}»";
            List<ActivationEntry> entries = mode.Groups.SelectMany(group => group.Steps).ToList();
            List<StepDefinition> used = entries.Where(entry => entry?.Step != null).Select(entry => entry.Step).ToList();

            if (used.Count < entries.Count)
            {
                issues.Add($"{prefix}: есть строки без шага.");
            }

            foreach (StepGroup emptyGroup in mode.Groups.Where(group => !group.Steps.Any(entry => entry?.Step != null)))
            {
                issues.Add($"{prefix}: группа «{emptyGroup.Name}» не содержит шагов.");
            }

            foreach (StepDefinition duplicate in used.GroupBy(step => step).Where(group => group.Count() > 1).Select(group => group.Key))
            {
                issues.Add($"{prefix}: шаг «{duplicate.Id}» указан несколько раз.");
            }

            if (library == null)
            {
                return issues;
            }

            foreach (StepDefinition missing in library.Except(used))
            {
                issues.Add($"{prefix}: шаг «{missing.Id}» не включён ни в одну группу.");
            }

            foreach (StepDefinition foreign in used.Distinct().Except(library))
            {
                issues.Add($"{prefix}: шаг «{foreign.Id}» не входит в список шагов сценария.");
            }

            return issues;
        }
    }
}
