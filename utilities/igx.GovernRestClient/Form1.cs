using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace igx.GovernRestClient
{
    public partial class Form1 : Form
    {
        public string FileName { get { return "UserInfo.json"; } }

        public Form1()
        {
            InitializeComponent();

            _CachedUserInfo = getUserInfoFile();
            ApiKey.Text = _CachedUserInfo.ApiKey;
            ApiSecret.Text = _CachedUserInfo.ApiSecret;
            UriText.Text = _CachedUserInfo.ApiUri;
        }

        UserInfo _CachedUserInfo;

        UserInfo getUserInfoFile()
        {
            UserInfo info;
            if (File.Exists(FileName))
            {
                info = JsonConvert.DeserializeObject<UserInfo>(File.ReadAllText(@"UserInfo.json"));
            }
            else
            {
                info = new UserInfo();
                File.WriteAllText(FileName, JsonConvert.SerializeObject(_CachedUserInfo));
            }
            return info;
        }

        private void SelectJsonButton_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                JsonRequestFilePath.Text = openFileDialog1.FileName;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var method = Method.SelectedItem;
            bool valid = true;
            ErrorText.Text = "";
            if (string.IsNullOrEmpty(ApiKey.Text))
            {
                valid = false;
                ErrorText.Text = "API Key is empty; ";
            }
            if (string.IsNullOrEmpty(ApiSecret.Text))
            {
                valid = false;
                ErrorText.Text = "API Secret is empty; ";
            }
            if (string.IsNullOrEmpty(UriText.Text))
            {
                valid = false;
                ErrorText.Text = "Uri is empty; ";
            }
            if (valid)
            {
                HttpWebRequest req = HttpWebRequest.CreateHttp(UriText.Text);
                req.Accept = "application/json";
                req.Headers.Add(HttpRequestHeader.Authorization, $"{ApiKey.Text};{ApiSecret.Text}");
                req.MediaType = "application/json";

                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"{ApiKey.Text};{ApiSecret.Text}");
                string jsonResponse;
                JToken parsedJson;
                string beautified;
                try
                {
                    switch (method)
                    {
                        case "GET":
                            jsonResponse = client.GetStringAsync(UriText.Text).Result;
                            parsedJson = JToken.Parse(jsonResponse);
                            beautified = parsedJson.ToString(Formatting.Indented);
                            ApiResponseJson.Text = beautified;
                            break;
                        case "DELETE":
                            if (!string.IsNullOrEmpty(openFileDialog1.FileName))
                            {
                                var json = File.ReadAllText(openFileDialog1.FileName);
                                HttpRequestMessage request = new HttpRequestMessage
                                {
                                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                                    Method = HttpMethod.Delete,
                                    RequestUri = new Uri(UriText.Text)
                                };
                                jsonResponse = client.SendAsync(request).Result.Content.ReadAsStringAsync().Result;
                                parsedJson = JToken.Parse(jsonResponse);
                                beautified = parsedJson.ToString(Formatting.Indented);
                                ApiResponseJson.Text = beautified;
                            }
                            else
                            {
                                ErrorText.Text = "No JSON file selected; ";
                            }
                            break;
                        case "POST":
                            if (!string.IsNullOrEmpty(openFileDialog1.FileName))
                            {
                                if (HttpChunked.Checked)
                                {
                                    client.DefaultRequestHeaders.TransferEncodingChunked = true;
                                    client.Timeout = new TimeSpan(0, 1, 0, 0, 0);

                                    using (var content = new MultipartContent())
                                    {
                                        content.Add(
                                            new StreamContent(
                                                openFileDialog1.OpenFile()
                                            )
                                        );

                                        var message = client.PostAsync(UriText.Text, content).Result;
                                        jsonResponse = message.Content.ReadAsStringAsync().Result;
                                    }
                                }
                                else
                                {
                                    var json = File.ReadAllText(openFileDialog1.FileName);
                                    HttpRequestMessage request = new HttpRequestMessage
                                    {
                                        Content = new StringContent(json, Encoding.UTF8, "application/json"),
                                        Method = HttpMethod.Post,
                                        RequestUri = new Uri(UriText.Text)
                                    };
                                    jsonResponse = client.SendAsync(request).Result.Content.ReadAsStringAsync().Result;
                                }

                                parsedJson = JToken.Parse(jsonResponse);
                                beautified = parsedJson.ToString(Formatting.Indented);
                                ApiResponseJson.Text = beautified;
                            }
                            else
                            {
                                ErrorText.Text = "No JSON file selected; ";
                            }
                            break;
                        case "PUT":
                            if (!string.IsNullOrEmpty(openFileDialog1.FileName))
                            {
                                if (HttpChunked.Checked)
                                {
                                    client.DefaultRequestHeaders.TransferEncodingChunked = true;
                                    client.Timeout = new TimeSpan(0, 1, 0, 0, 0);

                                    using (var content = new MultipartContent())
                                    {
                                        content.Add(
                                            new StreamContent(
                                                openFileDialog1.OpenFile()
                                            )
                                        );

                                        var message = client.PutAsync(UriText.Text, content).Result;
                                        jsonResponse = message.Content.ReadAsStringAsync().Result;
                                    }
                                }
                                else
                                {
                                    var json = File.ReadAllText(openFileDialog1.FileName);
                                    HttpRequestMessage request = new HttpRequestMessage
                                    {
                                        Content = new StringContent(json, Encoding.UTF8, "application/json"),
                                        Method = HttpMethod.Put,
                                        RequestUri = new Uri(UriText.Text)
                                    };
                                    jsonResponse = client.SendAsync(request).Result.Content.ReadAsStringAsync().Result;
                                }

                                parsedJson = JToken.Parse(jsonResponse);
                                beautified = parsedJson.ToString(Formatting.Indented);
                                ApiResponseJson.Text = beautified;
                            }
                            else
                            {
                                ErrorText.Text = "No JSON file selected; ";
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    ErrorText.Text = ex.Message;
                }
            }
        }

        private void saveUserDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _CachedUserInfo.ApiKey = ApiKey.Text;
            _CachedUserInfo.ApiSecret = ApiSecret.Text;
            _CachedUserInfo.ApiUri = UriText.Text;
            File.WriteAllText(FileName, JsonConvert.SerializeObject(_CachedUserInfo));
        }
    }
}
