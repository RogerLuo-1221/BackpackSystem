using System;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackSystem
{
    /// <summary>单个道具格子。挂在 ItemCell.prefab 根节点。</summary>
    public class ItemCellView : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private Text _countText;
        [SerializeField] private Button _button;

        private ItemData _itemData;

        private void Awake()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(HandleClick);
            }
        }

        /// <summary>设置此格子要显示的数据。</summary>
        public void SetData(ItemData itemData, IItemTypeProvider typeProvider, IIconLoader iconLoader)
        {
            _itemData = itemData;

            if (_countText != null)
            {
                _countText.text = itemData.Count.ToString();
            }

            ItemTypeData type = typeProvider != null ? typeProvider.GetTypeById(itemData.TypeId) : null;
            if (type != null && iconLoader != null && _iconImage != null)
            {
                iconLoader.Load(type.IconPath, sprite =>
                {
                    if (this == null) return;
                    if (_iconImage == null) return;
                    _iconImage.sprite = sprite;
                });
            }
        }

        private void HandleClick()
        {
            OnClicked?.Invoke(_itemData);
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClick);
            }
            OnClicked = null;
        }

        /// <summary>点击事件,参数为本格子持有的 ItemData。</summary>
        public event Action<ItemData> OnClicked;
    }
}
