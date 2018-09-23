﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQ.Configuration;
using MQ.Interfaces;
using MQ.Messages;
using Newtonsoft.Json;

namespace Client
{
    /// <summary>
    /// Воркер фоновых задач для первичной публикации документа
    /// В случае, если документ не будет опубликован (в случае возникновения внештатных ситуаций), он будет возвращен в конец
    /// очереди и увеличен счетчик повторных попыток.
    /// В случае, если вернут Processing, то документ уйдет в очередь для обновления статуса
    /// В случае, если вернут Success или Failed, то будет отправлено событие о завершении
    /// </summary>
    internal class DocumentPublishProcessingBackgroundService : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _provider;
        private readonly DocumentPublishQueueSettings _settings;

        public DocumentPublishProcessingBackgroundService(
            IOptions<DocumentPublishQueueSettings> settings,
            ILogger<DocumentPublishProcessingBackgroundService> logger,
            IServiceProvider provider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _provider = provider;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                for (var i = 0; i < _settings.ConsumersCount; i++)
                {
                    using (var scope = _provider.CreateScope())
                    {
                        var documentPublishProcessingService = scope.ServiceProvider.GetRequiredService<IDocumentPublishProcessingService>();
                        var documentPublishUpdateService = scope.ServiceProvider.GetRequiredService<IDocumentPublishUpdateService>();
                        var consumerThread = new Thread(() => MakeConsumer(documentPublishProcessingService, documentPublishUpdateService, cancellationToken))
                        {
                            IsBackground = true
                        };
                        consumerThread.Start();
                    }
                }
            }, cancellationToken);
        }

        private void MakeConsumer(IDocumentPublishProcessingService documentPublishProcessingService, IDocumentPublishUpdateService documentPublishUpdateService, CancellationToken cancellationToken)
        {
            documentPublishProcessingService.ProcessQueue(async (message, deliveryTag) =>
            {
                try
                {
                    var publishQueueMessage = JsonConvert.DeserializeObject<PublishQueueMessage>(message);
                    var documentPublicationInfo = await ProcessMessage(publishQueueMessage, cancellationToken);

                    if (documentPublicationInfo.ResultType == PublicationResultType.Processing)
                    {
                        var updateMessage = new PublishUpdateQueueMessage
                        {
                            UserId = publishQueueMessage.UserId,
                            DocumentType = publishQueueMessage.DocumentType,
                            RevisionIdentity = publishQueueMessage.RevisionIdentity,
                            UserData = publishQueueMessage.UserData,
                            RefId = documentPublicationInfo.RefId,
                            TimeStamp = DateTime.Now
                        };
                        documentPublishUpdateService.PublishMessage(updateMessage);
                    }

                    /*
                     *  need to update document status, and push notification
                     */
                    return true;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Возникла ошибка при обработке сообщения");
                    return false;
                }
            }, RaiseException);
        }

        /// <summary>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>must return http response message</returns>
        private static async Task<DocumentPublicationInfo> ProcessMessage(PublishQueueMessage message,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            /*
             * build xml
             * publish
             * get response... 
             * log to db
             * return DocumentPublicationInfo
             */
            await Task.Delay(0, cancellationToken);

            int? loadId = null;
            var result = (PublicationResultType) new Random().Next(0, 3);
            if (result == PublicationResultType.Success)
            {
                loadId = new Random().Next(1, int.MaxValue);
            }

            return new DocumentPublicationInfo(new Guid().ToString(), result, loadId);
        }

        private void RaiseException(Exception ex, ulong deliveryTag)
        {
            // обработка ошибки во время отправки результата обработки сообщения
            _logger.LogError(ex, "RaiseException" + deliveryTag);
        }
    }
}