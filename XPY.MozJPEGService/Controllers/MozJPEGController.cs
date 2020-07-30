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
using XPY.MozJPEGService.Models;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace XPY.MozJPEGService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MozJPEGController : Controller
    {
        public static long InProcessing = 0;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if ((context.ActionDescriptor as ControllerActionDescriptor)?.ControllerName == "MozJPEG" &&
                (context.ActionDescriptor as ControllerActionDescriptor)?.ActionName == nameof(Convert))
            {
                Interlocked.Increment(ref InProcessing);
            }
            base.OnActionExecuting(context);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if ((context.ActionDescriptor as ControllerActionDescriptor)?.ControllerName == "MozJPEG" &&
                (context.ActionDescriptor as ControllerActionDescriptor)?.ActionName == nameof(Convert))
            {
                Interlocked.Decrement(ref InProcessing);
            }
            base.OnActionExecuted(context);
        }

        [HttpGet]
        public async Task<ConvertStatus> Get()
        {
            return new ConvertStatus() {
                InProcessing = InProcessing
            };
        }

        [HttpPost]
        public async Task<IActionResult> Convert(
            IFormFile file,
            [FromForm] int? width,
            [FromForm] int? height,
            [FromForm] bool padMode = false,
            [FromForm] string padColor = "#000"
        ) {
            await Task.Delay(1000 * 5);
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
