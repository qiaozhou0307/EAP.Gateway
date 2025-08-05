namespace EAP.Gateway.Infrastructure.Configuration;

/// <summary>
/// RabbitMQ配置
/// </summary>
public class RabbitMQConfig
{
    public const string SectionName = "RabbitMQ";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public RabbitMQExchanges Exchanges { get; set; } = new();
    public RabbitMQQueues Queues { get; set; } = new();

    public string GetConnectionString()
    {
        return $"amqp://{UserName}:{Password}@{HostName}:{Port}{VirtualHost}";
    }
}

public class RabbitMQExchanges
{
    public string Commands { get; set; } = "eap.commands";
    public string Events { get; set; } = "eap.events";
}

public class RabbitMQQueues
{
    public string CommandQueue { get; set; } = "eap.command.queue";
    public string CommandAckQueue { get; set; } = "eap.command.ack.queue";
}
