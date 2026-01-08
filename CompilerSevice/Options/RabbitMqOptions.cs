namespace CompilerSevice
{
    public sealed class RabbitMqOptions
    {
        public string Host { get; set; } = "rabbitmq";
        public string VirtualHost { get; set; } = "/";
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
    }
}
