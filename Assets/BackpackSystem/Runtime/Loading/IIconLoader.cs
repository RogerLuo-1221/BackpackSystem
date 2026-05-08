using System;
using UnityEngine;

namespace BackpackSystem
{
    /// <summary>图标加载契约。回调形式以兼容未来异步加载。</summary>
    public interface IIconLoader
    {
        /// <summary>加载图标。失败时回调传 null。</summary>
        void Load(string iconPath, Action<Sprite> onLoaded);
    }
}
