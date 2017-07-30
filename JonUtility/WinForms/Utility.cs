namespace JonUtility.WinForms
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;

    public static class Utility
    {
        public class LabelLine
        {
            public string Label { get; set; }

            public Type FieldType { get; set; }

            public string Text { get; set; }

            public Label CreatedLabel { get; internal set; }

            public Control CreatedControl { get; internal set; }

            public static LabelLine Create(string label, Type fieldType = null, string text = null)
            {
                return new LabelLine()
                {
                    Label = label,
                    FieldType = fieldType ?? typeof(TextBox),
                    Text = text
                };
            }
        }
        

        public static Dictionary<string, LabelLine> LabelFieldArray(IEnumerable<LabelLine> labelsWithFieldType, Control parent, int left, int top, int width, int height, bool rightAlign = true, int verticalSpacing = 5, int horizontalSpacing = 5)
        {
            var createdItems = new Dictionary<string, LabelLine>();
            foreach (var labelWithFieldType in labelsWithFieldType)
            {
                var label = NewControl<Label>(parent, left, top, labelWithFieldType.Label, 100, height);
                var field = Activator.CreateInstance(labelWithFieldType.FieldType ?? typeof(TextBox)) as Control;
                field.Top = top;
                field.Width = width;
                field.Height = height;
                field.Tag = labelWithFieldType.Label;
                field.Text = labelWithFieldType.Text;
                label.AutoSize = true;

                labelWithFieldType.CreatedLabel = label;
                labelWithFieldType.CreatedControl = field;
                createdItems.Add(labelWithFieldType.Label, labelWithFieldType);

                top += height + verticalSpacing;
            }

            var maxWidth = createdItems.Select(c => c.Value.CreatedLabel.Width).Max();
            foreach (var createdItem in createdItems)
            {
                createdItem.Value.CreatedLabel.AutoSize = false;
                createdItem.Value.CreatedLabel.Width = maxWidth;
                createdItem.Value.CreatedControl.Left = createdItem.Value.CreatedLabel.Right + horizontalSpacing;
                createdItem.Value.CreatedControl.Parent = parent;
            }

            return createdItems;
        }


        /// <summary>
        ///     Creates a new <see cref="Control" /> with the specified
        ///     parameters.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of <see cref="Control" /> to create. It must inherit
        ///     from <see cref="Control" /> and have a
        ///     parameterless-constructor.
        /// </typeparam>
        /// <param name="parent">
        ///     The new <see cref="Control" />'s parent
        ///     <see cref="Control" />. This parameter can be null.
        /// </param>
        /// <param name="left">
        ///     The new <see cref="Control" />'s X coordinate.
        /// </param>
        /// <param name="top">
        ///     The new <see cref="Control" />'s Y coordinate.
        /// </param>
        /// <param name="text">
        ///     The text value for the new <see cref="Control" />. This value
        ///     can be null or empty.
        /// </param>
        /// <param name="width">
        ///     The new <see cref="Control" />'s width. If a value below 0 is
        ///     specified for either the width or height, this size parameters
        ///     are ignored.
        /// </param>
        /// <param name="height">
        ///     The new <see cref="Control" />'s height. If a value below 0 is
        ///     specified for either the width or height, this size parameters
        ///     are ignored.
        /// </param>
        /// <returns>T.</returns>
        public static T NewControl<T>(Control parent, int left, int top, string text, int width, int height)
            where T : Control, new()
        {
            T newControl = Activator.CreateInstance<T>();
            newControl.Parent = parent;
            newControl.Text = text ?? string.Empty;

            if (width > -1 && height > -1)
            {
                newControl.SetBounds(left, top, width, height);
            }
            else
            {
                newControl.Location = new Point(left, top);
            }

            return newControl;
        }

        /// <summary>
        ///     Creates a new <see cref="Control" /> with the specified parameters.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of <see cref="Control" /> to create. It must inherit from <see cref="Control" /> and have
        ///     a parameterless-constructor.
        /// </typeparam>
        /// <param name="parent">The new <see cref="Control" />'s parent <see cref="Control" />. This parameter can be null.</param>
        /// <param name="left">The new <see cref="Control" />'s X coordinate.</param>
        /// <param name="top">The new <see cref="Control" />'s Y coordinate.</param>
        public static T NewControl<T>(Control parent, int left, int top)
            where T : Control, new()
        {
            return Utility.NewControl<T>(parent, left, top, "", -1, -1);
        }
    }
}
