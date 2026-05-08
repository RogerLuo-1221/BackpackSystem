using System;
using System.Collections.Generic;
using UnityEngine;

namespace BackpackSystem
{
    /// <summary>背包主面板。挂在 BackpackPanel.prefab 根节点。</summary>
    public class BackpackPanelView : MonoBehaviour
    {
        [SerializeField] private Transform _categoryTabContainer;
        [SerializeField] private Transform _itemCellContainer;
        [SerializeField] private ItemCellView _itemCellPrefab;
        [SerializeField] private CategoryTabView _categoryTabPrefab;

        private Backpack _backpack;
        private IItemTypeProvider _typeProvider;
        private IIconLoader _iconLoader;
        private readonly List<CategoryTabView> _tabs = new List<CategoryTabView>();
        private readonly List<ItemCellView> _cells = new List<ItemCellView>();

        /// <summary>装配面板。Backpack 创建后、面板显示前调用一次。</summary>
        public void Init(Backpack backpack, IItemTypeProvider typeProvider, IIconLoader iconLoader)
        {
            _backpack = backpack;
            _typeProvider = typeProvider;
            _iconLoader = iconLoader;

            backpack.OnContentsChanged += HandleContentsChanged;
            backpack.OnCategoryChanged += HandleCategoryChanged;

            BuildCategoryTabs();
            RenderItems(backpack.GetItemsInCurrentCategory());
        }

        private void BuildCategoryTabs()
        {
            if (_categoryTabPrefab == null || _categoryTabContainer == null) return;

            foreach (ItemCategory category in Enum.GetValues(typeof(ItemCategory)))
            {
                CategoryTabView tab = Instantiate(_categoryTabPrefab, _categoryTabContainer);
                tab.SetCategory(category, GetCategoryDisplayName(category));
                tab.SetSelected(category == _backpack.CurrentCategory);
                tab.OnSelected += HandleTabSelected;
                _tabs.Add(tab);
            }
        }

        private static string GetCategoryDisplayName(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.All: return "全部";
                case ItemCategory.Equipment: return "装备";
                case ItemCategory.Usable: return "可使用";
                case ItemCategory.Material: return "材料";
                case ItemCategory.Fragment: return "碎片";
                case ItemCategory.ExpPill: return "经验丹";
                case ItemCategory.Other: return "其他";
                default: return category.ToString();
            }
        }

        private void HandleTabSelected(ItemCategory category)
        {
            if (_backpack == null) return;
            _backpack.SetCategory(category);
        }

        private void HandleContentsChanged()
        {
            if (_backpack == null) return;
            RenderItems(_backpack.GetItemsInCurrentCategory());
        }

        private void HandleCategoryChanged(ItemCategory category)
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                if (_tabs[i] != null)
                {
                    _tabs[i].SetSelected(_tabs[i].Category == category);
                }
            }
            if (_backpack != null)
            {
                RenderItems(_backpack.GetItemsInCurrentCategory());
            }
        }

        private void HandleCellClicked(ItemData item)
        {
            if (_backpack == null || item == null) return;
            _backpack.NotifyItemClicked(item.InstanceId);
        }

        /// <summary>销毁时取消事件订阅,防止 Backpack 持有死引用。</summary>
        private void OnDestroy()
        {
            if (_backpack != null)
            {
                _backpack.OnContentsChanged -= HandleContentsChanged;
                _backpack.OnCategoryChanged -= HandleCategoryChanged;
            }
            for (int i = 0; i < _tabs.Count; i++)
            {
                if (_tabs[i] != null)
                {
                    _tabs[i].OnSelected -= HandleTabSelected;
                }
            }
            for (int i = 0; i < _cells.Count; i++)
            {
                if (_cells[i] != null)
                {
                    _cells[i].OnClicked -= HandleCellClicked;
                }
            }
        }

        /// <summary>
        /// 渲染指定道具列表。阶段一:全量清空重建。阶段二会替换内部实现。
        /// </summary>
        private void RenderItems(IReadOnlyList<ItemData> items)
        {
            if (_itemCellContainer == null || _itemCellPrefab == null) return;

            for (int i = 0; i < _cells.Count; i++)
            {
                if (_cells[i] != null)
                {
                    _cells[i].OnClicked -= HandleCellClicked;
                    Destroy(_cells[i].gameObject);
                }
            }
            _cells.Clear();

            for (int i = 0; i < items.Count; i++)
            {
                ItemData item = items[i];
                ItemCellView cell = Instantiate(_itemCellPrefab, _itemCellContainer);
                cell.SetData(item, _typeProvider, _iconLoader);
                cell.OnClicked += HandleCellClicked;
                _cells.Add(cell);
            }
        }
    }
}
