using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Core.Models.Global;

namespace Api.Middleware
{
    public class ValidationFilterAttribute : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = new List<string>();

                foreach (var modelState in context.ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        errors.Add(error.ErrorMessage);
                    }
                }

                var responseObj = new Response<string>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Errors = errors,
                    Success = false
                };

                context.Result = new JsonResult(responseObj)
                {
                    StatusCode = 400
                };
            }

        }
        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}