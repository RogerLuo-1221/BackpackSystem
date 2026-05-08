using System;
using System.Collections.Generic;

namespace BackpackSystem
{
    /// <summary>
    /// 背包核心逻辑。纯 C# 类,不依赖 Unity MonoBehaviour。
    /// </summary>
    public class Backpack
    {
        private readonly IItemTypeProvider _typeProvider;
        private readonly IInstanceIdGenerator _idGenerator;
        private readonly List<ItemData> _items = new List<ItemData>();
        private ItemCategory _currentCategory = ItemCategory.All;

        public Backpack(IItemTypeProvider typeProvider, IInstanceIdGenerator idGenerator)
        {
            _typeProvider = typeProvider;
            _idGenerator = idGenerator;
        }

        /// <summary>当前选中分类(默认 ItemCategory.All)。</summary>
        public ItemCategory CurrentCategory
        {
            get => _currentCategory;
        }

        /// <summary>
        /// 添加道具(自动堆叠合并)。
        /// 堆叠规则:优先填满已有未满堆 → 满了开新格 → 每格上限 = MaxStackCount。
        /// </summary>
        /// <exception cref="ArgumentException">count ≤ 0</exception>
        /// <exception cref="InvalidOperationException">typeId 在 typeProvider 中不存在</exception>
        public void AddItem(int typeId, int count)
        {
            if (count <= 0)
            {
                throw new ArgumentException($"count must be > 0, got {count}", nameof(count));
            }

            ItemTypeData typeData = _typeProvider.GetTypeById(typeId);
            if (typeData == null)
            {
                throw new InvalidOperationException($"typeId {typeId} not found in typeProvider");
            }

            int max = typeData.MaxStackCount;
            int remaining = count;

            for (int i = 0; i < _items.Count && remaining > 0; i++)
            {
                ItemData item = _items[i];
                if (item.TypeId != typeId) continue;
                int space = max - item.Count;
                if (space <= 0) continue;
                int add = space < remaining ? space : remaining;
                item.Count += add;
                remaining -= add;
            }

            while (remaining > 0)
            {
                int newCount = max < remaining ? max : remaining;
                _items.Add(new ItemData
                {
                    InstanceId = _idGenerator.Generate(),
                    TypeId = typeId,
                    Count = newCount
                });
                remaining -= newCount;
            }

            OnContentsChanged?.Invoke();
        }

        /// <summary>清空背包。触发 OnContentsChanged。</summary>
        public void Clear()
        {
            _items.Clear();
            OnContentsChanged?.Invoke();
        }

        /// <summary>
        /// 当前分类下的可见道具列表(只读快照)。每次调用返回新拷贝,
        /// 不持有 Backpack 内部 list 的活引用,后续 AddItem/Clear 不影响已返回的快照。
        /// </summary>
        public IReadOnlyList<ItemData> GetItemsInCurrentCategory()
        {
            var snapshot = new List<ItemData>(_items.Count);
            if (_currentCategory == ItemCategory.All)
            {
                snapshot.AddRange(_items);
                return snapshot;
            }

            foreach (var item in _items)
            {
                ItemTypeData type = _typeProvider.GetTypeById(item.TypeId);
                if (type != null && type.Category == _currentCategory)
                {
                    snapshot.Add(item);
                }
            }
            return snapshot;
        }

        /// <summary>切换分类。新分类与当前相同时不触发事件。</summary>
        public void SetCategory(ItemCategory category)
        {
            if (_currentCategory == category) return;
            _currentCategory = category;
            OnCategoryChanged?.Invoke(category);
        }

        /// <summary>View 层调用:通知点击事件,触发 OnItemClicked。</summary>
        /// <exception cref="InvalidOperationException">instanceId 不存在</exception>
        public void NotifyItemClicked(string instanceId)
        {
            ItemData target = null;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].InstanceId == instanceId)
                {
                    target = _items[i];
                    break;
                }
            }
            if (target == null)
            {
                throw new InvalidOperationException($"instanceId '{instanceId}' not found in backpack");
            }
            OnItemClicked?.Invoke(target);
        }

        /// <summary>道具内容变化(添加、清空)后触发。</summary>
        public event Action OnContentsChanged;

        /// <summary>分类切换后触发,参数为新分类。</summary>
        public event Action<ItemCategory> OnCategoryChanged;

        /// <summary>道具被点击后触发(经 NotifyItemClicked 转发)。</summary>
        public event Action<ItemData> OnItemClicked;
    }
}
