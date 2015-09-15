namespace JonUtility.ASP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Mvc;

    public static class ExtensionMethods
    {
        private static MethodInfo methodToString;
        private static MethodInfo methodStringFormat;
        private static MethodInfo methodDisplayFor;
        private static MethodInfo methodEditorFor;
        private static MethodInfo methodValidationMessageFor;
        private static MethodInfo methodLabelFor;
        private static MethodInfo methodHiddenFor;

        static ExtensionMethods()
        {
            methodToString = typeof(Object).GetMethod("ToString", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            methodStringFormat = typeof(String).GetMethod("Format", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string), typeof(object) }, null);
            methodDisplayFor = typeof(System.Web.Mvc.Html.DisplayExtensions).GetMethods().First(mi => mi.Name == "DisplayFor");
            methodLabelFor = typeof(System.Web.Mvc.Html.LabelExtensions).GetMethods().First(mi => mi.Name == "LabelFor" && mi.GetParameters().Count() == 3 && mi.GetParameters()[2].Name == "htmlAttributes");
            methodHiddenFor = typeof(System.Web.Mvc.Html.InputExtensions).GetMethods().First(mi => mi.Name == "HiddenFor" && mi.GetParameters().Count() == 2);
            methodEditorFor = typeof(System.Web.Mvc.Html.EditorExtensions).GetMethods().First(mi => mi.Name == "EditorFor" && mi.GetParameters().Count() == 3 && mi.GetParameters()[2].Name == "additionalViewData");
            methodValidationMessageFor = typeof(System.Web.Mvc.Html.ValidationExtensions).GetMethods().First(mi => mi.Name == "ValidationMessageFor" && mi.GetParameters().Count() == 4 && mi.GetParameters()[3].Name == "htmlAttributes");
        }

        public static string ActionButton(this HtmlHelper helper, string action, string controller, string text)
        {
            var urlHelper = new UrlHelper();
            var actionText = urlHelper.Action(action, controller, new object[0]);
            return String.Format("<input type=\"button\" value=\"{0}\" onclick=\"location.href='{1}' />", text, actionText);
        }

        /// <summary>
        ///     Creates a table from an <see cref="IEnumerable{T}"/> of the View's model.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="helper"></param>
        /// <param name="models"></param>
        /// <param name="maxCount"></param>
        /// <param name="tableHtml"></param>
        /// <param name="modelSelector"></param>
        /// <returns></returns>
        public static MvcHtmlString TableFor<T>(this HtmlHelper<IEnumerable<T>> helper, IEnumerable<T> models, int maxCount = -1, string tableHtml = null, Func<T, bool> modelSelector = null, Func<T, string> extraModelColumn = null)
        {
            if (models == null || !models.Any())
            {
                return MvcHtmlString.Empty;
            }

            var builder = new StringBuilder();
            var modelType = typeof(T);
            var modelProperties = modelType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            var metaData = new ViewDataDictionary<T>().ModelMetadata;
            var expressionList = new List<Func<T, MvcHtmlString>>();
            var modelParam = Expression.Parameter(modelType, "model");
            var counter = 0;

            maxCount = maxCount < 0 ? Int32.MaxValue - 1 : maxCount;
            builder.AppendLine(@"<table " + (tableHtml ?? @"class=""table""") + ">");
            builder.Append(@"<tr>");

            foreach (var obj in metaData.Properties)
            {
                if (obj.HideSurroundingHtml)
                {
                    continue;
                }

                var property = modelProperties.First(p => p.Name == obj.PropertyName);
                var method = methodDisplayFor.MakeGenericMethod(typeof(IEnumerable<T>), property.PropertyType);
                var expr = Expression.MakeMemberAccess(modelParam, property);
                var expr2 = Expression.Lambda(expr, Expression.Parameter(typeof(IEnumerable<T>)));
                var caller = Expression.Call(null, method, Expression.Constant(helper), expr2);
                var actualMethod = (Func<T, MvcHtmlString>)Expression.Lambda(caller, modelParam).Compile();
                expressionList.Add(actualMethod);

                builder.Append("<th>");
                builder.Append(obj.GetDisplayName());
                builder.Append(@"</th>");
            }


            if (extraModelColumn != null)
            {
                builder.Append(@"<th></th>");
            }

            builder.AppendLine(@"</tr>");

            foreach (var model in models)
            {
                if (model == null)
                {
                    continue;
                }

                if (modelSelector != null)
                {
                    if (!modelSelector(model))
                    {
                        continue;
                    }
                }

                builder.Append(@"<tr>");
                foreach (var expr in expressionList)
                {
                    builder.Append("<td>");
                    var value = expr(model);
                    builder.Append(value.ToString());
                    builder.Append(@"</td>");
                }

                if (extraModelColumn != null)
                {
                    builder.Append("<td>");
                    builder.Append(extraModelColumn(model).ToString());
                    builder.Append(@"</td>");
                }

                builder.AppendLine(@"</tr>");

                counter++;
                if (counter == maxCount)
                {
                    break;
                }
            }

            builder.AppendLine(@"</table>");

            var htmlString = new MvcHtmlString(builder.ToString());
            return htmlString;
        }

        public static object EditorFormFor<T>(this HtmlHelper<T> helper, T model, string title = null)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var metaData = new ViewDataDictionary<T>().ModelMetadata;
            var builder = new StringBuilder();
            var writer = helper.ViewContext.Writer;
            var modelType = typeof(T);
            var modelParam = Expression.Parameter(modelType, "model");
            var modelProperties = modelType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var expressionList = new List<Tuple<Func<T, MvcHtmlString>, Func<T, MvcHtmlString>, Func<T, MvcHtmlString>>>();

            using (var form = System.Web.Mvc.Html.FormExtensions.BeginForm(helper))
            {
                writer.Write(helper.AntiForgeryToken());
                writer.WriteLine(@"<div class=""form-horizontal"">");
                writer.Write(@"<h4>");
                writer.Write(title ?? modelType.Name);
                writer.WriteLine(@"</h4>");
                writer.WriteLine(@"<hr />");

                var validationMessageFor = System.Web.Mvc.Html.ValidationExtensions.ValidationSummary(helper, true, "", new { @class = "text-danger" });
                if (validationMessageFor != null)
                {
                    writer.WriteLine(validationMessageFor.ToString());
                }

                foreach (var obj in metaData.Properties)
                {
                    var property = modelProperties.First(p => p.Name == obj.PropertyName);
                    var isNullableBoolean = property.PropertyType == typeof(bool?);
                    if (obj.HideSurroundingHtml)
                    {
                        var hiddenExpression = GetPropertyExpression(helper, property, methodHiddenFor, modelParam);
                        writer.WriteLine(hiddenExpression(model).ToString());
                        continue;
                    }

                    var labelExpression = GetPropertyExpression(helper, property, methodLabelFor, modelParam, Expression.Constant(new { @class = "control-label col-md-2" }));
                    var editorExpression = GetPropertyExpression(helper, property, methodEditorFor, modelParam, Expression.Constant(new { htmlAttributes = new { @class = "form-control" } }));
                    var validationExpression = GetPropertyExpression(helper, property, methodValidationMessageFor, modelParam, Expression.Constant(""), Expression.Constant(new { @class = "text-danger" }));

                    var labelText = labelExpression(model).ToString();
                    var editorText = editorExpression(model).ToString();
                    var validationText = validationExpression(model).ToString();

                    writer.WriteLine(@"<div class=""form-group"">");
                    writer.WriteLine(labelExpression(model).ToString());
                    writer.WriteLine(@"<div class=""col-md-10"">");
                    writer.WriteLine(editorExpression(model).ToString());
                    writer.WriteLine(validationExpression(model).ToString());
                    writer.WriteLine(@"</div>");
                    writer.WriteLine(@"</div>");
                }

                writer.WriteLine(@"<div class=""form-group"">");
                writer.WriteLine(@"<div class=""col-md-offset-2 col-md-10"">");
                writer.Write(@"<input type=""submit"" value=""Save"" class=""btn btn-default"" />");
                writer.WriteLine(@"</div>");
                writer.WriteLine(@"</div>");
                writer.WriteLine(@"</div>");
            }

            return null;
        }

        private static Func<T, MvcHtmlString> GetPropertyExpression<T>(HtmlHelper<T> helper, PropertyInfo property, MethodInfo methodToUse, ParameterExpression modelParam, params Expression[] methodParameters)
        {
            var modelType = typeof(T);
            var method = methodToUse.MakeGenericMethod(modelType, property.PropertyType);
            var expr = Expression.MakeMemberAccess(modelParam, property);
            var expr2 = Expression.Lambda(expr, modelParam);
            var parameters = new Expression[methodParameters.Length + 2];
            parameters[0] = Expression.Constant(helper);
            parameters[1] = expr2;

            for (int i = 0; i < methodParameters.Length; i++)
            {
                parameters[i + 2] = methodParameters[i];
            }

            var caller = Expression.Call(null, method, parameters);
            var labelExpression = (Func<T, MvcHtmlString>)Expression.Lambda(caller, modelParam).Compile();

            return labelExpression;
        }
    }
}
