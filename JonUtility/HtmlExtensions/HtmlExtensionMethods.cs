using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using HtmlAgilityPack;
using System.Text;
using System.Linq;
using System.Net;

namespace JonUtility.HtmlExtensions
{
    public class LinkInfo
    {
        public string Link { get; }
        public string Text { get; }
        public string Title { get; }

        public LinkInfo(string link, string Text, string title = "")
        {
            this.Link = link;
            this.Text = Text;
            this.Title = title;
        }

        public bool IsEmpty
        {
            get { return string.IsNullOrEmpty(this.Link) && string.IsNullOrEmpty(this.Text); }
        }
    }

    public class ImageInfo
    {
        public string Src { get; }
        public string Alt { get; }

        public ImageInfo(string src, string alt)
        {
            this.Src = src;
            this.Alt = alt;
        }

        public bool IsEmpty
        {
            get { return string.IsNullOrEmpty(this.Src) && string.IsNullOrEmpty(this.Alt); }
        }
    }

    public static class HtmlExtensionMethods
    {        
        public static string GetSiblingText(this HtmlNode node)
        {
            if (node == null)
                return string.Empty;
            StringBuilder builder = new StringBuilder();
            do
            {
                node = node.NextSibling;
                if (node == null)
                    break; // TODO: might not be correct. Was : Exit Do

                dynamic text = System.Web.HttpUtility.HtmlDecode(node.InnerText);
                if (!string.IsNullOrEmpty(text))
                {
                    builder.Append(text);
                }
            } while (true);

            return builder.ToString();
        }

        public static IEnumerable<HtmlNode> SelectNodesWithDefault(this HtmlNode node, string xpath)
        {
            var nodes = node.SelectNodes(xpath);
            if (nodes != null)
            {
                return nodes;
            }
            else
            {
                return Enumerable.Empty<HtmlNode>();
            }
        }

        public static LinkInfo GetAnchorInfo(this HtmlNode anchor)
        {
            var aText = System.Web.HttpUtility.HtmlDecode(anchor.InnerText).Trim();
            var hrefAttribute = anchor.Attributes["href"];
            var linkUrl = hrefAttribute == null || string.IsNullOrEmpty(hrefAttribute.Value) ? string.Empty : hrefAttribute.Value;
            var titleAttribute = anchor.Attributes["title"];
            var titleText = titleAttribute == null || string.IsNullOrEmpty(titleAttribute.Value) ? string.Empty : titleAttribute.Value;
            return new LinkInfo(link: linkUrl, Text: aText, title: titleText);
        }

        public static string ValueOrDefault(this HtmlAttribute @this, string defaultValue = null)
        {
            return @this != null ? @this.Value : defaultValue;
        }

        public static ImageInfo GetImageInfo(this HtmlNode img)
        {
            return new ImageInfo(src: img.Attributes["src"].ValueOrDefault(""), alt: img.Attributes["alt"].ValueOrDefault(""));
        }

        public static bool IsJapaneseText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            return text.Any(c => c.IsJapanese());
        }

        public static bool IsKoreanText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            return text.Any(c => c.IsKorean());
        }

        public static bool IsLatinText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            return text.All(c => c.IsLatin());
        }

        public static string SaveLine(Cookie cookie)
        {
            var builder = new StringBuilder();
            builder.Append(cookie.Name);
            builder.Append("=");
            builder.Append(cookie.Value);

            if (cookie.Expires != null)
            {
                builder.Append(";");
                builder.Append("expires=");
                builder.Append(cookie.Expires.ToString());
            }

            if (!string.IsNullOrEmpty(cookie.Path))
            {
                builder.Append(";");
                builder.Append("path=");
                builder.Append(cookie.Path);
            }

            if (!string.IsNullOrEmpty(cookie.Domain))
            {
                builder.Append(";");
                builder.Append("domain=");
                builder.Append(cookie.Domain);
            }

            if (cookie.HttpOnly)
            {
                builder.Append(";");
                builder.Append("HttpOnly");
            }

            if (cookie.Secure)
            {
                builder.Append(";");
                builder.Append("Secure");
            }

            return builder.ToString();
        }

        // containerTd.SelectSingleNode(".//*[text()[contains(.,'Serialization:')]]")

        public static HtmlNode SelectSingleNodeByText(this HtmlNode @this, string text, string elementType = "*")
        {
            var xpath = String.Format(".//{0}[text()[contains(.,'{1}')]]", elementType, text);
            return @this.SelectSingleNode(xpath);
        }


        public static HtmlNode SelectSingleNodeByClass(this HtmlNode @this, string className, string elementType = "*")
        {
            var xpath = String.Format(".//{0}[contains(@class,'{1}')]", elementType, className);
            return @this.SelectSingleNode(xpath);
        }

        public static IEnumerable<HtmlNode> SelectNodesByClass(this HtmlNode @this, string className, string elementType = "*")
        {
            var xpath = String.Format(".//{0}[contains(@class,'{1}')]", elementType, className);
            return @this.SelectNodes(xpath) ?? Enumerable.Empty<HtmlNode>();
        }

        public static bool HasAttribute(this HtmlNode @this, string attributeName, string attributeValue = null)
        {
            var attr = @this.Attributes[attributeName];
            if (attr == null)
            {
                return false;
            }

            return attributeValue != null ? attr.Value == attributeValue : true;
        }

        public static string GetNodeText(this HtmlAgilityPack.HtmlNode node)
        {
            if (node == null)
                return null;
            return System.Web.HttpUtility.HtmlDecode(node.InnerText).Trim();
        }
        //Name: "#text"
        
        public static HtmlAgilityPack.HtmlNode GetNextNonTextSibling(this HtmlAgilityPack.HtmlNode node)
        {
            if (node == null)
                return null;

            do
            {
                node = node.NextSibling;
                if (node == null)
                    return null;
                if (node.Name != "#text")
                    return node;
            } while (true);
        }
        
        public static string[] GetNodeTextList(this HtmlAgilityPack.HtmlNode node)
        {
            if (node == null)
                return null;
            return node.ChildNodes.Where(nd => nd.NodeType == HtmlAgilityPack.HtmlNodeType.Text).Select(nd => nd.GetNodeText()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
        }
        
        public static bool IsLatin(this char chr)
        {
            dynamic num = Strings.AscW(chr);
            if (num <= 591)
                return true;
            return false;
        }
        
        public static bool IsJapanese(this char chr)
        {
            dynamic num = Strings.AscW(chr);
            if (num > 19967 && num < 40896)
                return true;
            // '19968-40895  12352-12447  12448-12543
            if (num > 12351 && num < 12542)
                return true;
            return false;
        }
        
        public static bool IsKorean(this char chr)
        {
            dynamic num = Strings.AscW(chr);
            if (num >= 44032 && num <= 55203)
                return true;
            if (num >= 4352 && num <= 4607)
                return true;
            if (num >= 12592 && num <= 12687)
                return true;
            if (num >= 43360 && num <= 43391)
                return true;
            if (num >= 55216 && num <= 55295)
                return true;
            return false;
        }

        public static string UrlEncodeFixed(this string url)
        {
            dynamic encoded = System.Web.HttpUtility.UrlEncode(url).Replace("!", "%21").Replace("(", "%28").Replace(")", "%29").Replace("*", "%2A");

            return encoded;
        }
    }
}
