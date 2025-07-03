using StackExchange.Redis;

namespace ConsoleApp1.Config;

public class RedisConnection
{
    private readonly ConnectionMultiplexer redis;
    private readonly int dbId;

    public RedisConnection(RedisConfig redisConfig)
    {
        var options = ConfigurationOptions.Parse(redisConfig.ConnectionString);

        if (!string.IsNullOrEmpty(redisConfig.Password))
        {
            options.Password = redisConfig.Password;
        }

        dbId = redisConfig.DatabaseId;
        redis = ConnectionMultiplexer.Connect(options);
    }

    public IDatabase GetDatabase() => redis.GetDatabase(dbId);
}