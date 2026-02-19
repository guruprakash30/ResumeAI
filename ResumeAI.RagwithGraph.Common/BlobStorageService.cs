using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ResumeAI.RagwithGraph.Api.Model;
using ResumeAI.RagwithGraph.Common.Model;

namespace ResumeAI.RagwithGraph.Common
{
   public class BlobStorageService
   {

       private readonly string _blobStorageURI;
       private readonly string _containerName;
       private readonly ILogger<BlobStorageService> _logger;
       
       private readonly BlobContainerClient _containerClient;
       
       public BlobStorageService(ILogger<BlobStorageService> logger, IConfiguration configuration)
       {
           _logger = logger;
           _blobStorageURI = configuration["AzureStorageAccount:BlobStorage:BlobServiceUrl"];
           _containerName = configuration["AzureStorageAccount:BlobStorage:ContainerName"];
       }
       
       protected virtual BlobServiceClient ServiceClient
       {
           get
           {
               return new BlobServiceClient(new Uri(_blobStorageURI), new AzureCliCredential());
           }
       }
       
       protected BlobClient CreateBlobClient(string fileLocation)
       {
           return new BlobClient(new Uri(fileLocation),new AzureCliCredential());
       }
       
       public virtual async Task<OperationResult<FileUploadResponse>> SaveFileToBlobAsync(RequestFileUpload reqDTO, Stream stream)
       {
           OperationResult<FileUploadResponse> sResult = new();
       
           var filename = $"{reqDTO.FileId}{reqDTO.FileName}";
           
           try
           
           {
               var blobClient = ServiceClient.GetBlobContainerClient(_containerName).GetBlobClient(filename);
       
               await blobClient.UploadAsync(stream, new BlobUploadOptions
               {
                   TransferOptions = new StorageTransferOptions()
                   {
                       // For smaller files to upload in one flight, this limit helps for total file size <= InitialTransferSize.
                       InitialTransferSize = 6 * 1024 * 1024, // 6 MB
                       // Parallelism : number of concurrent workers
                       MaximumConcurrency = 8,
                       // Optional : set the maximum length of a single transfer (chunk size)
                       MaximumTransferSize = 4 * 1024 * 1024 //  4 MB
                   }
               });
       
               var properties = await blobClient.GetPropertiesAsync();
       
               return sResult.Success(new FileUploadResponse
               {
                   FileID = reqDTO.FileId,
                   FileName = reqDTO.FileName,
                   FileSizeInBytes = properties.Value.ContentLength,
                   FileLocation = Uri.UnescapeDataString(blobClient.Uri.ToString())
               });
           }
           catch (Exception ex)
           {
               await CommonLogger.LogExceptionAsync(_logger, ex, null);
               sResult.Failure(new FileUploadResponse
               {
                   FileName = reqDTO.FileName,
                   Error = $"Something went wrong, Please contact administrator"
               });
               return sResult;
           }
       
       }
       
       public virtual async Task<OperationResult<Stream>> OpenBlobReadStreamAsync(string fileLocation)
       {
           OperationResult<Stream> sResult = new();
       
           try
           {
               var blobClient = CreateBlobClient(fileLocation);
       
               if (!await blobClient.ExistsAsync())
                   throw new FileNotFoundException($"Blob '{blobClient.Name}' not found.");
       
               var stream = await blobClient.OpenReadAsync();
       
               return sResult.Success(stream);
           }
           catch (Exception ex)
           {
               await CommonLogger.LogExceptionAsync(_logger, ex, null);
               return sResult.Failure();
           }
       }

   }

}
