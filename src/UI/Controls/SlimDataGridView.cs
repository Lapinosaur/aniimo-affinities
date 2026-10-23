using System;
using System.Drawing;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class SlimDataGridView : DataGridView
    {
        private readonly SlimScrollBar scroll = new SlimScrollBar();
        private bool syncing;
        private int wheelDelta;
        internal SlimDataGridView()
        {
            ScrollBars = ScrollBars.None;
            Controls.Add(scroll);
            scroll.ValueChanged += ScrollTo;
            Scroll += (s, e) => SyncScroll();
            RowsAdded += (s, e) => SyncScroll();
            RowsRemoved += (s, e) => SyncScroll();
            SizeChanged += (s, e) => SyncScroll();
            ColumnWidthChanged += (s, e) => SyncScroll();
        }
        private int PageRows
        {
            get
            {
                return Math.Max(1, (ClientSize.Height - ColumnHeadersHeight) / Math.Max(1, RowTemplate.Height));
            }
        }
        private void ScrollTo(int row)
        {
            if (Rows.Count == 0)
                return;
            FirstDisplayedScrollingRowIndex = Math.Max(0, Math.Min(Math.Max(0, Rows.Count - PageRows), row));
            SyncScroll();
        }
        private void SyncScroll()
        {
            if (syncing || IsDisposed)
                return;
            syncing = true;
            try
            {
                scroll.BackColor = BackgroundColor;
                scroll.SetBounds(Math.Max(0, ClientSize.Width - 12), ColumnHeadersHeight + 1, 12, Math.Max(1, ClientSize.Height - ColumnHeadersHeight - 1));
                scroll.Configure(Math.Max(0, FirstDisplayedScrollingRowIndex), Rows.Count - PageRows, PageRows);
                scroll.BringToFront();
            }
            finally { syncing = false; }
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            wheelDelta += e.Delta;
            int steps = wheelDelta / 120;
            wheelDelta %= 120;
            int lines = SystemInformation.MouseWheelScrollLines;
            if (steps != 0 && lines != 0)
                ScrollTo(Math.Max(0, FirstDisplayedScrollingRowIndex) - steps * (lines < 0 ? PageRows : lines));
            var handled = e as HandledMouseEventArgs;
            if (handled != null)
                handled.Handled = true;
        }
    }
}
