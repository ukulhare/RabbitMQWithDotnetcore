﻿using System;
using Microsoft.Extensions.Options;

namespace MQ.Configuration.Base
{
    public class BaseQueueSettings
    {
        public BaseQueueSettings () { }

        public BaseQueueSettings(IOptions<ConnectionStrings> connectionStrings)
        {
            ConnectionString = connectionStrings?.Value.RabbitMQ ?? throw new ArgumentNullException(nameof(connectionStrings));
        }

        /// <summary>
        /// Строка подключения к базе данных
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Количество воркеров
        /// </summary>
        public int ConsumersCount { get; set; } = 5;

        /// <summary>
        /// Название очереди
        /// </summary>
        public string QueueName { get; set; }

        /// <summary>
        /// Название обменника
        /// </summary>
        public string ExchangeName { get; set; }

        /// <summary>
        /// Название ключа маршрута
        /// </summary>
        public string RoutingKey { get; set; } = "";

        /// <summary>
        /// Тип обменника
        /// </summary>
        public string ExchangeType { get; set; }

        /// <summary>
        /// Количество попыток процессить сообщение в случае возникновения ошибок
        /// </summary>
        public int MaxRetryCount { get; set; }

        /// <summary>
        /// Через сколько повторить
        /// </summary>
        /// <remarks>В секундах</remarks>
        public int RetryDelay { get; set; }

        /// <summary>
        /// Применять повторение для сообщений
        /// </summary>
        public bool WithDelay { get; set; }
    }
}