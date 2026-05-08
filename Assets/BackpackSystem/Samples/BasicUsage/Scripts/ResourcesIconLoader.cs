using System;
using UnityEngine;

namespace BackpackSystem.Samples
{
    /// <summary>用 Resources.Load 加载图标。同步加载,callback 立即调用。</summary>
    public class ResourcesIconLoader : IIconLoader
    {
        public void Load(string iconPath, Action<Sprite> onLoaded)
        {
            if (onLoaded == null) return;
            if (string.IsNullOrEmpty(iconPath))
            {
                onLoaded(null);
                return;
            }
            Sprite sprite = Resources.Load<Sprite>(iconPath);
            onLoaded(sprite);
        }
    }
}
