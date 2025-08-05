using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EAP.Gateway.Infrastructure.Caching.Configuration
{
    /// <summary>
    /// Redis配置选项
    /// </summary>
    public class RedisOptions
    {
        public const string SectionName = "Redis";

        /// <summary>
        /// 连接字符串
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// 实例名称
        /// </summary>
        public string InstanceName { get; set; } = "EapGateway";

        /// <summary>
        /// 默认过期时间（分钟）
        /// </summary>
        public int DefaultExpirationMinutes { get; set; } = 10;

        /// <summary>
        /// 连接超时（毫秒）
        /// </summary>
        public int ConnectTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// 同步超时（毫秒）
        /// </summary>
        public int SyncTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// 异步超时（毫秒）
        /// </summary>
        public int AsyncTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// 连接重试次数
        /// </summary>
        public int ConnectRetry { get; set; } = 3;

        /// <summary>
        /// 连接失败时是否中止
        /// </summary>
        public bool AbortOnConnectFail { get; set; } = false;

        /// <summary>
        /// 键扫描的批处理大小
        /// </summary>
        public int KeyScanBatchSize { get; set; } = 1000;
    }
}
