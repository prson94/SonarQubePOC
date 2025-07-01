using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Mandrill;
using Mandrill.Model;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using services.domain;
using System.Text;

namespace services
{
	public abstract class BaseService
	{
		internal const int MAX_SYNCHRONOUS_API_ITEM_COUNT = 250;

		private readonly IOptions<MailProviderOptions> MailOptions;
		private readonly IOptions<QueueProviderOptions> QueueOptions;
		private readonly IOptions<StorageProviderOptions> StorageOptions;


		private QueueServiceClient _QueueClient;
		private QueueServiceClient QueueClient 
		{ 
			get 
			{
				if (_QueueClient == null)
				{ 
					_QueueClient = new QueueServiceClient(QueueOptions.Value.ConnectionString);
				}
				return _QueueClient;
			} 
		}

		protected BaseService(
			IOptions<MailProviderOptions> mailOptions,
			IOptions<QueueProviderOptions> queueOptions,
			IOptions<StorageProviderOptions> storageOptions)
		{
			MailOptions = mailOptions;
			QueueOptions = queueOptions;
			StorageOptions = storageOptions;
		}

		protected async Task CreateMailNotificationAsync(string fromName, string subject, string toEmail, string toName, string content, bool useHtml = false)
		{
			var message = new MandrillMessage();

			message.AddTo(toEmail, toName);
			message.FromEmail = MailOptions.Value.ReplyAddress;
			message.FromName = fromName;

			// Add the message properties.
			message.TrackClicks = false;
			message.TrackOpens = false;

			message.Subject = subject;
			if (!useHtml)
			{
				message.Text = content;
			}
			else
			{
				message.Html = content;
			}

			if (MailOptions.Value.SubAccount != null && MailOptions.Value.SubAccount.Trim() != string.Empty)
			{
				message.Subaccount = MailOptions.Value.SubAccount;
			}

			var api = new MandrillApi(MailOptions.Value.ApiKey);

			await api.Messages.SendAsync(message);
		}

		protected async Task CreateEventAsync<T>(string topicOrQueueName, T item, TimeSpan? initialVisibilityDelay = null)
		{
			var queue = QueueClient.GetQueueClient(topicOrQueueName);
			var response = await queue.SendMessageAsync(encodeMessage(item), initialVisibilityDelay);
		}

		protected async Task CreateEventsAsync<T>(string topicOrQueueName, IList<T> items)
		{
			var queue = QueueClient.GetQueueClient(topicOrQueueName);
			while (items.Count > 0)
			{
				var item = items[0];
				await queue.SendMessageAsync(encodeMessage(item));
				items.RemoveAt(0);
			}
		}

		protected async Task CreateStorageFileAsync(string folderName, string fileName, Stream fileStream, string contentType = "")
		{
			var blob = getBlob(folderName, fileName);
			var headers = new BlobHttpHeaders();
			headers.CacheControl = "private, max-age=0, no-cache, no-store";
			if (!string.IsNullOrEmpty(contentType))
			{ 
				headers.ContentType = contentType;
			}
			fileStream.Position = 0;
			await blob.UploadAsync(fileStream, headers);
		}
		

		private string encodeMessage<T>(T item)
		{
			var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item)));
			return encoded;
		}

		private BlobContainerClient getContainer(string name)
		{
			var client = new BlobServiceClient(StorageOptions.Value.ConnectionString);
			var container = client.GetBlobContainerClient(name);
			return container;
		}

		private BlobClient getBlob(string folderName, string fileName)
		{
			var container = getContainer(folderName);
			return container.GetBlobClient(fileName);
		}
	}
}
