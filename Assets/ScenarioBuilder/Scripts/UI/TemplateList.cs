using System.Collections.Generic;
using UnityEngine;

namespace ScenarioBuilder.UI
{
    /// <summary>Spawns copies of an inactive template object under its parent and clears them on demand.</summary>
    internal sealed class TemplateList<T> where T : Component
    {
        private readonly T template;
        private readonly List<T> items = new();

        public TemplateList(T template)
        {
            this.template = template;
            template.gameObject.SetActive(false);
        }

        public T Add()
        {
            T item = Object.Instantiate(template, template.transform.parent);
            item.gameObject.SetActive(true);
            items.Add(item);
            return item;
        }

        public void Clear()
        {
            foreach (T item in items)
            {
                Object.Destroy(item.gameObject);
            }

            items.Clear();
        }
    }
}
