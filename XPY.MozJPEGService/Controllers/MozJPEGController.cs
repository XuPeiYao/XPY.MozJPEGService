using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using io = System.IO;

using CliWrap;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace XPY.MozJPEGService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MozJPEGController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Convert(IFormFile file)
        {
            var exe = Cli.Wrap("/opt/mozjpeg/bin/cjpeg");
            
            var tempInputFilePath = Guid.NewGuid() + ".jpg";
            var tempOutputFilePath = Guid.NewGuid() + ".jpg";
            using(var uploadStream = file.OpenReadStream())
            using(var inputStream = io.File.Create(tempInputFilePath))
            {
                await uploadStream.CopyToAsync(inputStream);
            }
            await exe.WithArguments(
                new string[] {
                "-outfile", tempOutputFilePath,
                tempInputFilePath
                }
            ).ExecuteAsync();

            System.IO.File.Delete(tempInputFilePath);

            var result = new System.IO.FileStream(
                tempOutputFilePath,
                System.IO.FileMode.Open,
                System.IO.FileAccess.Read,
                System.IO.FileShare.Read,
                4096,
                System.IO.FileOptions.DeleteOnClose);

            return File(result, "image/jpeg");
        }
    }
}
