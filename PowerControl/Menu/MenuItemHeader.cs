using PowerControl.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PowerControl.Menu
{
    public class MenuItemHeader : MenuItem
    {
        private ToolStripLabel toolStripItem = new ToolStripLabel(ProfilesController.DefaultName);
        private string text = ProfilesController.DefaultName;

        public string DefaultTitle = string.Empty;
        public delegate string CurrentTitleDelegate();
        public delegate bool IsVisibleDelegate();
        public CurrentTitleDelegate? CurrentTitle { get; set; }
        public IsVisibleDelegate? IsVisible { get; set; }

        public MenuItemHeader()
        {
            Selectable = false;
            Visible = false;
        }

        public override void CreateMenu(ContextMenuStrip contextMenu)
        {
            toolStripItem.Visible = false;
            toolStripItem.Text = text;
            contextMenu.Items.Add(toolStripItem);
            contextMenu.Opening += delegate
            {
                Update();

                toolStripItem.Visible = Visible;
            };
        }

        public override string Render(MenuItem? selected)
        {
            return Color(text, Colors.Red).PadRight(30);
        }

        public override void SelectNext(int n)
        {
        }

        public override void Update()
        {
            text = CurrentTitle?.Invoke() ?? string.Empty;
            Visible = IsVisible?.Invoke() ?? false;

            if (toolStripItem != null)
            {
                toolStripItem.Text = text;
                toolStripItem.Visible = Visible;
            }
        }

        public override void Reset()
        {
            text = DefaultTitle;
            Visible = false;

            if (toolStripItem != null)
            {
                toolStripItem.Text = text;
                toolStripItem.Visible = Visible;
            }
        }
    }
}
