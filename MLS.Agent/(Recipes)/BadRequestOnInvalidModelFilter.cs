using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Recipes
{
    internal class BadRequestOnInvalidModelFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                context.Result = new BadRequestObjectResult(
                    context.ModelState
                           .Values
                           .SelectMany(e => e.Errors
                                             .Select(ee => ee.ErrorMessage)));
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}
