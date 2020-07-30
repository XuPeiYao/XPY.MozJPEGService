using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace XPY.MozJPEGService
{
    public class QueueAsyncActionFilter : IAsyncActionFilter
    {
        public static ConcurrentQueue<string> Queue { get; set; } = new ConcurrentQueue<string>();
        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        { 
            if ((context.ActionDescriptor as ControllerActionDescriptor)?.ControllerName == "MozJPEG" &&
                (context.ActionDescriptor as ControllerActionDescriptor)?.ActionName == nameof(Convert))
            {
                Queue.Enqueue(context.HttpContext.TraceIdentifier);

                while (Queue.FirstOrDefault() != context.HttpContext.TraceIdentifier)
                {
                    await Task.Delay(500);
                }

                await next();

                Queue.TryDequeue(out _);
                return;
            }

            await next();            
        }
    }
}
