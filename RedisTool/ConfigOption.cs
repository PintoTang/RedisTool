namespace RedisTool
{
    public class ConfigOption
    {
        //配置节点名称
        public const string RedisConfig = "Redis";

        public string DbName { set; get; }
        public string ConnectionString { set; get; }
        public int DbIndex { set; get; }
    }
}
