using System;
using System.Drawing;
using System.Linq;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AniimoAffinities {
    internal static class Chrome {
        internal static event Action DisplayChanged;
        internal static float FontScale=1;
        internal static float WindowScale=1;
        internal static bool ShowBadges=true;
        private static readonly string DisplayPath=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AniimoAffinities","display.txt");
        internal static Font UiFont(string family,float size,FontStyle style=FontStyle.Regular) { return new Font(family,size*FontScale,style); }
        internal static Font UiFont(FontFamily family,float size,FontStyle style=FontStyle.Regular) { return new Font(family,size*FontScale,style); }
        internal static void SaveDisplay(float scale,bool badges,float windowScale=1) {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(DisplayPath));
            System.IO.File.WriteAllLines(DisplayPath,new[]{scale.ToString(System.Globalization.CultureInfo.InvariantCulture),badges.ToString(),windowScale.ToString(System.Globalization.CultureInfo.InvariantCulture)});
            ApplyDisplay(scale,badges,windowScale);
        }
        internal static void ApplyDisplay(float scale,bool badges,float windowScale=1) {
            WindowScale=Math.Max(.5f,Math.Min(1.5f,windowScale));
            foreach(Form form in Application.OpenForms) { var window=form as PortableWindow;if(window!=null)window.ApplyWindowScale(); }
            float ratio=scale/FontScale;FontScale=scale;ShowBadges=badges;
            var fonts=new System.Collections.Generic.List<Tuple<Control,Font>>();
            foreach(Form form in Application.OpenForms)CollectFonts(form,ratio,fonts);
            foreach(var pair in fonts) { pair.Item1.Font=pair.Item2;pair.Item1.Invalidate(); }
            if(DisplayChanged!=null)DisplayChanged();
        }
        private static void CollectFonts(Control control,float ratio,System.Collections.Generic.List<Tuple<Control,Font>> fonts) {
            fonts.Add(Tuple.Create(control,new Font(control.Font.FontFamily,control.Font.Size*ratio,control.Font.Style)));
            foreach(Control child in control.Controls)CollectFonts(child,ratio,fonts);
        }
        internal const float WindowRadius=16,PanelRadius=16,SelectorRadius=12,ControlRadius=8;
        internal static readonly Color Anthracite=Color.FromArgb(32,39,53),Ivory=Color.FromArgb(245,241,232);
        internal static Color AccentSurface,AccentHover,AccentPressed,AccentLight,AccentInk;
        internal static Color Background=Anthracite,Surface=Anthracite,Border=Anthracite,Accent=Color.FromArgb(25,197,199),Muted=Ivory;
        internal static Color TextColor=Ivory;
        internal static Color Attack,Defense,AttackSurface,DefenseSurface,ReducedDamage,IncreasedDamage;
        internal static Color AccentForeground=Accent,HoverSurface=Anthracite,StrongBorder=Anthracite,Error=Color.FromArgb(245,105,115),TooltipShadow=Anthracite;
        internal static int Appearance;
        private static readonly string AppearancePath=System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),"AniimoAffinities","appearance.txt");
        internal static void LoadAppearance() {
            try {
                var display=System.IO.File.ReadAllLines(DisplayPath);float scale;bool badges;
                if(display.Length>=2 && float.TryParse(display[0],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out scale) && scale>=0.9f && scale<=1.1f && bool.TryParse(display[1],out badges)) { FontScale=scale;ShowBadges=badges; }
                float zoom;if(display.Length>=3 && float.TryParse(display[2],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out zoom) && zoom>=.5f && zoom<=1.5f)WindowScale=zoom;
            } catch(System.IO.IOException) {} catch(UnauthorizedAccessException) {}
            int mode;
            try { if(int.TryParse(System.IO.File.ReadAllText(AppearancePath),out mode) && mode>=0 && mode<=2)Appearance=mode; } catch(System.IO.IOException) {} catch(UnauthorizedAccessException) {}
            ApplyAppearance();
            Microsoft.Win32.SystemEvents.UserPreferenceChanged+=(s,e)=> {
                if(Appearance!=1 || Application.OpenForms.Count==0)return;
                var form=Application.OpenForms[0];
                if(!form.IsDisposed && form.IsHandleCreated)form.BeginInvoke(new Action(ApplyAppearance));
            };
        }
        internal static void CycleAppearance() {
            int next=(Appearance+1)%3;
            try { System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(AppearancePath));System.IO.File.WriteAllText(AppearancePath,next.ToString()); }
            catch(System.IO.IOException) { return; } catch(UnauthorizedAccessException) { return; }
            Appearance=next;ApplyAppearance();
        }
        internal static void ApplyAppearance() {
            bool light=Appearance==2;
            if(Appearance==1) {
                using(var key=Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                    light=key==null || Convert.ToInt32(key.GetValue("AppsUseLightTheme",1))!=0;
            }
            var oldBackground=Background;var oldSurface=Surface;var oldText=TextColor;var oldMuted=Muted;
            var oldAccentSurface=AccentSurface;var oldAccentHover=AccentHover;var oldError=Error;var oldBorder=Border;
            Background=light?Ivory:Anthracite;
            Surface=Blend(Background,light?Anthracite:Ivory,light?0.025f:0.04f);
            TextColor=light?Anthracite:Ivory;
            Border=Blend(Background,TextColor,0.22f);
            Muted=Blend(Background,TextColor,light?0.72f:0.68f);
            AccentSurface=Blend(Surface,Accent,light?0.14f:0.18f);
            AccentHover=Blend(Surface,Accent,light?0.26f:0.32f);
            AccentPressed=Blend(Accent,Anthracite,0.16f);
            AccentLight=Blend(Accent,Ivory,0.22f);
            AccentInk=Anthracite;
            AccentForeground=light?Blend(Accent,Anthracite,0.68f):Accent;
            HoverSurface=Blend(Background,TextColor,0.09f);
            StrongBorder=Blend(Background,TextColor,0.52f);
            Error=light?Color.FromArgb(177,35,55):Color.FromArgb(245,105,115);
            TooltipShadow=Blend(Background,Anthracite,0.26f);
            Attack=light?Color.FromArgb(151,70,18):Color.FromArgb(255,183,112);
            Defense=light?Color.FromArgb(35,91,156):Color.FromArgb(131,188,255);
            AttackSurface=Blend(Surface,Attack,0.10f);
            DefenseSurface=Blend(Surface,Defense,0.10f);
            ReducedDamage=light?Color.FromArgb(105,53,140):Color.FromArgb(212,175,245);
            IncreasedDamage=light?AccentForeground:Blend(Accent,TextColor,.22f);
            foreach(Form form in Application.OpenForms) {
                ApplyControlTheme(form,oldBackground,oldSurface,oldText,oldMuted);
                UpdateAccentControls(form,oldAccentSurface,oldAccentHover,oldError,oldBorder);
                if(form is PortableWindow && form.TopMost)form.TransparencyKey=Background;
            }
        }
        internal static Color MultiplierInk(double value) { return value<.999?ReducedDamage:value>1.001?IncreasedDamage:Muted; }
        internal static Color MultiplierFill(double value) {
            if(value>=.999 && value<=1.001)return Surface;
            bool extreme=value<.5 || value>2;
            return Blend(Surface,value<1?ReducedDamage:Accent,extreme?.21f:.12f);
        }
        internal static Color MultiplierBorder(double value) {
            return value>=.999 && value<=1.001?Border:Blend(Surface,MultiplierInk(value),value<.5 || value>2?.55f:.32f);
        }
        private static Color Blend(Color background,Color foreground,float amount) {
            return Color.FromArgb((int)(background.R+(foreground.R-background.R)*amount),(int)(background.G+(foreground.G-background.G)*amount),(int)(background.B+(foreground.B-background.B)*amount));
        }
        private static void UpdateAccentControls(Control control,Color oldSurface,Color oldHover,Color oldError,Color oldBorder) {
            if(control.BackColor==oldSurface)control.BackColor=AccentSurface;
            else if(control.BackColor==oldBorder)control.BackColor=Border;
            if(control.ForeColor==oldError)control.ForeColor=Error;
            var button=control as Button;
            if(button!=null && button.FlatAppearance.MouseOverBackColor==oldHover)button.FlatAppearance.MouseOverBackColor=AccentHover;
            foreach(Control child in control.Controls)UpdateAccentControls(child,oldSurface,oldHover,oldError,oldBorder);
        }
        private static void ApplyControlTheme(Control control,Color oldBackground,Color oldSurface,Color oldText,Color oldMuted) {
            if(control.BackColor==oldBackground)control.BackColor=Background;
            else if(control.BackColor==oldSurface)control.BackColor=Surface;
            if(control.ForeColor==oldText)control.ForeColor=TextColor;
            else if(control.ForeColor==oldMuted)control.ForeColor=Muted;
            foreach(Control child in control.Controls)ApplyControlTheme(child,oldBackground,oldSurface,oldText,oldMuted);
            control.Invalidate();
        }
        // Inputs: rectangle and radius. Output: rounded vector path. Fallback: none. Raises: invalid geometry.
        internal static GraphicsPath Round(RectangleF r,float radius) {
            float d=radius*2; var p=new GraphicsPath(); p.AddArc(r.X,r.Y,d,d,180,90); p.AddArc(r.Right-d,r.Y,d,d,270,90); p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90); p.AddArc(r.X,r.Bottom-d,d,d,90,90); p.CloseFigure(); return p;
        }
        internal static void Box(Graphics g,RectangleF r,Color fill,Color border,float radius) {
            using(var path=Round(r,radius))using(var brush=new SolidBrush(fill))using(var pen=new Pen(border)) { g.FillPath(brush,path); g.DrawPath(pen,path); }
        }
        internal static void HoldHint(Graphics g,RectangleF bounds) {
            // A short accent segment previews the contour that fills during a hold.
            using(var pen=new Pen(Color.FromArgb(160,AccentForeground),1.5f) { StartCap=LineCap.Round,EndCap=LineCap.Round })
                g.DrawLine(pen,bounds.X+bounds.Width/2-3,bounds.Bottom-1,bounds.X+bounds.Width/2+3,bounds.Bottom-1);
        }
        internal static void Text(Graphics g,string text,Font font,Color color,RectangleF rect,bool center) {
            using(var b=new SolidBrush(color))using(var f=new StringFormat { Alignment=center?StringAlignment.Center:StringAlignment.Near,LineAlignment=StringAlignment.Center })g.DrawString(text,font,b,rect,f);
        }
        // Inputs: semantic icon and box. Output: vector shield, swords, reset or pin. Fallback: reset. Raises: none.
        internal static void Glyph(Graphics g,string name,RectangleF box,Color color) {
            if(name=="atk" || name=="rows")color=Attack;
            else if(name=="def" || name=="columns")color=Defense;
            var state=g.Save(); g.TranslateTransform(box.X,box.Y); g.ScaleTransform(box.Width/24,box.Height/24);
            using(var pen=new Pen(color,1.7f) { StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round }) {
                if(name=="matrix") { using(var frame=Round(new RectangleF(3,3,18,18),2))g.DrawPath(pen,frame);g.DrawLine(pen,9,3,9,21);g.DrawLine(pen,15,3,15,21);g.DrawLine(pen,3,9,21,9);g.DrawLine(pen,3,15,21,15); }
                else if(name=="affinities") {
                    g.DrawRectangle(pen,2,8,5,8);g.DrawRectangle(pen,17,8,5,8);
                    g.DrawLine(pen,8,12,16,12);g.DrawLine(pen,13,9,16,12);g.DrawLine(pen,13,15,16,12);
                }
                else if(name=="combined") {
                    g.DrawRectangle(pen,3,3,12,12);g.DrawRectangle(pen,9,9,12,12);
                }
                else if(name=="separate") {
                    using(var left=Round(new RectangleF(2,3,8,18),2))g.DrawPath(pen,left);
                    using(var right=Round(new RectangleF(14,3,8,18),2))g.DrawPath(pen,right);
                }
                else if(name=="radar") {
                    var points=Enumerable.Range(0,5).Select(i=>new PointF(12+(float)Math.Sin(i*Math.PI*2/5)*9,12-(float)Math.Cos(i*Math.PI*2/5)*9)).ToArray();
                    g.DrawPolygon(pen,points);
                    g.DrawPolygon(pen,new[]{new PointF(12,7),new PointF(18,10),new PointF(15,17),new PointF(8,17),new PointF(9,11)});
                }
                else if(name=="rows" || name=="columns") {
                    g.SmoothingMode=SmoothingMode.AntiAlias;
                    using(var frame=Round(new RectangleF(3,3,18,18),2))g.DrawPath(pen,frame);
                    for(int i=0;i<2;i++) {
                        float divider=9+i*6;
                        if(name=="rows")g.DrawLine(pen,3,divider,21,divider);
                        else g.DrawLine(pen,divider,3,divider,21);
                    }
                }
                else if(name=="moon") {
                    using(var moon=new GraphicsPath()) {
                        moon.AddBezier(15,3,4,0,0,16,9,21);
                        moon.AddBezier(9,21,15,24,21,19,22,14);
                        moon.AddBezier(22,14,13,19,8,9,15,3);
                        moon.CloseFigure();g.DrawPath(pen,moon);
                    }
                }
                else if(name=="system") { g.DrawRectangle(pen,3,4,18,13);g.DrawLine(pen,12,17,12,21);g.DrawLine(pen,8,21,16,21); }
                else if(name=="sun") { g.DrawEllipse(pen,7,7,10,10);for(int i=0;i<8;i++) { double a=i*Math.PI/4;g.DrawLine(pen,12+(float)Math.Cos(a)*8,12+(float)Math.Sin(a)*8,12+(float)Math.Cos(a)*10,12+(float)Math.Sin(a)*10); } }
                else if(name=="def")g.DrawPolygon(pen,new[]{new PointF(12,2),new PointF(20,6),new PointF(20,13),new PointF(17,19),new PointF(12,22),new PointF(7,19),new PointF(4,13),new PointF(4,6)});
                else if(name=="atk") {
                    g.DrawLines(pen,new[]{new PointF(4,3),new PointF(8,4),new PointF(19,16),new PointF(16,19),new PointF(4,7),new PointF(4,3)});
                    g.DrawLine(pen,14,19,20,13);g.DrawLine(pen,18,18,22,22);
                    g.DrawLines(pen,new[]{new PointF(20,3),new PointF(16,4),new PointF(12,8)});g.DrawLine(pen,20,3,20,7);g.DrawLine(pen,20,7,16,11);
                    g.DrawLine(pen,9,14,5,18);g.DrawLine(pen,4,13,10,19);g.DrawLine(pen,6,18,2,22);
                } else if(name=="pin") { g.DrawPolygon(pen,new[]{new PointF(8,3),new PointF(16,3),new PointF(16,10),new PointF(19,14),new PointF(5,14),new PointF(8,10)});g.DrawLine(pen,12,14,12,21); }
                else if(name=="lock") { g.DrawArc(pen,7,3,10,10,180,180);g.DrawLine(pen,7,8,7,10);g.DrawLine(pen,17,8,17,10);using(var body=Round(new RectangleF(5,10,14,11),2))g.DrawPath(pen,body);g.DrawLine(pen,12,14,12,17); }
                else if(name=="help") {
                    g.DrawEllipse(pen,3,3,18,18);
                    using(var mark=new GraphicsPath()) { mark.AddBezier(9,9,9,5,16,6,15,10);mark.AddBezier(15,10,14,12,12,11,12,14);g.DrawPath(pen,mark); }
                    g.DrawLine(pen,12,17,12,17.2f);
                }
                else if(name=="minimize")g.DrawLine(pen,5,12,19,12);
                else if(name=="settings") {
                    var points=new PointF[24];
                    for(int i=0;i<points.Length;i++) { double angle=(i*15-7.5)*Math.PI/180;float radius=i%4==1 || i%4==2?9.5f:7.5f;points[i]=new PointF(12+(float)Math.Cos(angle)*radius,12+(float)Math.Sin(angle)*radius); }
                    g.DrawPolygon(pen,points);g.DrawEllipse(pen,8.5f,8.5f,7,7);
                }
                else { g.DrawLine(pen,7,7,17,17);g.DrawLine(pen,17,7,7,17); }
            }g.Restore(state);
        }
    }
    internal sealed class IconButton : Button {
        internal int Type=-1; internal string Badge="", Glyph=""; internal bool Active, Segment, SegmentStart, SegmentEnd; private bool hover;
        internal RectangleF GroupShape;
        internal string Shortcut="";
        internal IconButton() { SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true); Cursor=Cursors.Hand; TabStop=true; }
        protected override void OnMouseEnter(EventArgs e) { hover=true;Invalidate();base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover=false;Invalidate();base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e) {
            float zoom=WindowZoom.Factor(this);int Width=(int)Math.Round(base.Width/zoom),Height=(int)Math.Round(base.Height/zoom);
            var g=e.Graphics;g.ScaleTransform(zoom,zoom);g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Segment && !SegmentStart && !SegmentEnd?Chrome.Surface:Chrome.Background);
            if(Segment) {
                if(SegmentStart || SegmentEnd) {
                    // Paint the parent's exact contour in local coordinates; do not invent a second corner.
                    Chrome.Box(g,GroupShape,Chrome.Surface,Chrome.Border,Chrome.SelectorRadius);
                    var inner=GroupShape;inner.Inflate(-1,-1);
                    using(var clip=Chrome.Round(inner,Chrome.SelectorRadius-1))g.SetClip(clip);
                }
                using(var fill=new SolidBrush(Active?Chrome.AccentSurface:hover?Chrome.HoverSurface:Chrome.Surface))g.FillRectangle(fill,0,0,Width,Height);
                if(!SegmentEnd)using(var divider=new Pen(Chrome.Surface))g.DrawLine(divider,Width-1,7,Width-1,Height-7);
                if(Active)using(var underline=new Pen(Chrome.Accent,2))g.DrawLine(underline,0,Height-1,Width,Height-1);
                if(Focused && ShowFocusCues)using(var outline=Chrome.Round(new RectangleF(3,3,Width-7,Height-7),5))using(var focus=new Pen(Chrome.AccentForeground,1.5f))g.DrawPath(focus,outline);
            } else Chrome.Box(g,new RectangleF(2,3,Width-5,Height-7),Active?Chrome.AccentSurface:hover?Chrome.HoverSurface:Chrome.Surface,Focused?Chrome.AccentForeground:Active?Chrome.Accent:Chrome.Border,8);
            if(Type>=0)VectorIcon.Draw(g,Type,new RectangleF(11,(Height-20)/2f,20,20));else Chrome.Glyph(g,Glyph,new RectangleF(11,(Height-20)/2f,20,20),Active?Chrome.Accent:Chrome.Muted);
            if(Badge.Length>0) {
                var badgeBounds=new RectangleF(Width-27,Height-13,25,11);
                Chrome.Box(g,badgeBounds,Chrome.AccentSurface,Chrome.AccentSurface,4);
                using(var badgeFont=Chrome.UiFont(Font.FontFamily,6f,FontStyle.Bold))Chrome.Text(g,Badge,badgeFont,Chrome.AccentForeground,badgeBounds,true);
            }
            if(Chrome.ShowBadges && Shortcut.Length>0 && Form.ActiveForm==FindForm()) {
                g.ResetClip();
                int badgeWidth=Math.Max(11,Shortcut.Length*6+3);
                Chrome.Box(g,new RectangleF(Width-badgeWidth-2,1,badgeWidth,11),Chrome.Border,Chrome.Border,3);
                using(var keyFont=Chrome.UiFont("Segoe UI",6.5f,FontStyle.Bold))Chrome.Text(g,Shortcut,keyFont,Chrome.TextColor,new RectangleF(Width-badgeWidth-2,1,badgeWidth,11),true);
            }
        }
    }
    internal sealed class TypeSelector : DrawingPanel {
        internal bool IconModes;
        private RectangleF groupBounds;
        internal void Arrange(bool selected) {
            float zoom=WindowZoom.Factor(this);int x=13;
            var buttons=Controls.OfType<IconButton>().Where(button=>IconModes || button.Type>=0).OrderBy(button=>button.TabIndex).ToArray();
            for(int i=0;i<buttons.Length;i++) {
                var button=buttons[i];button.Segment=true;button.SegmentStart=i==0;button.SegmentEnd=i==buttons.Length-1 && !selected;
                button.SetBounds((int)(x*zoom),(int)(11*zoom),(int)(42*zoom),(int)(36*zoom));x+=42;
            }
            var reset=IconModes?null:Controls.OfType<IconButton>().FirstOrDefault(button=>button.Type<0);
            if(reset!=null) { reset.Visible=selected;reset.Segment=true;reset.SegmentStart=false;reset.SegmentEnd=true;reset.SetBounds((int)(x*zoom),(int)(11*zoom),(int)(40*zoom),(int)(36*zoom));if(selected)x+=40; }
            groupBounds=new RectangleF(12,10,x-11,38);
            foreach(var button in Controls.OfType<IconButton>()) { button.GroupShape=new RectangleF(groupBounds.X-button.Left/zoom,groupBounds.Y-button.Top/zoom,groupBounds.Width,groupBounds.Height);button.Invalidate(); }
            if(base.Width!=(int)((x+12)*zoom))base.Width=(int)((x+12)*zoom);Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);float zoom=WindowZoom.Factor(this);e.Graphics.ScaleTransform(zoom,zoom);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            if(groupBounds.Width>0)Chrome.Box(e.Graphics,groupBounds,Chrome.Surface,Chrome.Border,Chrome.SelectorRadius);
        }
    }
    internal class DrawingPanel : Panel {
        internal bool PhysicalPainting;
        private bool painting;
        public new int Width { get { return painting?(int)Math.Round(base.Width/WindowZoom.Factor(this)):base.Width; } set { base.Width=value; } }
        public new int Height { get { return painting?(int)Math.Round(base.Height/WindowZoom.Factor(this)):base.Height; } set { base.Height=value; } }
        public new Rectangle ClientRectangle { get { return new Rectangle(0,0,Width,Height); } }
        protected override void OnPaint(PaintEventArgs e) {
            var state=e.Graphics.Save();painting=!PhysicalPainting;
            if(painting) { float zoom=WindowZoom.Factor(this);e.Graphics.ScaleTransform(zoom,zoom); }
            try { base.OnPaint(e); } finally { painting=false;e.Graphics.Restore(state); }
        }
        protected override void OnMouseMove(MouseEventArgs e) { float z=PhysicalPainting?1:WindowZoom.Factor(this);base.OnMouseMove(new MouseEventArgs(e.Button,e.Clicks,(int)(e.X/z),(int)(e.Y/z),e.Delta)); }
        internal DrawingPanel() { DoubleBuffered=true;ResizeRedraw=true; }
    }
}
