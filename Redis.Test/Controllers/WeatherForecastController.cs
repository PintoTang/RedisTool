using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RedisTool;
using System.Threading;

namespace Redis.Test.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly object LockObj = new object();

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IRedisHelper redisHelper;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IRedisHelper _redisHelper)
        {
            _logger = logger;
            redisHelper = _redisHelper;
        }

        /// <summary>
        /// 单机锁应用
        /// 1.缓存击穿是指某一个Key失效瞬间大量并发直接访问数据库(用互斥锁或者设置热点数据永不过期)
        /// 2.缓存穿透是指缓存和数据库中都没有数据，而用户不断发起请求(这种情况要么在接口层做校验要么使用布隆过滤器)
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [HttpGet("StandaloneLock")]
        public string Get(string key)
        {
            //从Redis取数据
            var result = redisHelper.Get(key);
            if (result == null)
            {
                try
                {
                    //获取锁
                    if (Monitor.TryEnter(LockObj))
                    {
                        //从数据库取数据
                        result = "Select * From DbTable";
                        if (result != null)
                        {
                            //写入Redis缓存
                            redisHelper.Insert(key, result);
                        }
                        //释放锁
                        Monitor.Exit(LockObj);
                    }
                    else
                    {
                        Thread.Sleep(100);
                        result = redisHelper.Get(key);
                    }
                }
                finally
                {
                    if (Monitor.IsEntered(LockObj))
                        Monitor.Exit(LockObj);
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// 分布式锁应用
        /// </summary>
        /// <returns></returns>
        [HttpGet("DistributedLock")]
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
                    msg = "success";
                }
                else
                    msg = "nolock";
            }
            catch
            {
                msg = "fail";
            }
            finally
            {
                //释放锁
                redisHelper.DelLock("LockKey");
            }
            return msg;
        }

    }
}
