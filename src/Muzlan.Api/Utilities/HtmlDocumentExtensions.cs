using System;

using AngleSharp.Html.Dom;

namespace Muzlan.Api.Utilities
{
    internal static class HtmlDocumentExtensions
    {
        public static bool IsVipPage(this IHtmlDocument document)
        {
            return document.QuerySelector("div.page-content div.alert-danger") != null
                || document.QuerySelector("div.page-content div.order-form") != null;
        }
    }
}
