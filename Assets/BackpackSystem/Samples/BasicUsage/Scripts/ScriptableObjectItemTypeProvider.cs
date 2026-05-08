using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BackpackSystem.Samples
{
    /// <summary>从 Resources 路径下的 ItemTypeDatabase 读取的 provider 实现。</summary>
    public class ScriptableObjectItemTypeProvider : IItemTypeProvider
    {
        private readonly List<ItemTypeData> _types;
        private readonly Dictionary<int, ItemTypeData> _byId;

        /// <summary>构造时立即从 Resources 加载,失败抛 FileNotFoundException。</summary>
        public ScriptableObjectItemTypeProvider(string resourcesPath)
        {
            ItemTypeDatabase database = Resources.Load<ItemTypeDatabase>(resourcesPath);
            if (database == null)
            {
                throw new FileNotFoundException(
                    $"ItemTypeDatabase not found at Resources path '{resourcesPath}'. " +
                    "Confirm the .asset is placed under a Resources/ folder and the path has no extension.");
            }

            _types = new List<ItemTypeData>(database.Types != null ? database.Types : new List<ItemTypeData>());
            _byId = new Dictionary<int, ItemTypeData>();
            for (int i = 0; i < _types.Count; i++)
            {
                ItemTypeData t = _types[i];
                if (t == null) continue;
                _byId[t.Id] = t;
            }
        }

        public IReadOnlyList<ItemTypeData> GetAllTypes()
        {
            return _types;
        }

        public ItemTypeData GetTypeById(int typeId)
        {
            return _byId.TryGetValue(typeId, out ItemTypeData t) ? t : null;
        }
    }
}
