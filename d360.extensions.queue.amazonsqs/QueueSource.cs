using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Xml.Linq;

namespace d360.extensions.queue.amazonsqs
{
    public class QueueSource: IQueueSource
    {
        public string AccessKey { get; set; }
        public string AccessSecret { get; set; }
        public string QueueAddress { get; set; }

        public int CompanyID { get; set; }

        public void CreateQueue(string name)
        {
            try
            {
                var sqs = new AmazonSQSClient(AccessKey, AccessSecret);
                var q = sqs.CreateQueue(new CreateQueueRequest().WithQueueName(name));
                QueueAddress = q.CreateQueueResult.QueueUrl;
            }
            catch (Exception)
            {
            }
        }

        public void CreateQueueMessage(string message)
        {
            if (string.IsNullOrEmpty(QueueAddress)) throw new ArgumentNullException("QueueAddress");

            try
            {
                var sqs = new AmazonSQSClient(AccessKey, AccessSecret);
                var msg = new SendMessageRequest().WithMessageBody(message).WithQueueUrl(QueueAddress);
                sqs.SendMessage(msg);
            }
            catch (Exception)
            {
            }
        }

        public List<XElement> GetQueueMessages()
        {
            if (string.IsNullOrEmpty(QueueAddress)) throw new ArgumentNullException("QueueAddress");
            
            var nodes = new List<XElement>();
            
            try
            {
                var sqs = new AmazonSQSClient(AccessKey, AccessSecret);
                var result = sqs.ReceiveMessage(new ReceiveMessageRequest().WithMaxNumberOfMessages(1000000).WithQueueUrl(QueueAddress));

                foreach (var msg in result.ReceiveMessageResult.Message)
                {
                    nodes.Add(XElement.Parse(msg.Body));
                }
            }
            catch (Exception)
            {
            }

            return nodes;
        }
    }
}
