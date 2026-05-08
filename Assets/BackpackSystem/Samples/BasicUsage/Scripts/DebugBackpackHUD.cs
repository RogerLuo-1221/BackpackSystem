using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackSystem.Samples
{
    /// <summary>调试 HUD:dropdown 选道具 + input 数量 + 添加/清空按钮。</summary>
    public class DebugBackpackHUD : MonoBehaviour
    {
        [SerializeField] private Dropdown _typeDropdown;
        [SerializeField] private InputField _countInput;
        [SerializeField] private Button _addButton;
        [SerializeField] private Button _clearButton;

        private Backpack _backpack;
        private IItemTypeProvider _typeProvider;
        private readonly List<int> _typeIdsByDropdownIndex = new List<int>();

        /// <summary>装配 HUD。在 Backpack 和 typeProvider 创建后调用。</summary>
        public void Init(Backpack backpack, IItemTypeProvider typeProvider)
        {
            _backpack = backpack;
            _typeProvider = typeProvider;

            PopulateDropdown();

            if (_addButton != null)
            {
                _addButton.onClick.AddListener(HandleAddClicked);
            }
            if (_clearButton != null)
            {
                _clearButton.onClick.AddListener(HandleClearClicked);
            }
        }

        private void PopulateDropdown()
        {
            if (_typeDropdown == null || _typeProvider == null) return;

            _typeDropdown.ClearOptions();
            _typeIdsByDropdownIndex.Clear();

            IReadOnlyList<ItemTypeData> all = _typeProvider.GetAllTypes();
            var options = new List<Dropdown.OptionData>(all.Count);
            for (int i = 0; i < all.Count; i++)
            {
                ItemTypeData t = all[i];
                if (t == null) continue;
                options.Add(new Dropdown.OptionData(string.IsNullOrEmpty(t.Name) ? $"#{t.Id}" : t.Name));
                _typeIdsByDropdownIndex.Add(t.Id);
            }
            _typeDropdown.AddOptions(options);
            _typeDropdown.value = 0;
            _typeDropdown.RefreshShownValue();
        }

        private void HandleAddClicked()
        {
            if (_backpack == null) return;

            if (_typeIdsByDropdownIndex.Count == 0)
            {
                Debug.LogWarning("[DebugBackpackHUD] No item types available in dropdown.");
                return;
            }

            int index = _typeDropdown != null ? _typeDropdown.value : -1;
            if (index < 0 || index >= _typeIdsByDropdownIndex.Count)
            {
                Debug.LogWarning($"[DebugBackpackHUD] Invalid dropdown index: {index}.");
                return;
            }

            string raw = _countInput != null ? _countInput.text : null;
            if (!int.TryParse(raw, out int count) || count <= 0)
            {
                Debug.LogWarning($"[DebugBackpackHUD] Invalid count input: '{raw}'. Must be a positive integer.");
                return;
            }

            int typeId = _typeIdsByDropdownIndex[index];
            _backpack.AddItem(typeId, count);
        }

        private void HandleClearClicked()
        {
            if (_backpack == null) return;
            _backpack.Clear();
        }

        private void OnDestroy()
        {
            if (_addButton != null)
            {
                _addButton.onClick.RemoveListener(HandleAddClicked);
            }
            if (_clearButton != null)
            {
                _clearButton.onClick.RemoveListener(HandleClearClicked);
            }
        }
    }
}
