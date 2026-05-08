using System.Collections.Generic;
using UnityEngine;

namespace BackpackSystem.Samples
{
    [CreateAssetMenu(fileName = "ItemTypeDatabase", menuName = "BackpackSystem/Item Type Database", order = 1)]
    public class ItemTypeDatabase : ScriptableObject
    {
        public List<ItemTypeData> Types = new List<ItemTypeData>();
    }
}
