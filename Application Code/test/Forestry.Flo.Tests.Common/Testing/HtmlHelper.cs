using System.Net.Http.Headers;
using AngleSharp;
using AngleSharp.Html.Dom;
using AngleSharp.Io;
using Microsoft.Net.Http.Headers;

namespace Forestry.Flo.Tests.Common.Testing;

public static class HtmlHelper
{
    private const string AntiForgeryTokenFieldName = "__RequestVerificationToken";

    public static async Task<string> ExtractAntiForgeryTokenAsync(HttpResponseMessage response)
    {
        var doc = await GetDocumentAsync(response);
        return await ExtractAntiForgeryTokenAsync(doc);
    }

    public static Task<string> ExtractAntiForgeryTokenAsync(IHtmlDocument document)
    {
        var element = document.All.First(x => x.Matches($"input[name={AntiForgeryTokenFieldName}]"));
        var result = element.GetAttribute("value");
        return Task.FromResult(result);
    }

    
    public static async Task<IHtmlDocument> GetDocumentAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var document = await BrowsingContext.New().OpenAsync(ResponseFactory, cancellationToken);
        return (IHtmlDocument) document;

        void ResponseFactory(VirtualResponse htmlResponse)
        {
            htmlResponse
                .Address(response.RequestMessage.RequestUri)
                .Status(response.StatusCode);

            MapHeaders(response.Headers);
            MapHeaders(response.Content.Headers);

            htmlResponse.Content(content);

            void MapHeaders(HttpHeaders headers)
            {
                foreach (var header in headers)
                {
                    foreach (var value in header.Value)
                    {
                        htmlResponse.Header(header.Key, value);
                    }
                }
            }
        }
    }

    public static void ApplyForm(
        HttpRequestMessage request, 
        Dictionary<string, string> formParameters, 
        string? antiForgeryToken = null, 
        SetCookieHeaderValue? antiForgeryCookie =null)
    {
        if (!formParameters.ContainsKey(AntiForgeryTokenFieldName) && !string.IsNullOrWhiteSpace(antiForgeryToken))
            formParameters.Add(AntiForgeryTokenFieldName, antiForgeryToken);

        if (antiForgeryCookie != null)
            request.Headers.Add("Cookie", new CookieHeaderValue(antiForgeryCookie!.Name, antiForgeryCookie.Value).ToString());

        request.Content = new FormUrlEncodedContent(formParameters);
    }
}