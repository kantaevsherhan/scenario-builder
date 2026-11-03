using System.Collections.Generic;
using System.Linq;
using ScenarioBuilder.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScenarioBuilder.Demo
{
    /// <summary>Esc closes the first open window from the list; with nothing open it exits the application where it can.</summary>
    public sealed class EscapeKey : MonoBehaviour
    {
        [SerializeField, Tooltip("Окна в порядке приоритета. Каждое должно реализовывать IClosablePanel.")]
        private List<MonoBehaviour> panels = new();

        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return;
            }

            IClosablePanel open = panels.OfType<IClosablePanel>().FirstOrDefault(panel => panel.IsOpen);
            if (open != null)
            {
                open.Hide();
            }
            else
            {
                ApplicationExit.Quit();
            }
        }
    }
}
