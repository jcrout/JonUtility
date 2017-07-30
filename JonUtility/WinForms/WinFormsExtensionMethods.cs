namespace JonUtility.WinForms
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;

    public static class WinFormsExtensionMethods
    {

        public static List<object> ToList(this ComboBox.ObjectCollection @this)
        {
            var items = new List<object>();
            foreach (var obj in @this)
            {
                items.Add(obj);
            }

            return items;
        }

        /// <summary>
        ///     Use this to always return a Color from a Color's Name property,
        ///     including when the Color's Name is a 4-digit hex string (doesn't
        ///     work with Color.FromName, which expects an enum name).
        /// </summary>
        /// <param name="name">
        ///     The name of the color, as a 4-digit hex string or common/enum
        ///     name.
        /// </param>
        /// <returns></returns>
        public static Color ToColor(this string @this)
        {
            var returnColor = Color.FromName(@this);

            if (returnColor != default(Color))
            {
                return returnColor;
            }

            var bytes = Enumerable.Range(0, @this.Length)
                                  .Where(i => i % 2 == 0)
                                  .Select(i => Convert.ToByte(@this.Substring(i, 2), 16))
                                  .ToArray();

            returnColor = Color.FromArgb(bytes[0], bytes[1], bytes[2], bytes[3]);

            return returnColor;
        }

        public static T AddChild<T>(this Control @this, int left, int top, string text = "", int width = -1, int height = -1) where T : Control, new()
        {
            return Utility.NewControl<T>(@this, left, top, text, width, height);
        }

        public static Label AddChildLabel(this Control @this, int left, int top, string text = "", int width = -1, int height = -1, ContentAlignment textAlign = ContentAlignment.TopLeft, BorderStyle borderStyle = BorderStyle.None)
        {
            var lbl = Utility.NewControl<Label>(@this, left, top, text, width, height);
            lbl.TextAlign = textAlign;
            lbl.BorderStyle = borderStyle;
            return lbl;
        }

        public static void CenterForm(this Form @this, int width, int height)
        {
            @this.SetBounds(
                (Screen.PrimaryScreen.WorkingArea.Width - width) / 2,
                (Screen.PrimaryScreen.WorkingArea.Height - height) / 2,
                width,
                height);
        }
    }
}
