using Newtonsoft.Json;
using StackExchange.Redis;
using System;

namespace RedisTool
{
    public class RedisHelper : IRedisHelper
    {
        JsonSerializerSettings jsonConfig = new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };
        IDatabase database;
        ConfigOption config;

        class CacheObject<T>
        {
            /// <summary>
            /// 过期时间(秒)
            /// </summary>
            public int ExpireSeconds { get; set; }
            /// <summary>
            /// 是否强制过期
            /// </summary>
            public bool ForceExpire { get; set; }
            /// <summary>
            /// 存储对象
            /// </summary>
            public T Value { get; set; }
        }

        public RedisHelper(ConfigOption _config)
        {
            config = _config;
            RedisClient client = new RedisClient();
            if (config.ConnectionString == null || string.IsNullOrWhiteSpace(config.ConnectionString))
                throw new ApplicationException("配置文件中未找到RedisServer的有效配置！");
            database = client.GetDatabase(config.ConnectionString, config.DbIndex);
        }

        public object Get(string key)
        {
            return Get<object>(key);
        }

        /// <summary>
        /// 获取指定键的值,并重新设置过期时间
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T Get<T>(string key)
        {
            var cacheValue = database.StringGet(key);
            var value = default(T);
            if (!cacheValue.IsNull)
            {
                var cacheObject = JsonConvert.DeserializeObject<CacheObject<T>>(cacheValue, jsonConfig);
                if (cacheObject.ForceExpire)
                    database.KeyExpire(key, new TimeSpan(0, 0, cacheObject.ExpireSeconds));
                value = cacheObject.Value;
            }
            return value;
        }

        /// <summary>
        /// 将指定键的对象添加到缓存中(不设定过期)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        public void Insert(string key, object data)
        {
            var jsonData = GetJsonData(data, -1, false);
            database.StringSet(key, jsonData);
        }

        /// <summary>
        /// 将指定键的对象添加到缓存中(设定过期秒数)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <param name="cacheTime">过期秒数</param>
        public void Insert(string key, object data, int cacheTime)
        {
            var timeSpan = TimeSpan.FromSeconds(cacheTime);
            var jsonData = GetJsonData(data, cacheTime, true);
            database.StringSet(key, jsonData, timeSpan);
        }

        /// <summary>
        /// 将指定键的对象添加到缓存中(设定过期时刻)，这种适合定时刷新热点的场景
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <param name="cacheTime">失效时间加个随机值，防止雪崩(即大面积缓存失效)</param>
        public void Insert(string key, object data, DateTime cacheTime)
        {
            var timeSpan = cacheTime.AddSeconds(new Random().Next(1000)) - DateTime.Now;
            int expireSeconds = timeSpan.Seconds;
            var jsonData = GetJsonData(data, expireSeconds, false);
            database.StringSet(key, jsonData, timeSpan);
        }

        /// <summary>
        /// 将指定键的对象添加到缓存中(不设定过期)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="data"></param>
        public void Insert<T>(string key, T data)
        {
            var jsonData = GetJsonData<T>(data, -1, false);
            database.StringSet(key, jsonData);
        }

        /// <summary>
        /// 将指定键的对象添加到缓存中(设定过期秒数)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <param name="cacheTime">过期秒数</param>
        public void Insert<T>(string key, T data, int cacheTime)
        {
            var timeSpan = TimeSpan.FromSeconds(cacheTime);
            var jsonData = GetJsonData<T>(data, cacheTime, true);
            database.StringSet(key, jsonData, timeSpan);
        }

        /// <summary>
        /// 将指定键的对象添加到缓存中(设定过期时刻)，这种适合定时刷新热点的场景
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <param name="cacheTime">失效时间加个随机值，防止雪崩(即大面积缓存失效)</param>
        public void Insert<T>(string key, T data, DateTime cacheTime)
        {
            var timeSpan = cacheTime.AddSeconds(new Random().Next(1000)) - DateTime.Now;
            int expireSeconds = timeSpan.Seconds;
            var jsonData = GetJsonData<T>(data, expireSeconds, false);
            database.StringSet(key, jsonData, timeSpan);
        }

        /// <summary>
        /// 序列化存储对象
        /// </summary>
        /// <param name="data"></param>
        /// <param name="cacheTime"></param>
        /// <param name="dateTime"></param>
        /// <param name="forceExpire"></param>
        /// <returns></returns>
        string GetJsonData(object data, int cacheTime, bool forceExpire)
        {
            var cacheObject = new CacheObject<object>() { Value = data, ExpireSeconds = cacheTime, ForceExpire = forceExpire };
            return JsonConvert.SerializeObject(cacheObject, jsonConfig);//序列化对象
        }

        /// <summary>
        /// 序列化存储对象
        /// </summary>
        /// <param name="data"></param>
        /// <param name="cacheTime"></param>
        /// <param name="dateTime"></param>
        /// <param name="forceExpire"></param>
        /// <returns></returns>
        string GetJsonData<T>(T data, int cacheTime, bool forceExpire)
        {
            var cacheObject = new CacheObject<T>() { Value = data, ExpireSeconds = cacheTime, ForceExpire = forceExpire };
            return JsonConvert.SerializeObject(cacheObject, jsonConfig);//序列化对象
        }

        /// <summary>
        /// 删除Key
        /// </summary>
        /// <param name="key"></param>
        public void Remove(string key)
        {
            database.KeyDelete(key, CommandFlags.None);
        }

        /// <summary>
        /// 判断Key是否存在
        /// </summary>
        public bool Exists(string key)
        {
            return database.KeyExists(key);
        }

    }
}
