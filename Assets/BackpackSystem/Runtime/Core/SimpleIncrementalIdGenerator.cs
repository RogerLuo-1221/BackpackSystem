namespace BackpackSystem
{
    /// <summary>
    /// 简单递增整数 id 生成器。形式 "1", "2", "3", ...
    /// 仅适用于 demo / 非持久化场景。非线程安全。
    /// </summary>
    public class SimpleIncrementalIdGenerator : IInstanceIdGenerator
    {
        private int _next = 1;

        /// <summary>返回下一个递增 id,从 "1" 开始。</summary>
        public string Generate()
        {
            string id = _next.ToString();
            _next++;
            return id;
        }
    }
}
