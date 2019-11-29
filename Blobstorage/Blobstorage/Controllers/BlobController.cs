using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Blobstorage.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;


namespace Blobstorage.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlobController : ControllerBase
    {
        private readonly IOptions<StorageConfig> config;
        public BlobController(IOptions<StorageConfig> config)
        {
            this.config = config;
        }
       
        [HttpGet("ListFiles")]
        public async Task<List<string>> ListFiles()
        {
            List<string> blobs = new List<string>();
           
                if (CloudStorageAccount.TryParse(config.Value.StorageConnection, out CloudStorageAccount storageAccount))
                {
                    CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                    CloudBlobContainer container = blobClient.GetContainerReference(config.Value.Container);

                    BlobResultSegment resultSegment = await container.ListBlobsSegmentedAsync(null);
                    foreach (IListBlobItem item in resultSegment.Results)
                    {
                    if (item.GetType() == typeof(CloudBlockBlob))
                    {
                        CloudBlockBlob blob = (CloudBlockBlob)item;
                        blobs.Add(blob.Name);
                    }
                    else if (item.GetType() == typeof(CloudPageBlob))
                    {
                        CloudPageBlob blob = (CloudPageBlob)item;
                        blobs.Add(blob.Name);
                    }
                    else if (item.GetType() == typeof(CloudBlobDirectory))
                    {
                        CloudBlobDirectory dir = (CloudBlobDirectory)item;
                        blobs.Add(dir.Uri.ToString());
                    }
                }
                }
            return blobs;
        }

        [HttpGet("DownloadFile/{fileName}")]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            MemoryStream ms = new MemoryStream();
            if (CloudStorageAccount.TryParse(config.Value.StorageConnection, out CloudStorageAccount storageAccount))
            {
                CloudBlobClient BlobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = BlobClient.GetContainerReference(config.Value.Container);

                if (await container.ExistsAsync())
                {
                    CloudBlob file = container.GetBlobReference(fileName);
                    if (await file.ExistsAsync())
                    {
                        await file.DownloadToStreamAsync(ms);
                        Stream blobStream = file.OpenReadAsync().Result;
                        return File(blobStream, file.Properties.ContentType, file.Name);
                    }
                    else
                    {
                        return Content("File does not exist");
                    }
                }
                else
                {
                    return Content("Container does not exist");
                }
            }
            else
            {
                return Content("Error opening storage");
            }
        }

        [HttpGet("DownloadFile/externaluser/{fileName}")]
        public IActionResult DownloadFileForExternalUser(string fileName)
        {
            if (CloudStorageAccount.TryParse(config.Value.StorageConnection, out CloudStorageAccount storageAccount))
            {
                SharedAccessBlobPolicy adHocPolicy = new SharedAccessBlobPolicy()
                {
                    SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(1),
                    Permissions =  SharedAccessBlobPermissions.Read
                };
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(config.Value.Container);
               // string sasContainerToken = container.GetSharedAccessSignature(adHocPolicy);
                ICloudBlob blob = container.GetBlockBlobReference(fileName);
                string sasContainerToken = blob.GetSharedAccessSignature(adHocPolicy);
                Uri fileSasUri = new Uri(blob.Uri + sasContainerToken);
                return Ok(fileSasUri.ToString());
            }
            else
            {
                return Content("file not found");
            }
        }
    }
}