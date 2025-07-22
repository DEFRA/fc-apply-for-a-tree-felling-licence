using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Infrastructure.Display;

public static class UrlHelperExtensions
{
    /// <summary>
    /// Generates a fully qualified URL to a relative resource path by using
    /// </summary>
    /// <param name="url">The URL helper.</param>
    /// <param name="contentPath">The relative path to a resource</param>
    /// <returns>The absolute URL.</returns>
    public static string AbsoluteContent(this IUrlHelper url, string contentPath)
    {
        var request = url.ActionContext.HttpContext.Request;
        return new Uri(new Uri($"{request.Scheme}://{request.Host.Value}"), url.Content(contentPath)).ToString();
    }
    
    /// <summary>
    /// Generates a fully qualified URL to an action method by using
    /// the specified action name, controller name and route values.
    /// </summary>
    /// <param name="url">The URL helper.</param>
    /// <param name="actionName">The name of the action method.</param>
    /// <param name="controllerName">The name of the controller.</param>
    /// <param name="routeValues">The route values.</param>
    /// <returns>The absolute URL.</returns>
    public static string? AbsoluteAction(this IUrlHelper url,
        string actionName, string controllerName, object? routeValues = null)
    {
        var scheme = url.ActionContext.HttpContext.Request.Scheme;
        return url.Action(actionName, controllerName, routeValues, scheme);
    }
}