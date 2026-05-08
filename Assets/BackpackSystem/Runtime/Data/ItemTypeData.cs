using System;

namespace BackpackSystem
{
    /// <summary>道具类型数据(静态)。Inspector 可序列化以支持 ScriptableObject 编辑。</summary>
    [Serializable]
    public class ItemTypeData
    {
        public int Id;
        public string Name;
        public string IconPath;
        public string Description;
        public int MaxStackCount;
        public ItemCategory Category;
    }
}
