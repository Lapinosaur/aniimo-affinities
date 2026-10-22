using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AniimoAffinities {
    // Native caption stays present until the replacement controls have handles and click handlers.
    internal class PortableWindow : Form {
        internal float AppliedWindowScale=1;
        internal void ApplyWindowScale() {
            if(!customCaption || !(this is MainForm || this is RadarDialog || this is MatrixDialog))return;
            WindowZoom.Apply(this,Chrome.WindowScale);RefreshWindowFrame();PositionUnlockWindow();
            if(unlockWindow!=null)unlockWindow.RefreshSizes();
        }

        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr window,int message,IntPtr w,IntPtr l);
        private bool customCaption;
        private readonly Timer attentionTimer=new Timer { Interval=16 };
        private readonly System.Diagnostics.Stopwatch attentionWatch=new System.Diagnostics.Stopwatch();
        private readonly System.Diagnostics.Stopwatch attentionSoundWatch=new System.Diagnostics.Stopwatch();
        private Point attentionOrigin;
        private PortableWindow BlockingDialog() {
            foreach(var owned in OwnedForms) {
                var dialog=owned as PortableWindow;
                if(dialog!=null && dialog.Visible && dialog.Modal)return dialog.BlockingDialog() ?? dialog;
            }
            return null;
        }
        private bool RequestAttention() {
            if(attentionTimer.Enabled)return false;
            Activate();attentionOrigin=Location;attentionWatch.Restart();attentionTimer.Start();
            if(attentionSoundWatch.IsRunning && attentionSoundWatch.ElapsedMilliseconds<800)return false;
            attentionSoundWatch.Restart();return true;
        }
        internal static Point AttentionOffset(double progress) {
            if(progress<=0 || progress>=1)return Point.Empty;
            double decay=(1-progress)*(1-progress);
            return new Point((int)Math.Round(Math.Sin(progress*Math.PI*8)*8*decay),(int)Math.Round(Math.Sin(progress*Math.PI*10)*5*decay));
        }
        private void StopAttention() {
            if(!attentionTimer.Enabled)return;
            attentionTimer.Stop();attentionWatch.Reset();
            if(!IsDisposed)Location=attentionOrigin;
        }
        private readonly DrawingPanel windowFrame=new DrawingPanel { TabStop=false };
        // Inputs: current window mode and size. Output: a shared rounded frame; overlays stay frameless.
        private void RefreshWindowFrame() {
            if(!customCaption || windowFrame==null || ClientSize.Width<2 || ClientSize.Height<2)return;
            bool framed=!TopMost && WindowState!=FormWindowState.Maximized;
            var previous=Region;
            if(framed)using(var outline=Chrome.Round(new RectangleF(0,0,ClientSize.Width,ClientSize.Height),Chrome.WindowRadius))Region=new Region(outline);
            else Region=null;
            if(previous!=null)previous.Dispose();
            Padding=framed?new Padding(2):Padding.Empty;
            windowFrame.Visible=framed;
            if(!framed)return;
            windowFrame.Bounds=ClientRectangle;
            using(var outside=Chrome.Round(new RectangleF(0,0,ClientSize.Width,ClientSize.Height),Chrome.WindowRadius))
            using(var inside=Chrome.Round(new RectangleF(2,2,ClientSize.Width-4,ClientSize.Height-4),Chrome.WindowRadius-2)) {
                var ring=new Region(outside);ring.Exclude(inside);
                var old=windowFrame.Region;windowFrame.Region=ring;if(old!=null)old.Dispose();
            }
            windowFrame.BringToFront();windowFrame.Invalidate();
        }
        protected override void OnSizeChanged(EventArgs e) { base.OnSizeChanged(e);RefreshWindowFrame(); }
        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr window,int index);
        [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr window,int index,int value);
        [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr window,int id,uint modifiers,uint key);
        [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr window,int id);
        private readonly Timer holdTimer=new Timer { Interval=16 };
        private readonly System.Diagnostics.Stopwatch holdWatch=new System.Diagnostics.Stopwatch();
        private Button pinControl;private Panel captionPanel;
        protected Control TutorialCaptionTarget(string name) { return name=="pin"?pinControl:name=="matrix"?matrixControl:name=="radar"?radarControl:null; }
        private bool suppressPinClick,unlockRegistered;
        private int unlockedStyle;
        private UnlockWindow unlockWindow;
        internal bool OverlayLocked { get; private set; }
        protected virtual void OnOverlayLockChanged() { }
        private float holdProgress;
        private void CancelHold() { holdTimer.Stop();holdWatch.Reset();holdProgress=0;if(pinControl!=null)pinControl.Invalidate(); }
        protected void SetOverlayLocked(bool locked) {
            if(locked==OverlayLocked)return;
            if(locked) {
                var shortcut=KeyBindings.Load(KeyBindings.FilePath)[11];
                uint mods=0x4000u|((shortcut&Keys.Control)!=0?2u:0u)|((shortcut&Keys.Alt)!=0?1u:0u)|((shortcut&Keys.Shift)!=0?4u:0u);
                unlockRegistered=shortcut!=Keys.None && RegisterHotKey(Handle,0x414E,mods,(uint)(shortcut&Keys.KeyCode));
                SetPinned(true);unlockedStyle=GetWindowLong(Handle,-20);
                OverlayLocked=true;SetWindowLong(Handle,-20,unlockedStyle|0x20|0x08000000);
            } else {
                SetWindowLong(Handle,-20,unlockedStyle);OverlayLocked=false;
                if(unlockRegistered)UnregisterHotKey(Handle,0x414E);unlockRegistered=false;
            }
            if(captionPanel!=null)foreach(Control control in captionPanel.Controls)control.Visible=!locked || control==pinControl;
            if(pinControl!=null) { pinControl.AccessibleName=locked?"Locked overlay · Ctrl+Alt+L to unlock":"Always on top";pinControl.Invalidate(); }
            if(locked && pinControl!=null) {
                unlockWindow=new UnlockWindow(()=>SetOverlayLocked(false),()=>Close(),unlockRegistered,()=>Location,point=> {
                    Location=KeepCaptionVisible(new Rectangle(point,Size),Screen.FromPoint(Cursor.Position).WorkingArea,54).Location;
                });
                PositionUnlockWindow();unlockWindow.Show(this);
            } else if(unlockWindow!=null) {
                var window=unlockWindow;unlockWindow=null;window.Dispose();
            }
            CancelHold();OnOverlayLockChanged();Invalidate(true);
        }
        private void PositionUnlockWindow() {
            if(unlockWindow!=null && pinControl!=null)unlockWindow.Bounds=new Rectangle(pinControl.PointToScreen(new Point((pinControl.Width-32)/2-140,Math.Max(0,(pinControl.Height-32)/2))),new Size(172,32));
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct WindowRect { public int Left,Top,Right,Bottom; }
        // Inputs: proposed bounds and destination monitor work area. Output: reachable caption.
        // Horizontal overlap allows crossing monitors; vertical bounds keep the full caption visible.
        internal static Rectangle KeepCaptionVisible(Rectangle bounds,Rectangle workArea,int captionHeight) {
            int top=Math.Max(workArea.Top,Math.Min(bounds.Top,workArea.Bottom-captionHeight));
            int left=Math.Max(workArea.Left-bounds.Width+48,Math.Min(bounds.Left,workArea.Right-48));
            return new Rectangle(left,top,bounds.Width,bounds.Height);
        }
        private readonly ApplicationTips captionTips=ApplicationTips.Create();
        protected Control CaptionContent { get; set; }
        protected virtual void OpenSettings() { }
        protected virtual void OpenMatrix() { }
        protected virtual void OpenRadar() { }
        protected virtual bool InlineRadar { get { return false; } }
        protected virtual void ToggleInlineRadar() { }
        protected virtual void ToggleInlineMatrix() { }
        protected virtual string RadarViewGlyph { get { return "radar"; } }
        protected virtual string MatrixViewGlyph { get { return "matrix"; } }
        private static string ViewName(string view) { return view=="matrix"?"Effectiveness matrix":view=="radar"?"Affinity radar":"Affinity diagrams"; }
        private static string ViewTip(string view,string key) {
            return ViewName(view)+"\nClick: open in a separate window"+(key.Length>0?" ("+key+")":"")+"\nHold for 0.9 s: swap this view with the main view. The button takes the previous view's icon.";
        }
        private Button viewHoldTarget;
        private bool holdingMatrix;
        private readonly Timer radarHoldTimer=new Timer { Interval=16 };
        private readonly System.Diagnostics.Stopwatch radarHoldWatch=new System.Diagnostics.Stopwatch();
        private bool suppressRadarClick;
        private float radarHoldProgress;
        private void CancelRadarHold() { radarHoldTimer.Stop();radarHoldWatch.Reset();radarHoldProgress=0;if(radarControl!=null)radarControl.Invalidate();if(matrixControl!=null)matrixControl.Invalidate(); }
        protected virtual Keys RadarShortcut { get { return Keys.None; } }
        private Button radarControl;
        protected void RefreshRadarShortcut() {
            if(radarControl==null)return;
            string key=KeyBindings.Label(RadarShortcut);
            captionTips.SetToolTip(radarControl,ViewTip(RadarViewGlyph,key));radarControl.AccessibleName=ViewName(RadarViewGlyph);
            radarControl.AccessibleDescription=key.Length>0?"Shortcut: "+key:"No shortcut";radarControl.Invalidate();
        }
        protected virtual Keys MatrixShortcut { get { return Keys.None; } }
        private Button matrixControl;
        protected void RefreshMatrixShortcut() {
            if(matrixControl==null)return;
            string key=KeyBindings.Label(MatrixShortcut);
            captionTips.SetToolTip(matrixControl,ViewTip(MatrixViewGlyph,key));matrixControl.AccessibleName=ViewName(MatrixViewGlyph);
            matrixControl.AccessibleDescription=key.Length>0?"Shortcut: "+key:"No shortcut";matrixControl.Invalidate();
        }
        protected virtual string CaptionGlyph { get { return "settings"; } }
        protected virtual bool CompactCaption { get { return false; } }
        protected override bool ProcessCmdKey(ref Message message,Keys input) {
            if(!(this is DisplayConfirmationDialog) && WindowZoom.HandleShortcut(input,message.LParam))return true;
            if(input==Keys.Escape) { Close();return true; }
            return base.ProcessCmdKey(ref message,input);
        }
        // Inputs: passive surface. Output: native window dragging on left press.
        // Child controls retain their own mouse handlers. Fallback: ignore other buttons. Raises: none.
        protected void EnableWindowDrag(Control surface) {
            surface.Cursor=Cursors.SizeAll;
            surface.MouseDown+=DragSurface;
        }
        private void DragSurface(object sender,MouseEventArgs e) {
            if(e.Button!=MouseButtons.Left)return;
            ReleaseCapture();SendMessage(Handle,0xA1,new IntPtr(2),IntPtr.Zero);
        }
        // Inputs: pin state. Output: topmost overlay with transparent background, or normal opaque window.
        // Keep the dark key equal to the painted background so antialiased text has no colored fringe.
        internal void SetPinned(bool pinned) {
            TopMost=pinned;
            TransparencyKey=pinned?Chrome.Background:Color.Empty;
            RefreshWindowFrame();
            Invalidate(true);
        }
        private readonly Icon applicationIcon;
        private readonly Image coffeeArtwork;
        internal PortableWindow() {
            radarHoldTimer.Tick+=(s,e)=> {
                if(viewHoldTarget==null || !viewHoldTarget.ClientRectangle.Contains(viewHoldTarget.PointToClient(Cursor.Position)) || (Control.MouseButtons&MouseButtons.Left)==0) { CancelRadarHold();return; }
                radarHoldProgress=Math.Min(1f,radarHoldWatch.ElapsedMilliseconds/900f);viewHoldTarget.Invalidate();
                if(radarHoldProgress>=1) { suppressRadarClick=true;CancelRadarHold();if(holdingMatrix)ToggleInlineMatrix();else ToggleInlineRadar(); }
            };
            Deactivate+=(s,e)=>CancelRadarHold();
            attentionTimer.Tick+=(s,e)=> {
                double progress=attentionWatch.Elapsed.TotalMilliseconds/320;
                if(progress>=1 || !Visible) { StopAttention();return; }
                var offset=AttentionOffset(progress);
                Location=new Point(attentionOrigin.X+offset.X,attentionOrigin.Y+offset.Y);
            };
            FormClosing+=(s,e)=>StopAttention();
            windowFrame.Paint+=(s,e)=> {
                e.Graphics.Clear(Chrome.Background);e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using(var outline=Chrome.Round(new RectangleF(0.5f,0.5f,windowFrame.Width-1,windowFrame.Height-1),Chrome.WindowRadius-0.5f))
                using(var pen=new Pen(Chrome.Border,1))e.Graphics.DrawPath(pen,outline);
            };
            EnableWindowDrag(windowFrame);
            Deactivate+=(s,e)=>CancelHold();
            LocationChanged+=(s,e)=>PositionUnlockWindow();SizeChanged+=(s,e)=>PositionUnlockWindow();
            holdTimer.Tick+=(s,e)=> {
                if(pinControl==null || !pinControl.ClientRectangle.Contains(pinControl.PointToClient(Cursor.Position)) || (Control.MouseButtons&MouseButtons.Left)==0) { CancelHold();return; }
                holdProgress=Math.Min(1f,holdWatch.ElapsedMilliseconds/1200f);pinControl.Invalidate();
                if(holdProgress>=1) { suppressPinClick=true;CancelHold();ReleaseCapture();SetOverlayLocked(true); }
            };
            using(var stream=typeof(PortableWindow).Assembly.GetManifestResourceStream("app.ico"))
            using(var embedded=new Icon(stream))applicationIcon=(Icon)embedded.Clone();
            Icon=applicationIcon;ShowIcon=true;
            using(var stream=typeof(PortableWindow).Assembly.GetManifestResourceStream("buy-me-a-coffee.png"))
            using(var image=Image.FromStream(stream))coffeeArtwork=new Bitmap(image);
        }
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            if(disposing) { radarHoldTimer.Dispose();attentionTimer.Dispose();if(unlockWindow!=null)unlockWindow.Dispose();holdTimer.Dispose();applicationIcon.Dispose();coffeeArtwork.Dispose();captionTips.Dispose(); }
        }
        protected override void OnHandleDestroyed(EventArgs e) { if(unlockRegistered) { UnregisterHotKey(Handle,0x414E);unlockRegistered=false; }base.OnHandleDestroyed(e); }
        protected override void OnShown(EventArgs e) {
            base.OnShown(e);
            BeginInvoke(new Action(()=> {
                if(IsDisposed || Disposing || !Visible)return;
                var caption=new Panel { Dock=DockStyle.Top,Height=CompactCaption?36:54,BackColor=Chrome.Background };
                captionPanel=caption;
                var title=new Label { Text=CaptionContent==null?Text:"",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(40,0,0,0),ForeColor=Chrome.Muted };
                if(CompactCaption)title.Paint+=(s,a)=> {
                    a.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    a.Graphics.ScaleTransform(AppliedWindowScale,AppliedWindowScale);
                    Chrome.Glyph(a.Graphics,CaptionGlyph,new RectangleF(14,(title.Height/AppliedWindowScale-16)/2f,16,16),Chrome.Muted);
                };
                var close=CaptionButton("×","Close (Alt+F4)",true);
                if(CompactCaption) { close.TabStop=false;captionTips.SetToolTip(close,"Close · Esc"); }
                var settings=CaptionButton("⚙","Settings");
                var matrix=CaptionButton("matrix","Effectiveness matrix");
                matrixControl=matrix;matrix.Click+=(s,a)=>{if(suppressRadarClick) { suppressRadarClick=false;return; }OpenMatrix();};
                matrix.MouseDown+=(s,a)=>{if(a.Button==MouseButtons.Left) { viewHoldTarget=matrix;holdingMatrix=true;suppressRadarClick=false;radarHoldWatch.Restart();radarHoldTimer.Start(); }};
                matrix.MouseUp+=(s,a)=>CancelRadarHold();matrix.MouseLeave+=(s,a)=>CancelRadarHold();matrix.MouseCaptureChanged+=(s,a)=>{if(!matrix.Capture)CancelRadarHold();};RefreshMatrixShortcut();
                var radar=CaptionButton("radar","Affinity radar");
                radarControl=radar;radar.Click+=(s,a)=>{if(suppressRadarClick) { suppressRadarClick=false;return; }OpenRadar();};RefreshRadarShortcut();
                radar.MouseDown+=(s,a)=>{if(a.Button==MouseButtons.Left) { viewHoldTarget=radar;holdingMatrix=false;suppressRadarClick=false;radarHoldWatch.Restart();radarHoldTimer.Start(); }};
                radar.MouseUp+=(s,a)=>CancelRadarHold();radar.MouseLeave+=(s,a)=>CancelRadarHold();radar.MouseCaptureChanged+=(s,a)=>{if(!radar.Capture)CancelRadarHold();};
                settings.Font=Chrome.UiFont("Segoe UI Symbol",13);
                captionTips.SetToolTip(settings,"Settings · Keyboard shortcuts");
                settings.Click+=(s,a)=>OpenSettings();
                EnableWindowDrag(title);
                EnableWindowDrag(caption);
                var coffee=CaptionButton("","Buy Me a Coffee - Lapinosaur");
                coffee.Width=144;
                coffee.Paint+=(s,a)=> {
                    a.Graphics.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    a.Graphics.ScaleTransform(AppliedWindowScale,AppliedWindowScale);
                    float width=134,height=width*coffeeArtwork.Height/coffeeArtwork.Width;
                    a.Graphics.DrawImage(coffeeArtwork,5,(coffee.Height/AppliedWindowScale-height)/2,width,height);
                };
                coffee.Cursor=Cursors.Hand;
                captionTips.SetToolTip(coffee,"Buy Me a Coffee · Lapinosaur");
                coffee.Click+=(s,a)=> {
                    try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://buymeacoffee.com/lapinosaur") { UseShellExecute=true }); }
                    catch(System.ComponentModel.Win32Exception) { MessageBox.Show(this,"https://buymeacoffee.com/lapinosaur","Open in your browser",MessageBoxButtons.OK,MessageBoxIcon.Information); }
                };
                var pin=CaptionButton("","Always on top",false,true);
                pinControl=pin;
                pin.MouseDown+=(s,a)=> { if(a.Button==MouseButtons.Left) { suppressPinClick=false;holdWatch.Restart();holdTimer.Start(); } };
                pin.MouseUp+=(s,a)=>CancelHold();pin.MouseLeave+=(s,a)=>CancelHold();
                pin.MouseCaptureChanged+=(s,a)=>{if(!pin.Capture)CancelHold();};
                pin.Click+=(s,a)=>{if(suppressPinClick || OverlayLocked)return;SetPinned(!TopMost);pin.AccessibleName=TopMost?"Disable always on top":"Always on top";pin.Invalidate();};
                captionTips.SetToolTip(pin,"Click to pin · Hold to lock overlay");
                var minimize=CaptionButton("−","Minimize");
                captionTips.SetToolTip(close,"Close · Esc / Alt+F4");captionTips.SetToolTip(minimize,"Minimize");
                close.Click+=(s,a)=>Close();minimize.Click+=(s,a)=>WindowState=FormWindowState.Minimized;
                title.DoubleClick+=(s,a)=> { if(!CompactCaption)WindowState=WindowState==FormWindowState.Maximized?FormWindowState.Normal:FormWindowState.Maximized; };
                caption.Controls.Add(title);
                if(CaptionContent!=null) {
                    CaptionContent.Dock=DockStyle.Left;caption.Controls.Add(CaptionContent);
                }
                if(!CompactCaption) { caption.Controls.Add(matrix);caption.Controls.Add(radar);caption.Controls.Add(pin);caption.Controls.Add(coffee);caption.Controls.Add(settings);caption.Controls.Add(minimize); }
                else { matrix.Dispose();radar.Dispose();settings.Dispose();coffee.Dispose();pin.Dispose();minimize.Dispose(); }
                caption.Controls.Add(close);
                Controls.Add(caption);caption.BringToFront();
                // Docking order: title above the existing selector, with the canvas filling the rest.
                Controls.SetChildIndex(caption,Controls.Count-1);
                caption.CreateControl();close.CreateControl();
                if(IsDisposed || Disposing)return;
                var size=ClientSize;FormBorderStyle=FormBorderStyle.None;ClientSize=size;customCaption=true;
                windowFrame.PhysicalPainting=true;Controls.Add(windowFrame);RefreshWindowFrame();ApplyWindowScale();
            }));
        }
        // Inputs: caption action. Output: consistent rounded surface, hover and keyboard focus.
        // Artwork-only actions keep their supplied branding. Raises: none.
        private Button CaptionButton(string text,string label,bool danger=false,bool pin=false) {
            var button=new Button { Text="",AccessibleName=label,Dock=DockStyle.Right,Width=42,FlatStyle=FlatStyle.Flat,ForeColor=Chrome.Muted,BackColor=Chrome.Background,TabStop=true };
            button.Cursor=Cursors.Hand;button.FlatAppearance.BorderSize=0;
            button.FlatAppearance.MouseOverBackColor=Chrome.Background;button.FlatAppearance.MouseDownBackColor=Chrome.Background;
            bool hover=false,pressed=false;
            button.MouseEnter+=(s,e)=>{hover=true;button.Invalidate();};
            button.MouseLeave+=(s,e)=>{hover=false;pressed=false;button.Invalidate();};
            button.MouseDown+=(s,e)=>{pressed=e.Button==MouseButtons.Left;button.Invalidate();};
            button.MouseUp+=(s,e)=>{pressed=false;button.Invalidate();};
            button.GotFocus+=(s,e)=>button.Invalidate();button.LostFocus+=(s,e)=>button.Invalidate();
            button.Paint+=(s,e)=> {
                float zoom=AppliedWindowScale;int bw=(int)Math.Round(button.Width/zoom),bh=(int)Math.Round(button.Height/zoom);
                var g=e.Graphics;g.ScaleTransform(zoom,zoom);g.Clear(Chrome.Background);g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                if(text.Length==0 && !pin)return;
                var bounds=new RectangleF((bw-28)/2f,(bh-28)/2f,28,28);
                bool active=pin && TopMost;
                Color fill=danger && (TopMost || hover)?Color.FromArgb(185,45,55):active?Chrome.AccentSurface:Chrome.Surface;
                if(hover)fill=danger?Color.FromArgb(225,65,75):active?Chrome.AccentHover:Chrome.HoverSurface;
                if(pressed)fill=ControlPaint.Dark(fill);
                bool hasHold=pin || text=="matrix" || text=="radar";
                Chrome.Box(g,bounds,fill,active?Chrome.Accent:hasHold?Chrome.Border:fill,Chrome.ControlRadius);
                if(hasHold)Chrome.HoldHint(g,bounds);
                if(button.Focused && ShowFocusCues) {
                    bounds.Inflate(2,2);
                    using(var outline=Chrome.Round(bounds,Chrome.ControlRadius+2))using(var pen=new Pen(Chrome.AccentForeground))g.DrawPath(pen,outline);
                }
                Color ink=danger && (TopMost || hover)?Color.White:active?Chrome.AccentForeground:Chrome.TextColor;
                if(pin && OverlayLocked)Chrome.Glyph(g,"lock",new RectangleF((bw-16)/2f,(bh-16)/2f,16,16),ink);
                else if(pin)Chrome.Glyph(g,"pin",new RectangleF((bw-16)/2f,(bh-16)/2f,16,16),ink);
                else if(text=="matrix" || text=="radar")Chrome.Glyph(g,text=="radar"?RadarViewGlyph:MatrixViewGlyph,new RectangleF((bw-16)/2f,(bh-16)/2f,16,16),ink);
                else if(text=="⚙" || text=="−")Chrome.Glyph(g,text=="⚙"?"settings":"minimize",new RectangleF((bw-16)/2f,(bh-16)/2f,16,16),ink);
                else Chrome.Text(g,text,button.Font,ink,new RectangleF(0,0,bw,bh),true);
                Keys shortcut=danger?Keys.Escape:text=="matrix"?MatrixShortcut:text=="radar"?RadarShortcut:Keys.None;
                if(Chrome.ShowBadges && shortcut!=Keys.None && Form.ActiveForm==this) {
                    string key=shortcut==Keys.Escape?"Esc":KeyBindings.Label(shortcut);
                    int width=Math.Min(bw-2,Math.Max(15,key.Length*5+4));
                    var badge=new RectangleF(bw-width-1,3,width,12);
                    Chrome.Box(g,badge,Chrome.Border,Chrome.Border,3);
                    using(var font=Chrome.UiFont("Segoe UI",key.Length>7?5.5f:6.5f,FontStyle.Bold))Chrome.Text(g,key,font,Chrome.TextColor,badge,true);
                }
                float progress=pin?holdProgress:((text=="radar" && !holdingMatrix)||(text=="matrix" && holdingMatrix))?radarHoldProgress:0;
                if(progress>0) {
                    using(var path=Chrome.Round(new RectangleF((bw-32)/2f,(bh-32)/2f,32,32),Chrome.ControlRadius+2)) {
                        path.Flatten();var points=path.PathPoints;float length=0;
                        for(int i=0;i<points.Length;i++) { var a=points[i];var b=points[(i+1)%points.Length];length+=(float)Math.Sqrt((b.X-a.X)*(b.X-a.X)+(b.Y-a.Y)*(b.Y-a.Y)); }
                        float remaining=length*progress;
                        using(var pen=new Pen(Chrome.Accent,2))for(int i=0;i<points.Length && remaining>0;i++) {
                            var a=points[i];var b=points[(i+1)%points.Length];float segment=(float)Math.Sqrt((b.X-a.X)*(b.X-a.X)+(b.Y-a.Y)*(b.Y-a.Y));
                            if(segment<=0)continue;float fraction=Math.Min(1,remaining/segment);
                            g.DrawLine(pen,a,new PointF(a.X+(b.X-a.X)*fraction,a.Y+(b.Y-a.Y)*fraction));remaining-=segment;
                        }
                    }
                }
            };
            return button;
        }
        // Inputs: native hit tests. Output: resize borders for the custom frame. Fallback: native window handling (including close/Alt+F4). Raises: none.
        protected override void WndProc(ref Message message) {
            // Retain the first native sound, but swallow repeated blocked clicks while it may still be playing.
            if(message.Msg==0x20 && (short)(message.LParam.ToInt64()&0xffff)==-2) {
                var blocking=BlockingDialog();
                if(blocking!=null) {
                    int mouseMessage=(int)((message.LParam.ToInt64()>>16)&0xffff);
                    if(mouseMessage==0x201 || mouseMessage==0x204 || mouseMessage==0x207 || mouseMessage==0xA1) {
                        if(!blocking.RequestAttention()) { message.Result=new IntPtr(1);return; }
                    }
                }
            }
            if(message.Msg==0x312 && message.WParam.ToInt32()==0x414E) { SetOverlayLocked(false);return; }
            base.WndProc(ref message);
            if(customCaption && message.Msg==0x216 && WindowState==FormWindowState.Normal) {
                var proposed=(WindowRect)Marshal.PtrToStructure(message.LParam,typeof(WindowRect));
                var destination=Screen.FromPoint(Cursor.Position).WorkingArea;
                var bounds=KeepCaptionVisible(Rectangle.FromLTRB(proposed.Left,proposed.Top,proposed.Right,proposed.Bottom),destination,CompactCaption?36:54);
                proposed.Left=bounds.Left;proposed.Top=bounds.Top;proposed.Right=bounds.Right;proposed.Bottom=bounds.Bottom;
                Marshal.StructureToPtr(proposed,message.LParam,false);message.Result=new IntPtr(1);return;
            }
            if(!customCaption || CompactCaption || message.Msg!=0x84 || WindowState!=FormWindowState.Normal || message.Result!=new IntPtr(1))return;
            long position=message.LParam.ToInt64();var p=PointToClient(new Point((short)(position&0xffff),(short)((position>>16)&0xffff)));
            bool left=p.X<5,right=p.X>=ClientSize.Width-5,top=p.Y<5,bottom=p.Y>=ClientSize.Height-5;
            int hit=top?(left?13:right?14:12):bottom?(left?16:right?17:15):left?10:right?11:1;
            message.Result=new IntPtr(hit);
        }
    }
    // Independent interactive island above the click-through overlay.
    internal sealed class UnlockWindow : Form {
        protected override bool ProcessCmdKey(ref Message message,Keys input) {
            if(WindowZoom.HandleShortcut(input,message.LParam)) { RefreshSizes();return true; }
            return base.ProcessCmdKey(ref message,input);
        }
        private readonly Action closeApplication;
        private readonly HoldSizeButton[] sizeButtons=new HoldSizeButton[2];
        internal void RefreshSizes() {
            var values=WindowZoom.Neighbors(Chrome.WindowScale);
            for(int i=0;i<2;i++) {
                var button=sizeButtons[i];if(button==null)continue;
                button.Target=values[i];button.Text=Math.Round(values[i]*100)+"%";button.AccessibleName="Resize overlay to "+button.Text;
                tips.SetToolTip(button,"Hold for 0.9 s to resize to "+button.Text+". Release early to cancel.");button.Invalidate();
            }
        }
        private readonly ApplicationTips tips=ApplicationTips.Create();
        internal UnlockWindow(Action unlock,Action close,bool hotkeyAvailable,Func<Point> getLocation,Action<Point> move) {
            closeApplication=close;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;TopMost=true;
            BackColor=Chrome.Surface;ClientSize=new Size(172,32);
            var shape=new Region();shape.MakeEmpty();
            foreach(var bounds in new[]{new RectangleF(0,0,48,32),new RectangleF(52,0,48,32),new RectangleF(104,0,32,32),new RectangleF(140,0,32,32)})
                using(var outline=Chrome.Round(bounds,8))shape.Union(outline);
            Region=shape;
            for(int i=0;i<2;i++) {
                var sizeButton=new HoldSizeButton { Location=new Point(i*52,0),Size=new Size(48,32),TabIndex=i };
                sizeButtons[i]=sizeButton;Controls.Add(sizeButton);
                sizeButton.Commit+=value=> {
                    try { Chrome.SaveDisplay(Chrome.FontScale,Chrome.ShowBadges,value);RefreshSizes(); }
                    catch(System.IO.IOException) { tips.SetToolTip(sizeButton,"Unable to save window size. Hold to try again."); }
                    catch(UnauthorizedAccessException) { tips.SetToolTip(sizeButton,"Unable to save window size. Hold to try again."); }
                };
            }
            RefreshSizes();
            var button=new Button { Location=new Point(140,0),Size=new Size(32,32),FlatStyle=FlatStyle.Flat,Text="",AccessibleName="Unlock overlay",Cursor=Cursors.Hand,BackColor=Chrome.AccentSurface };
            button.FlatAppearance.BorderSize=0;button.FlatAppearance.MouseOverBackColor=Chrome.AccentHover;
            button.Paint+=(s,e)=> {
                e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                Chrome.Glyph(e.Graphics,"lock",new RectangleF(8,8,16,16),Chrome.AccentForeground);
            };
            button.Click+=(s,e)=>unlock();Controls.Add(button);tips.SetToolTip(button,hotkeyAvailable?"Unlock overlay · "+KeyBindings.Label(KeyBindings.Load(KeyBindings.FilePath)[11]):"Unlock overlay");
            var grip=new Button { Location=new Point(104,0),Size=new Size(32,32),FlatStyle=FlatStyle.Flat,Text="",AccessibleName="Move overlay",Cursor=Cursors.SizeAll,BackColor=Chrome.Surface };
            grip.FlatAppearance.BorderSize=0;grip.FlatAppearance.MouseOverBackColor=Chrome.Surface;
            grip.Paint+=(s,e)=> {
                e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using(var pen=new Pen(Chrome.TextColor,1.5f)) {
                    e.Graphics.DrawLine(pen,8,16,24,16);e.Graphics.DrawLine(pen,16,8,16,24);
                    e.Graphics.DrawLines(pen,new[]{new Point(11,13),new Point(8,16),new Point(11,19)});
                    e.Graphics.DrawLines(pen,new[]{new Point(21,13),new Point(24,16),new Point(21,19)});
                    e.Graphics.DrawLines(pen,new[]{new Point(13,11),new Point(16,8),new Point(19,11)});
                    e.Graphics.DrawLines(pen,new[]{new Point(13,21),new Point(16,24),new Point(19,21)});
                }
            };
            bool dragging=false;Point pointerStart=Point.Empty,windowStart=Point.Empty;
            grip.MouseDown+=(s,e)=> { if(e.Button!=MouseButtons.Left)return;pointerStart=Cursor.Position;windowStart=getLocation();dragging=true;grip.Capture=true; };
            grip.MouseMove+=(s,e)=> { if(dragging)move(new Point(windowStart.X+Cursor.Position.X-pointerStart.X,windowStart.Y+Cursor.Position.Y-pointerStart.Y)); };
            grip.MouseUp+=(s,e)=> { if(e.Button==MouseButtons.Left) { dragging=false;grip.Capture=false; } };
            grip.MouseCaptureChanged+=(s,e)=> { if(!grip.Capture)dragging=false; };
            Controls.Add(grip);tips.SetToolTip(grip,"Hold and drag to move overlay");
        }
        protected override void OnDeactivate(EventArgs e) { foreach(var button in sizeButtons)button.CancelHold();base.OnDeactivate(e); }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override void OnFormClosing(FormClosingEventArgs e) {
            if(e.CloseReason==CloseReason.UserClosing) { e.Cancel=true;closeApplication();return; }
            base.OnFormClosing(e);
        }
        protected override void Dispose(bool disposing) { if(disposing)tips.Dispose();base.Dispose(disposing); }
    }
}
