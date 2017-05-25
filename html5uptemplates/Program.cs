using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using System.Net.Http;
using System.IO.Compression;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Configuration;
using System.IO;

namespace html5uptemplates
{
    // To learn more about Microsoft Azure WebJobs SDK, please see https://go.microsoft.com/fwlink/?LinkID=320976
    class Program
    {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        static void Main()
        {
            var config = new JobHostConfiguration();

            if (config.IsDevelopment)
            {
                config.UseDevelopmentSettings();
            }


            DownloadTemplate().GetAwaiter().GetResult();


            var host = new JobHost();
            // The following code ensures that the WebJob will be running continuously
            host.RunAndBlock();


        }

        private static async Task Proxy()
        {
            var path = Path.Combine(Environment.ExpandEnvironmentVariables("%HOME%"), @"site\wwwroot");
            var installProxy = Path.Combine(path, "proxies.json.install");
            var liveProxy = Path.Combine(path, "proxies.json.live");
            if (File.Exists(liveProxy))
            {
                File.Move(Path.Combine(path, "proxies.json"), installProxy);
                File.Move(liveProxy, Path.Combine(path, "proxies.json"));
            }
        }

        private static async Task Restore()
        {
            var path = Path.Combine(Environment.ExpandEnvironmentVariables("%HOME%"), @"site\wwwroot");
            var installProxy = Path.Combine(path, "proxies.json.install");
            var liveProxy = Path.Combine(path, "proxies.json.live");
            if (File.Exists(installProxy))
            {
                File.Move(Path.Combine(path, "proxies.json"), liveProxy);
                File.Move(installProxy, Path.Combine(path, "proxies.json"));
            }
        }

        private static async Task DownloadTemplate()
        {
            var client = new HttpClient();
            var storage = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["AzureWebJobsStorage"]);

            var blobClient = storage.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("tes2t");
            container.CreateIfNotExists(BlobContainerPublicAccessType.Blob);

            //var zip = await client.GetStreamAsync("https://html5up.net/massively/download");
            using (var zip = File.OpenRead(@"C:\Users\blasi\Downloads\html5up-massively.zip"))
            {
                var taskList = new List<Task>();
                using (var ms = new MemoryStream())
                {
                    await zip.CopyToAsync(ms);
                    using (var s = new ZipArchive(ms, ZipArchiveMode.Read))
                    {
                        foreach (var e in s.Entries)
                        {
                            var file = container.GetBlockBlobReference(e.FullName);
                            file.Properties.ContentType = Path.GetExtension(e.FullName) == ".css" ? "text/css" : "text/html";
                            taskList.Add(file.UploadFromStreamAsync(e.Open()));
                        }


                        Task.WaitAll(taskList.ToArray());

                    }
                }
            }
        }
    }
}
