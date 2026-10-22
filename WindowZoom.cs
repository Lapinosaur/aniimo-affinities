using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
namespace AniimoAffinities {
    // The window owns physical geometry; painters and hit targets retain logical coordinates.
    internal static class WindowZoom {
        internal static float Step(float scale,int direction) { return Math.Max(.5f,Math.Min(1.5f,scale+direction*.25f)); }
        internal static bool HandleShortcut(Keys input,IntPtr flags) {
            if(input!=(Keys.Control|Keys.Up) && input!=(Keys.Control|Keys.Down))return false;
            if((flags.ToInt64()&(1L<<30))!=0)return true;
            float next=Step(Chrome.WindowScale,input==(Keys.Control|Keys.Up)?1:-1);
            if(next==Chrome.WindowScale)return true;
            try { Chrome.SaveDisplay(Chrome.FontScale,Chrome.ShowBadges,next); }
            catch(System.IO.IOException) { System.Media.SystemSounds.Exclamation.Play(); }
            catch(UnauthorizedAccessException) { System.Media.SystemSounds.Exclamation.Play(); }
            return true;
        }
        internal static float[] Neighbors(float scale) {
            if(scale<=.5f)return new[]{.75f,1f};
            if(scale<1f)return new[]{.5f,1f};
            if(scale==1f)return new[]{.5f,1.5f};
            if(scale<1.5f)return new[]{1f,1.5f};
            return new[]{1f,1.25f};
        }
        internal static float Factor(Control control) { var form=control.FindForm() as PortableWindow;return form==null?1:form.AppliedWindowScale; }
        internal static void Apply(PortableWindow window,float scale) {
            float ratio=scale/window.AppliedWindowScale;if(Math.Abs(ratio-1)<.001)return;
            var bounds=new Dictionary<Control,Rectangle>();Collect(window,bounds);
            Size size=window.ClientSize,min=window.MinimumSize;
            window.SuspendLayout();window.MinimumSize=Size.Empty;window.AppliedWindowScale=scale;
            foreach(var item in bounds) {
                item.Key.Bounds=Scale(item.Value,ratio);
                if(item.Key is Label) {
                    item.Key.Font=new Font(item.Key.Font.FontFamily,item.Key.Font.Size*ratio,item.Key.Font.Style);
                    var padding=item.Key.Padding;item.Key.Padding=new Padding((int)Math.Round(padding.Left*ratio),(int)Math.Round(padding.Top*ratio),(int)Math.Round(padding.Right*ratio),(int)Math.Round(padding.Bottom*ratio));
                }
            }
            window.ClientSize=new Size((int)Math.Round(size.Width*ratio),(int)Math.Round(size.Height*ratio));
            window.MinimumSize=new Size((int)Math.Round(min.Width*ratio),(int)Math.Round(min.Height*ratio));
            window.ResumeLayout(true);Arrange(window);window.Invalidate(true);
            var work=Screen.FromControl(window).WorkingArea;
            window.Location=new Point(Math.Max(work.Left,Math.Min(window.Left,work.Right-window.Width)),Math.Max(work.Top,Math.Min(window.Top,work.Bottom-40)));
        }
        private static Rectangle Scale(Rectangle r,float scale) { return new Rectangle((int)Math.Round(r.X*scale),(int)Math.Round(r.Y*scale),(int)Math.Round(r.Width*scale),(int)Math.Round(r.Height*scale)); }
        private static void Collect(Control parent,Dictionary<Control,Rectangle> bounds) { foreach(Control child in parent.Controls) { bounds[child]=child.Bounds;Collect(child,bounds); } }
        private static void Arrange(Control parent) { foreach(Control child in parent.Controls) { var row=child as TypeSelector;if(row!=null) { bool selected=false;foreach(Control button in row.Controls)if(button is IconButton && ((IconButton)button).Type<0 && button.Visible && !row.IconModes)selected=true;row.Arrange(selected); }Arrange(child); } }
    }
    internal sealed class HoldSizeButton : Button {
        internal float Target;
        internal event Action<float> Commit;
        private readonly Timer timer=new Timer { Interval=16 };
        private readonly System.Diagnostics.Stopwatch watch=new System.Diagnostics.Stopwatch();
        private float progress;
        internal HoldSizeButton() {
            SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
            Cursor=Cursors.Hand;timer.Tick+=(s,e)=> {
                progress=Math.Min(1,(float)watch.Elapsed.TotalMilliseconds/900);Invalidate();
                if(progress>=1) { float target=Target;CancelHold();if(Commit!=null)Commit(target); }
            };
        }
        internal void CancelHold() { timer.Stop();watch.Reset();progress=0;Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e);if(e.Button==MouseButtons.Left) { watch.Restart();timer.Start(); } }
        protected override void OnMouseUp(MouseEventArgs e) { CancelHold();base.OnMouseUp(e); }
        protected override void OnMouseLeave(EventArgs e) { CancelHold();base.OnMouseLeave(e); }
        protected override void OnMouseCaptureChanged(EventArgs e) { if(!Capture)CancelHold();base.OnMouseCaptureChanged(e); }
        protected override void OnClick(EventArgs e) { } // A click alone must never change scale.
        protected override void OnPaint(PaintEventArgs e) {
            var g=e.Graphics;g.Clear(Chrome.Surface);g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Chrome.Box(g,new RectangleF(1,1,Width-3,Height-3),Chrome.Surface,Chrome.Border,7);
            using(var font=Chrome.UiFont("Segoe UI",8,FontStyle.Bold))Chrome.Text(g,Text,font,Chrome.TextColor,new RectangleF(0,0,Width,Height),true);
            Chrome.HoldHint(g,new RectangleF(1,1,Width-3,Height-3));
            if(progress<=0)return;
            using(var path=Chrome.Round(new RectangleF(1,1,Width-3,Height-3),7))using(var pen=new Pen(Chrome.Accent,2)) {
                path.Flatten();var points=path.PathPoints;float length=0;
                for(int i=0;i<points.Length;i++)length+=Distance(points[i],points[(i+1)%points.Length]);
                float remaining=length*progress;
                for(int i=0;i<points.Length && remaining>0;i++) {
                    var a=points[i];var b=points[(i+1)%points.Length];float distance=Distance(a,b);if(distance<=0)continue;
                    float fraction=Math.Min(1,remaining/distance);g.DrawLine(pen,a,new PointF(a.X+(b.X-a.X)*fraction,a.Y+(b.Y-a.Y)*fraction));remaining-=distance;
                }
            }
        }
        private static float Distance(PointF a,PointF b) { return (float)Math.Sqrt((b.X-a.X)*(b.X-a.X)+(b.Y-a.Y)*(b.Y-a.Y)); }
        protected override void Dispose(bool disposing) { if(disposing)timer.Dispose();base.Dispose(disposing); }
    }

}
