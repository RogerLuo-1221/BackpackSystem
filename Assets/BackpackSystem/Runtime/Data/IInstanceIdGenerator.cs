namespace BackpackSystem
{
    /// <summary>实例 id 生成策略。同一 generator 应保证生成的 id 互不重复。</summary>
    public interface IInstanceIdGenerator
    {
        /// <summary>生成一个新的实例 id 字符串。同一 generator 不应重复返回相同 id。</summary>
        string Generate();
    }
}
