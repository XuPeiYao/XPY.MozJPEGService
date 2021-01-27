using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using io = System.IO;

using CliWrap;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading;
using System.Collections.Concurrent;
using XPY.MozJPEGService.Models;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Drawing.Blurhash;
using System.IO;

namespace XPY.MozJPEGService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class MozJPEGController : Controller
    { 
        [HttpGet]
        public async Task<ConvertStatus> Get()
        {
            return new ConvertStatus() {
                InProcessing = QueueAsyncActionFilter.Queue.Count
            };
        }

        [HttpPost("blurhash")]
        public async Task<string> BlurHash(IFormFile file)
        {
            var encoder = new Encoder();
            using (var uploadStream = file.OpenReadStream())
            using (var image = Image.Load(uploadStream))
            using (var outputStream = new MemoryStream())
            {
                var rate = ((double)image.Height / image.Width);
                image.Mutate(x => x.Resize(40, (int)Math.Round(40 * rate)));

                image.SaveAsJpeg(outputStream);
                outputStream.Seek(0, SeekOrigin.Begin);

                using (var systemImage = System.Drawing.Image.FromStream(outputStream))
                {
                    return encoder.Encode(systemImage, 4, (int)Math.Round(4 * rate));
                }
            }  
        }

        [HttpPost]
        public async Task<IActionResult> Convert(
            IFormFile file,
            [FromForm] int? width,
            [FromForm] int? height,
            [FromForm] bool padMode = false,
            [FromForm] string padColor = "#000"
        ) {
            var exe = Cli.Wrap("/opt/mozjpeg/bin/cjpeg");
            
            var tempInputFilePath = Guid.NewGuid() + ".jpg";
            var resizeTempInputFilePath = Guid.NewGuid() + ".jpg";
            var tempOutputFilePath = Guid.NewGuid() + ".jpg";
            
            using(var uploadStream = file.OpenReadStream())
            using(var inputStream = io.File.Create(tempInputFilePath))
            {
                await uploadStream.CopyToAsync(inputStream);
            }

            if (width.HasValue || height.HasValue) {
                using (var image = Image.Load(tempInputFilePath))
                {
                    if (width.HasValue && height.HasValue)
                    {
                        if (padMode)
                        {
                            image.Mutate(x => x.Pad(width.Value, height.Value,Color.Parse(padColor)));
                        }
                        else
                        {
                            image.Mutate(x => x.Resize(width.Value, height.Value));
                        }
                        image.Save(resizeTempInputFilePath);
                    }
                    else if (width.HasValue)
                    {
                        image.Mutate(x => x.Resize(width.Value, (int)(image.Height * (width.Value/(double)image.Width))));
                        image.Save(resizeTempInputFilePath);
                    }
                    else if (height.HasValue)
                    {
                        image.Mutate(x => x.Resize((int)(image.Width * (height.Value / (double)image.Height)), height.Value));
                        image.Save(resizeTempInputFilePath);
                    }
                }
            }
            else
            {
                resizeTempInputFilePath = null;
            }

            await exe.WithArguments(
                new string[] {
                "-outfile", tempOutputFilePath,
                resizeTempInputFilePath ?? tempInputFilePath
                }
            ).ExecuteAsync();

            System.IO.File.Delete(tempInputFilePath);

            if(resizeTempInputFilePath != null)
            {
                System.IO.File.Delete(resizeTempInputFilePath); 
            }

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
