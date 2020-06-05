using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Web.Http;
using Microsoft.Azure;
using Azure.Storage.Files.Shares;
using Azure;

namespace NetworkShares.WebApplication.Controllers
{
    [RoutePrefix("api/home")]
    public class HomeController : ApiController
    {
        //string _storagePath = ConfigurationManager.AppSettings["toUpload"].ToString();
        
        //string _shareDirectoryName = CloudConfigurationManager.GetSetting("ShareDirectoryName");

        [HttpGet]
        [Route("Status")]
        public IHttpActionResult CheckStatus()
        {
            return Ok(new { status = "Ok" });
        }

        [HttpPost]
        [Route("File")]
        public async Task<IHttpActionResult> PostFile()
        {
            //return await SaveFile(@"\\sharedstoragepocaccount.file.core.windows.net\sharedstoragepocaccount-fs02");
            return await SaveFile(CloudConfigurationManager.GetSetting("toUpload"));
        }

        public async Task<IHttpActionResult> SaveFile(string diskFolderPath)
        {
            var path = Path.GetTempPath();

            if (!Request.Content.IsMimeMultipartContent("form-data"))
            {
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.UnsupportedMediaType));
            }

            MultipartFormDataStreamProvider streamProvider = new MultipartFormDataStreamProvider(path);

            await Request.Content.ReadAsMultipartAsync(streamProvider);

            foreach (MultipartFileData fileData in streamProvider.FileData)
            {
                string fileName = "";
                if (string.IsNullOrEmpty(fileData.Headers.ContentDisposition.FileName))
                {
                    fileName = Guid.NewGuid().ToString();
                }
                fileName = fileData.Headers.ContentDisposition.FileName;
                if (fileName.StartsWith("\"") && fileName.EndsWith("\""))
                {
                    fileName = fileName.Trim('"');
                }
                if (fileName.Contains(@"/") || fileName.Contains(@"\"))
                {
                    fileName = Path.GetFileName(fileName);
                }

                var newFileName = Path.Combine(diskFolderPath, fileName);
                var fileInfo = new FileInfo(newFileName);
                if (fileInfo.Exists)
                {
                    fileName = fileInfo.Name.Replace(fileInfo.Extension, "");
                    fileName = fileName + (new Random().Next(0, 10000)) + fileInfo.Extension;

                    newFileName = Path.Combine(diskFolderPath, fileName);
                }

                if (!Directory.Exists(fileInfo.Directory.FullName))
                {
                    Directory.CreateDirectory(fileInfo.Directory.FullName);
                }

                File.Move(fileData.LocalFileName, newFileName);
                //File.Copy(fileData.LocalFileName, newFileName);

                //ShareClient share = new ShareClient(
                //    CloudConfigurationManager.GetSetting("StorageConnectionString"),
                //    CloudConfigurationManager.GetSetting("ShareName"));
                //ShareDirectoryClient directory = share.GetDirectoryClient(
                //    CloudConfigurationManager.GetSetting("ShareDirectoryName"));

                //// Get a reference to a file and upload it
                //ShareFileClient file = directory.GetFileClient(fileName);
                //using (FileStream stream = File.OpenRead(Path.Combine(fileData.LocalFileName, newFileName)))
                //{
                //    file.Create(stream.Length);
                //    file.UploadRange(new HttpRange(0, stream.Length), stream);
                //}

                return Json(new { link = $"{diskFolderPath}\\{fileName}" });
            }

            return BadRequest();
        }
    }
}
