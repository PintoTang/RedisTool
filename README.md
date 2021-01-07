# RedisTool
 Redis工具库

1.失效时间加个随机值，防止雪崩,这种适合定时刷新热点的场景
    
    /// <summary>
    /// 将指定键的对象添加到缓存中(设定过期时刻)，这种适合定时刷新热点的场景
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="data"></param>
    /// <param name="cacheTime">失效时间加个随机值，防止雪崩(即大面积缓存失效)</param>
    public bool Insert<T>(string key, T data, DateTime cacheTime)
    {
        try
        {
            var timeSpan = cacheTime.AddSeconds(new Random().Next(1000)) - DateTime.Now;
            int expireSeconds = timeSpan.Seconds;
            var jsonData = GetJsonData<T>(data, expireSeconds, false);
            database.StringSet(key, jsonData, timeSpan);
            return true;
        }
        catch
        {
            return false;
        }
    }

2.增加分布式锁

    /// <summary>
    /// 获取分布式锁
    /// </summary>
    /// <param name="key">锁key</param>
    /// <param name="lockExpirySeconds">锁自动超时时间(秒)</param>
    /// <param name="waitLockMs">等待锁时间(秒)</param>
    /// <returns></returns>
    public bool Lock(string key, int lockExpirySeconds = 10, double waitLockSeconds = 0)
    {
        //循环间隔50毫秒
        int waitIntervalMs = 50;
        string lockKey = "DistributedLock:" + key;
        DateTime begin = DateTime.Now;
        //循环获取取锁
        while (true)
        {
            if (database.StringSet(lockKey, new byte[] { 1 }, new TimeSpan(0, 0, lockExpirySeconds), When.NotExists, CommandFlags.None))
                return true;
            //不等待锁直接返回
            if (waitLockSeconds == 0) break;
            //超过等待时间，则不再等待
            if ((DateTime.Now - begin).TotalSeconds >= waitLockSeconds) break;
            Thread.Sleep(waitIntervalMs);
        }
        return false;
    }
    
3.分布式锁应用

    public string DistributedLock()
    {
        string msg = "";
        try
        {
            //取锁,设置key10秒后失效，最大等待锁5秒
            if (redisHelper.Lock("LockKey", 10, 5))
            {
                //取到锁,执行具体业务
                //todo
                msg = "成功";
            }
            else
                msg = "未获取到锁";
        }
        catch
        {
            msg = "失败";
        }
        finally
        {
            //释放锁
            redisHelper.DelLock("LockKey");
        }
        return msg;
    }


