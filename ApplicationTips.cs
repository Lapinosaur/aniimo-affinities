using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AniimoAffinities {
    // Shared native bubble matching ApplicationTooltips.css and tooltipPlacement.ts.
    internal sealed class ApplicationTips : IDisposable {
        private readonly Dictionary<Control,string> labels=new Dictionary<Control,string>();
        private readonly Timer timer=new Timer();
        private readonly TipBubble bubble=new TipBubble();
        private Control anchor; private Form owner; private bool displayed;
        internal static ApplicationTips Create() { return new ApplicationTips(); }
        private ApplicationTips() { timer.Interval=450;timer.Tick+=Tick; }
        internal void SetToolTip(Control control,string text) {
            if(!labels.ContainsKey(control)) { control.MouseEnter+=Enter;control.MouseLeave+=Leave;control.MouseDown+=Down;control.Disposed+=Leave; }
            labels[control]=text;
        }
        private void Enter(object sender,EventArgs e) { Hide();anchor=(Control)sender;timer.Interval=450;timer.Start(); }
        private void Leave(object sender,EventArgs e) { Hide(); }
        private void Down(object sender,MouseEventArgs e) { Hide(); }
        private void Tick(object sender,EventArgs e) {
            timer.Stop();
            if(displayed) { Hide();return; }
            if(anchor==null || anchor.IsDisposed || !anchor.Visible || !anchor.Enabled)return;
            owner=anchor.FindForm();if(owner==null || Form.ActiveForm!=owner)return;
            string text;if(!labels.TryGetValue(anchor,out text) || string.IsNullOrWhiteSpace(text))return;
            owner.Deactivate+=Leave;owner.LocationChanged+=Leave;
            bubble.Present(anchor,text);displayed=true;timer.Interval=8000;timer.Start();
        }
        private void Hide() { timer.Stop();bubble.Hide();displayed=false;anchor=null;if(owner!=null) { owner.Deactivate-=Leave;owner.LocationChanged-=Leave;owner=null; } }
        public void Dispose() {
            Hide();timer.Dispose();bubble.Dispose();
            foreach(var item in labels) { item.Key.MouseEnter-=Enter;item.Key.MouseLeave-=Leave;item.Key.MouseDown-=Down;item.Key.Disposed-=Leave; }labels.Clear();
        }
    }
    internal sealed class TipBubble : Form {
        private string label;private bool below;private float arrowX;
        private const TextFormatFlags TextFlags=TextFormatFlags.WordBreak|TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix|TextFormatFlags.HorizontalCenter;
        internal TipBubble() {
            FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;
            BackColor=Chrome.Surface;DoubleBuffered=true;Font=Chrome.UiFont("Segoe UI",8.25f,FontStyle.Bold);
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams { get { var p=base.CreateParams;p.ExStyle|=0x08000000|0x80|0x20;return p; } }
        // Inputs: hovered control, text. Output: clamped non-activating bubble with arrow pointing at anchor. Fallback: flip above when bottom lacks room. Raises: none.
        internal void Present(Control anchor,string text) {
            BackColor=Chrome.Surface;label=text;var bounds=anchor.RectangleToScreen(anchor.ClientRectangle);var screen=Screen.FromControl(anchor).WorkingArea;
            // Measure and paint with the same renderer, width and wrapping rules.
            int maxTextWidth=Math.Max(40,Math.Min(264,screen.Width-34));
            var measured=TextRenderer.MeasureText(text,Font,new Size(maxTextWidth,int.MaxValue),TextFlags);
            int textWidth=Math.Min(maxTextWidth,measured.Width+2);
            measured=TextRenderer.MeasureText(text,Font,new Size(textWidth,int.MaxValue),TextFlags);
            Size=new Size(textWidth+20,measured.Height+22);
            below=bounds.Bottom+7+Height<=screen.Bottom-7;
            int left=Math.Max(screen.Left+7,Math.Min(bounds.Left+(bounds.Width-Width)/2,screen.Right-Width-7));
            int top=below?bounds.Bottom+7:Math.Max(screen.Top+7,bounds.Top-Height-7);
            arrowX=Math.Max(12,Math.Min(Width-14,bounds.Left+bounds.Width/2f-left));Location=new Point(left,top);
            // Clip the window to its silhouette instead of color-keying antialiased pixels.
            var body=new RectangleF(1,below?6:1,Width-4,Height-10);
            using(var outline=Chrome.Round(body,6)) {
                var shape=new Region(outline);
                using(var shadow=Chrome.Round(new RectangleF(body.X+2,body.Y+2,body.Width,body.Height),6))shape.Union(shadow);
                float edge=below?body.Top:body.Bottom;
                using(var arrow=new GraphicsPath()) {
                    arrow.AddPolygon(new[]{new PointF(arrowX-5,edge),new PointF(arrowX,below?edge-5:edge+5),new PointF(arrowX+5,edge)});
                    shape.Union(arrow);
                }
                var previous=Region;Region=shape;if(previous!=null)previous.Dispose();
            }
            Show(anchor.FindForm());Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e) {
            var g=e.Graphics;g.Clear(Chrome.Surface);g.SmoothingMode=SmoothingMode.AntiAlias;
            var body=new RectangleF(1,below?6:1,Width-4,Height-10);
            using(var shadow=Chrome.Round(new RectangleF(body.X+2,body.Y+2,body.Width,body.Height),6))using(var brush=new SolidBrush(Chrome.TooltipShadow))g.FillPath(brush,shadow);
            Chrome.Box(g,body,Chrome.Surface,Chrome.StrongBorder,6);
            float edge=below?body.Top:body.Bottom,tip=below?edge-5:edge+5;
            var points=new[]{new PointF(arrowX-5,edge),new PointF(arrowX,tip),new PointF(arrowX+5,edge)};
            using(var fill=new SolidBrush(Chrome.Surface))g.FillPolygon(fill,points);
            using(var erase=new Pen(Chrome.Surface,2))g.DrawLine(erase,arrowX-4,edge,arrowX+4,edge);
            using(var border=new Pen(Chrome.StrongBorder))g.DrawLines(border,points);
            TextRenderer.DrawText(g,label,Font,Rectangle.Round(new RectangleF(body.X+8,body.Y+6,body.Width-16,body.Height-12)),Chrome.TextColor,TextFlags);
        }
    }
}
