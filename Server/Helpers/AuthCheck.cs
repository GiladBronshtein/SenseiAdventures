using System;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Filters;

namespace template.Server.Helpers
{
	public class AuthCheck : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            //first do auth check, then continue to the action itself

            int userId = 0;

            //check if the user is authenticated inside the application
            if (!context.HttpContext.User.Identity.IsAuthenticated)
            {
                //if user is not authenticated: re-check the portelem auth

                //get the host of the project
                string host = context.HttpContext.Request.Host.Host;

                var authRepo = context.HttpContext.RequestServices.GetService<AuthRepository>();

                KeyValuePair<string, string> res = await authRepo.CheckAuth(host);

                switch (res.Key)
                {
                    case "no data found":
                        context.Result = new BadRequestObjectResult("no data found");
                        return;

                    case "dev user insert fail":
                        context.Result = new BadRequestObjectResult("dev user insert fail");
                        return;

                    case "no service found":
                        //context.Result = new BadRequestObjectResult("no service found");
                        context.Result = new RedirectResult(res.Value);
                        return;
                    case "no token found":
                        //context.Result = new BadRequestObjectResult("no token found");
                        context.Result = new RedirectResult(res.Value);
                        return;
                    case "expired token":
                        //context.Result = new BadRequestObjectResult("expired token");
                        context.Result = new RedirectResult(res.Value);
                        return;
                    case "unauthorize user":
                        //context.Result = new BadRequestObjectResult("unauthorize user");
                        context.Result = new RedirectResult(res.Value);
                        return;
                    case "new user insert fail":
                        context.Result = new BadRequestObjectResult("new user insert fail");
                        return;

                    case "user Id":
                        userId = Convert.ToInt32(res.Value);
                        break;

                    default:
                        context.Result = new BadRequestObjectResult("some other error");
                        return;
                }

            }
            else
            {

                var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);

                // If the user ID claim is not present, deny access
                if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
                {
                    context.Result = new BadRequestObjectResult("unauthorize user - no claim");
                    return;
                }
                var routeUserId = context.HttpContext.Request.RouteValues["userId"]?.ToString();

                // Compare the user ID from the token with the user ID from the route
                if (!string.IsNullOrEmpty(routeUserId) && routeUserId != userIdClaim.Value)
                {
                    context.Result = new BadRequestObjectResult("unauthorize user - this is not your data");
                    return;
                }

                userId = int.Parse(userIdClaim.Value);
            }

            context.ActionArguments["authUserId"] = userId;

            var resultContext = await next();
        }

    }
}

