using System;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackSystem
{
    /// <summary>单个分类页签。挂在 CategoryTab.prefab 根节点。</summary>
    public class CategoryTabView : MonoBehaviour
    {
        [SerializeField] private Text _label;
        [SerializeField] private GameObject _selectedIndicator;
        [SerializeField] private Button _button;

        /// <summary>本页签代表的分类。由 SetCategory 写入。</summary>
        public ItemCategory Category { get; private set; }

        private void Awake()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(HandleClick);
            }
        }

        /// <summary>设置本页签代表的分类与显示文案。</summary>
        public void SetCategory(ItemCategory category, string displayName)
        {
            Category = category;
            if (_label != null)
            {
                _label.text = displayName;
            }
        }

        /// <summary>切换选中态视觉指示器。</summary>
        public void SetSelected(bool selected)
        {
            if (_selectedIndicator != null)
            {
                _selectedIndicator.SetActive(selected);
            }
        }

        private void HandleClick()
        {
            OnSelected?.Invoke(Category);
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClick);
            }
            OnSelected = null;
        }

        /// <summary>页签被点击后触发,参数为本页签的分类。</summary>
        public event Action<ItemCategory> OnSelected;
    }
}
