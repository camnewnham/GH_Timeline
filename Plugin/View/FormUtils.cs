using System;
using System.Windows.Forms;

namespace GH_Timeline
{
    internal static class FormUtils
    {
        public static ToolStripItem Menu_AppendNumericUpDown(ToolStrip menu, string label, decimal value, int decimals, EventHandler<NumericUpDown> OnChange, decimal min = decimal.MinValue, decimal max = decimal.MaxValue)
        {
            NumericUpDown updown = new NumericUpDown()
            {
                DecimalPlaces = decimals,
                Minimum = min,
                Maximum = max,
                Value = value,
                Width = 100,
            };

            updown.ValueChanged += (s, e) => OnChange?.Invoke(s, updown);


            TableLayoutPanel layout = new TableLayoutPanel()
            {
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                Width = -1,
                BackColor = System.Drawing.Color.Transparent
            };

            layout.Controls.Add(new Label() { Text = label, AutoSize = true, Width = -1, MinimumSize = new System.Drawing.Size(200, -1), TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 0, 0);
            layout.Controls.Add(updown, 1, 0);

            ToolStripItem item = new ToolStripControlHost(layout);

            menu.Items.Add(item);

            return item;
        }
    }
}
