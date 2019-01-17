using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace igx.PostFusionJob
{
    public class FusionProcessingData
    {
        public int CompanyID { get; set; }
        public int FusionID { get; set; }
        public string LogFileName { get; set; }
    }

    public class FusionQueueManager
    {
        public string AZURE_STORAGE_NAME = ConfigurationManager.AppSettings["AzureStorageName"];
        public string AZURE_STORAGE_KEY = ConfigurationManager.AppSettings["AzureStorageKey"];

        private CloudQueueClient _queueClient;
        private string _queueName;

        public FusionQueueManager(string queueName)
        {
            var acctName = AZURE_STORAGE_NAME;
            var keyValue = AZURE_STORAGE_KEY;
            _queueName = queueName;
            CloudStorageAccount storageAccount = new CloudStorageAccount(new StorageCredentials(acctName, keyValue), true);
            _queueClient = storageAccount.CreateCloudQueueClient();
        }

        // Puts a serialized fixit onto the queue.
        public async Task SendMessageAsync(FusionProcessingData fusion)
        {
            CloudQueue queue = _queueClient.GetQueueReference(_queueName);
            await queue.CreateIfNotExistsAsync();

            var fusionJson = JsonConvert.SerializeObject(fusion);
            CloudQueueMessage message = new CloudQueueMessage(fusionJson);

            await queue.AddMessageAsync(message);
        }
    }

    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            var queueName = txtQueueName.Text;
            var fusionID = Convert.ToInt32(txtFusionID.Text);
            var fileName = txtFileName.Text;
            var companyID = Convert.ToInt32(txtCompanyID.Text);            

            if(string.IsNullOrEmpty(queueName))
            {
                MessageBox.Show("Please specify a queue name.");

                return;
            }

            if (string.IsNullOrEmpty(fileName))
            {
                MessageBox.Show("Please specify a file to process.");

                return;
            }

            if (companyID < 0)
            {
                MessageBox.Show("Please specify valid company ID.");

                return;
            }

            if (fusionID < 0)
            {
                MessageBox.Show("Please specify valid company ID.");

                return;
            }

            var fusionQueue = new FusionQueueManager(queueName);

            await fusionQueue.SendMessageAsync(new FusionProcessingData
            {
                CompanyID = companyID,
                FusionID = fusionID,
                LogFileName = fileName
            });

        }
    }
}
