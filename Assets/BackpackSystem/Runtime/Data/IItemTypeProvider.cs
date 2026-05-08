using System.Collections.Generic;

namespace BackpackSystem
{
    /// <summary>道具类型表数据源契约。</summary>
    public interface IItemTypeProvider
    {
        /// <summary>返回所有已注册类型(实现方应缓存,避免每次返回新集合)。</summary>
        IReadOnlyList<ItemTypeData> GetAllTypes();

        /// <summary>按 typeId 查询单个类型。未找到返回 null。</summary>
        ItemTypeData GetTypeById(int typeId);
    }
}
