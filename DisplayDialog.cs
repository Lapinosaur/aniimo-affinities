using System;
using System.Drawing;
using System.Windows.Forms;

namespace AniimoAffinities {
    internal sealed class DisplayDialog : PortableWindow {
        protected override bool CompactCaption { get { return true; } }
        private float scale=Chrome.FontScale,windowScale=Chrome.WindowScale;
        private bool badges=Chrome.ShowBadges;
        private readonly Form settingsHost;
        private Panel zoomChoices;
        private void SyncZoom() {
            windowScale=Chrome.WindowScale;
            if(zoomChoices!=null)foreach(DisplayChoice choice in zoomChoices.Controls)choice.Checked=Math.Abs(choice.PreviewScale-windowScale)<.01;
        }
        protected override void Dispose(bool disposing) { if(disposing)Chrome.DisplayChanged-=SyncZoom;base.Dispose(disposing); }
        protected override void OnShown(EventArgs e) { if(settingsHost==null)base.OnShown(e); }
        protected override bool ProcessCmdKey(ref Message message,Keys input) {
            if(settingsHost!=null && input==Keys.Escape) { settingsHost.Close();return true; }
            return base.ProcessCmdKey(ref message,input);
        }
        internal DisplayDialog(Form host=null,Func<int> mainType=null,Func<Keys[]> previewBindings=null) {
            settingsHost=host;
            int fallbackType=new Random().Next(Model.Names.Length);
            Func<int> previewType=()=> { int selected=mainType==null?-1:mainType();return selected>=0 && selected<Model.Names.Length?selected:fallbackType; };
            if(previewBindings==null) { var savedBindings=KeyBindings.Load(KeyBindings.FilePath);previewBindings=()=>savedBindings; }
            Text="Display settings";ClientSize=new Size(440,460);StartPosition=FormStartPosition.CenterParent;
            MaximizeBox=false;MinimizeBox=false;ShowInTaskbar=false;BackColor=Chrome.Background;ForeColor=Chrome.TextColor;Font=Chrome.UiFont("Segoe UI",9);
            Controls.Add(new Label { Text="Text size",Location=new Point(24,52),Size=new Size(380,24) });
            for(int i=0;i<3;i++) {
                float value=0.9f+i*0.1f;
                var preview=new DisplayChoice {
                    Text=new[]{"Small · 90%","Default · 100%","Large · 110%"}[i],PreviewScale=value,
                    Location=new Point(24+i*132,80),Size=new Size(128,100),TabIndex=i,
                    Checked=Math.Abs(scale-value)<.01,AccessibleName="Text size: "+Math.Round(value*100)+" percent"
                };
                preview.CheckedChanged+=(s,e)=> { if(preview.Checked)scale=value; };
                Controls.Add(preview);
            }
            Controls.Add(new Label { Text="Keyboard badges",Location=new Point(24,326),Size=new Size(200,26) });
            var badgeChoices=new Panel { Location=new Point(224,320),Size=new Size(192,72),TabIndex=8,AccessibleName="Keyboard badge visibility" };
            Controls.Add(badgeChoices);
            for(int i=0;i<2;i++) {
                bool visible=i==0;
                var choice=new DisplayChoice {
                    Text=visible?"Shown":"Hidden",BadgePreview=true,PreviewBadges=visible,PreviewType=previewType,PreviewBindings=previewBindings,
                    Location=new Point(i*98,0),Size=new Size(94,72),TabIndex=i,
                    Checked=badges==visible,AccessibleName=visible?"Show keyboard badges":"Hide keyboard badges"
                };
                choice.CheckedChanged+=(s,e)=> { if(choice.Checked)badges=visible; };
                badgeChoices.Controls.Add(choice);
            }
            Controls.Add(new Label { Text="Main / Sub labels always remain visible.",ForeColor=Chrome.Muted,Location=new Point(24,354),Size=new Size(392,28) });
            var status=new Label { ForeColor=Chrome.Error,Location=new Point(24,376),Size=new Size(392,22) };Controls.Add(status);
            var cancel=new SettingsButton { Text="Cancel",DialogResult=DialogResult.Cancel,Location=new Point(196,400),Size=new Size(106,34),TabIndex=4 };
            var save=new SettingsButton { Text="Save",Primary=true,Location=new Point(310,400),Size=new Size(106,34),TabIndex=5 };
            Controls.Add(cancel);Controls.Add(save);CancelButton=cancel;AcceptButton=save;
            Controls.Add(new Label { Text="Window scale",Location=new Point(24,216),Size=new Size(392,22) });
            var windowChoices=new Panel { Location=new Point(24,242),Size=new Size(392,72),TabIndex=3,AccessibleName="Window size" };
            Controls.Add(windowChoices);zoomChoices=windowChoices;Chrome.DisplayChanged+=SyncZoom;
            for(int i=0;i<5;i++) {
                float zoom=.5f+i*.25f;
                var choice=new DisplayChoice {
                    Text=Math.Round(zoom*100)+"%",PreviewScale=zoom,WindowPreview=true,
                    Location=new Point(i*79,0),Size=new Size(76,72),TabIndex=i,
                    Checked=Math.Abs(windowScale-zoom)<.01,AccessibleName="Window size: "+Math.Round(zoom*100)+" percent"
                };
                choice.CheckedChanged+=(s,e)=> { if(choice.Checked)windowScale=zoom; };
                windowChoices.Controls.Add(choice);
            }
            foreach(Control control in Controls) { if(control.Top>=354)control.Top+=40;if(control.Top>=216)control.Top-=26; }
            cancel.TabIndex=10;save.TabIndex=11;
            save.Click+=(s,e)=> {
                float oldScale=Chrome.FontScale,oldWindowScale=Chrome.WindowScale;bool oldBadges=Chrome.ShowBadges,kept=false;
                if(Math.Abs(scale-oldScale)<0.001 && badges==oldBadges && Math.Abs(windowScale-oldWindowScale)<.001) { if(settingsHost==null) { DialogResult=DialogResult.OK;Close(); }return; }
                try {
                    Chrome.ApplyDisplay(scale,badges,windowScale);
                    var hidden=new System.Collections.Generic.List<Form>();
                    Form origin=settingsHost ?? this;
                    while(origin is DisplayDialog || origin is SettingsDialog) {
                        hidden.Add(origin);
                        if(origin.Owner==null)break;
                        origin=origin.Owner;
                    }
                    DialogResult decision;
                    try {
                        foreach(var window in hidden)window.Hide();
                        using(var confirmation=new DisplayConfirmationDialog()) {
                            if(!hidden.Contains(origin)) {
                                confirmation.StartPosition=FormStartPosition.Manual;
                                confirmation.Location=ConfirmationLocation(origin.Bounds,confirmation.Size,Screen.FromControl(origin).WorkingArea);
                            }
                            decision=confirmation.ShowDialog(hidden.Contains(origin)?null:origin);
                        }
                    } finally {
                        for(int i=hidden.Count-1;i>=0;i--)if(!hidden[i].IsDisposed)hidden[i].Show();
                    }
                    if(decision!=DialogResult.OK)return;
                    Chrome.SaveDisplay(scale,badges,windowScale);kept=true;
                    if(settingsHost==null) { DialogResult=DialogResult.OK;Close(); }
                    else { status.ForeColor=Chrome.AccentForeground;status.Text="Display settings saved."; }
                }
                catch(System.IO.IOException) { status.Text="Unable to save display preferences."; }
                catch(UnauthorizedAccessException) { status.Text="Unable to save display preferences."; }
                finally { if(!kept)Chrome.ApplyDisplay(oldScale,oldBadges,oldWindowScale); }
            };
            if(settingsHost!=null) {
                TopLevel=false;FormBorderStyle=FormBorderStyle.None;
                foreach(Control control in Controls)control.Top-=38;
                cancel.Visible=false;save.Text="Apply";CancelButton=null;AcceptButton=null;
                ClientSize=new Size(440,422);
            }
        }
        internal static Point ConfirmationLocation(Rectangle origin,Size dialog,Rectangle work) {
            int x=origin.Left+(origin.Width-dialog.Width)/2,y=origin.Bottom+12;
            if(y+dialog.Height>work.Bottom) {
                if(origin.Top-dialog.Height-12>=work.Top)y=origin.Top-dialog.Height-12;
                else if(origin.Right+12+dialog.Width<=work.Right) { x=origin.Right+12;y=origin.Top; }
                else if(origin.Left-dialog.Width-12>=work.Left) { x=origin.Left-dialog.Width-12;y=origin.Top; }
                else { x=origin.Right-dialog.Width;y=origin.Bottom-dialog.Height; }
            }
            return new Point(Math.Max(work.Left,Math.Min(x,work.Right-dialog.Width)),Math.Max(work.Top,Math.Min(y,work.Bottom-dialog.Height)));
        }
    }
    internal sealed class DisplayChoice : RadioButton {
        internal float PreviewScale=1;
        internal bool WindowPreview,BadgePreview,PreviewBadges;
        internal Func<int> PreviewType;
        internal Func<Keys[]> PreviewBindings;
        internal DisplayChoice() {
            Appearance=Appearance.Button;Cursor=Cursors.Hand;
            SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
        }
        protected override void OnCheckedChanged(EventArgs e) { base.OnCheckedChanged(e);Invalidate(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e);Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e);Invalidate(); }
        protected override void OnPaint(PaintEventArgs e) {
            var g=e.Graphics;g.Clear(Chrome.Background);g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Chrome.Box(g,new RectangleF(2,2,Width-5,Height-5),Checked?Chrome.AccentSurface:Chrome.Surface,Checked?Chrome.Accent:Chrome.Border,Chrome.ControlRadius);
            if(Focused && ShowFocusCues)using(var outline=Chrome.Round(new RectangleF(0,0,Width-1,Height-1),Chrome.ControlRadius+2))using(var pen=new Pen(Chrome.AccentForeground))g.DrawPath(pen,outline);
            using(var label=Chrome.UiFont("Segoe UI",7.5f))Chrome.Text(g,Text,label,Checked?Chrome.AccentForeground:Chrome.Muted,new RectangleF(8,5,Width-16,18),true);
            if(BadgePreview) {
                float left=(Width-42)/2f;
                Chrome.Box(g,new RectangleF(left,27,42,38),Chrome.Background,Chrome.Border,6);
                int type=PreviewType();
                VectorIcon.Draw(g,type,new RectangleF(left+8,32,20,20));
                var main=new RectangleF(left+15,53,25,10);
                Chrome.Box(g,main,Chrome.AccentSurface,Chrome.AccentSurface,3);
                using(var font=Chrome.UiFont("Segoe UI",6,FontStyle.Bold))Chrome.Text(g,"Main",font,Chrome.AccentForeground,main,true);
                string shortcut=KeyBindings.Label(PreviewBindings()[Array.IndexOf(KeyBindings.Actions,Model.Names[type])]);
                if(PreviewBadges && shortcut.Length>0) {
                    float width=Math.Max(12,shortcut.Length*6+4);
                    var key=new RectangleF(left+42-width,25,width,12);
                    Chrome.Box(g,key,Chrome.Border,Chrome.Border,3);
                    using(var font=Chrome.UiFont("Segoe UI",6.5f,FontStyle.Bold))Chrome.Text(g,shortcut,font,Chrome.TextColor,key,true);
                }
                return;
            }
            if(WindowPreview) {
                float width=36*PreviewScale,height=22*PreviewScale,left=(Width-width)/2f,top=62-height;
                var ink=Checked?Chrome.AccentForeground:Chrome.Muted;
                Chrome.Box(g,new RectangleF(left,top,width,height),Chrome.Background,ink,3);
                using(var pen=new Pen(ink))g.DrawLine(pen,left+1,top+height*.23f,left+width-1,top+height*.23f);
                using(var fill=new SolidBrush(Chrome.Surface)) {
                    g.FillRectangle(fill,left+width*.08f,top+height*.36f,width*.38f,height*.5f);
                    g.FillRectangle(fill,left+width*.54f,top+height*.36f,width*.38f,height*.5f);
                }
                var host=TopLevelControl as SettingsDialog;
                if(Chrome.ShowBadges && (host==null || host.KeyboardNavigation)) {
                    string hint=Math.Abs(PreviewScale-Chrome.WindowScale-.25f)<.01?"Ctrl+↑":Math.Abs(PreviewScale-Chrome.WindowScale+.25f)<.01?"Ctrl+↓":"";
                    if(hint.Length>0) {
                        var badge=new RectangleF(Width-49,22,44,13);
                        Chrome.Box(g,badge,Chrome.Border,Chrome.Border,3);
                        using(var font=Chrome.UiFont("Segoe UI",6.5f,FontStyle.Bold))Chrome.Text(g,hint,font,Chrome.TextColor,badge,true);
                    }
                }
                return;
            }
            using(var title=new Font("Segoe UI",13*PreviewScale,FontStyle.Bold))Chrome.Text(g,"Attack",title,Chrome.Attack,new RectangleF(12,25,104,28),false);
            using(var body=new Font("Segoe UI",9*PreviewScale))Chrome.Text(g,"Damage dealt",body,Chrome.TextColor,new RectangleF(12,53,104,22),false);
            using(var number=new Font("Segoe UI",8*PreviewScale,FontStyle.Bold))Chrome.Text(g,"×1.6  ·  ×0.625",number,Chrome.Muted,new RectangleF(12,75,104,20),false);
        }
    }
    internal sealed class DisplayConfirmationDialog : PortableWindow {
        protected override bool CompactCaption { get { return true; } }
        private readonly Timer countdown=new Timer { Interval=33 };
        private readonly System.Diagnostics.Stopwatch elapsed=new System.Diagnostics.Stopwatch();
        internal DisplayConfirmationDialog(double seconds=15) {
            Text="Keep display settings?";ClientSize=new Size(440,226);StartPosition=FormStartPosition.CenterParent;ShowInTaskbar=false;MaximizeBox=false;MinimizeBox=false;
            BackColor=Chrome.Background;ForeColor=Chrome.TextColor;Font=Chrome.UiFont("Segoe UI",9);
            Controls.Add(new Label { Text="Is everything readable?",Font=Chrome.UiFont("Segoe UI",13,FontStyle.Bold),Location=new Point(24,54),Size=new Size(392,32) });
            var remaining=new Label { Location=new Point(24,94),Size=new Size(392,44),ForeColor=Chrome.Muted };Controls.Add(remaining);
            var progress=new DrawingPanel { Location=new Point(24,140),Size=new Size(392,8),TabStop=false,AccessibleRole=AccessibleRole.ProgressBar,AccessibleName="Time remaining before restoring previous settings" };
            progress.Paint+=(s,e)=> {
                e.Graphics.Clear(Chrome.Background);e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                float fraction=(float)Math.Max(0,Math.Min(1,1-elapsed.Elapsed.TotalSeconds/seconds));
                using(var track=Chrome.Round(new RectangleF(0,0,progress.Width,progress.Height),4)) {
                    using(var fill=new SolidBrush(Chrome.Border))e.Graphics.FillPath(fill,track);
                    var state=e.Graphics.Save();e.Graphics.SetClip(track);
                    using(var fill=new SolidBrush(Chrome.Accent))e.Graphics.FillRectangle(fill,0,0,progress.Width*fraction,progress.Height);
                    e.Graphics.Restore(state);
                }
            };
            Controls.Add(progress);
            var revert=new SettingsButton { Text="Revert",DialogResult=DialogResult.Cancel,Location=new Point(152,164),Size=new Size(112,36),TabIndex=0 };
            var keep=new SettingsButton { Text="Keep changes",Primary=true,DialogResult=DialogResult.OK,Location=new Point(272,164),Size=new Size(144,36),TabIndex=1 };
            Controls.Add(revert);Controls.Add(keep);CancelButton=revert;AcceptButton=keep;
            Action refresh=()=> {
                double left=seconds-elapsed.Elapsed.TotalSeconds;
                remaining.Text="Previous settings will be restored in "+Math.Max(0,(int)Math.Ceiling(left))+" seconds.";
                progress.AccessibleDescription=Math.Max(0,(int)Math.Ceiling(left))+" seconds remaining";progress.Invalidate();
                if(left<=0) { DialogResult=DialogResult.Cancel;Close(); }
            };
            countdown.Tick+=(s,e)=>refresh();Shown+=(s,e)=>{elapsed.Start();refresh();countdown.Start();};FormClosed+=(s,e)=>countdown.Stop();refresh();
        }
        protected override void Dispose(bool disposing) { if(disposing)countdown.Dispose();base.Dispose(disposing); }
    }
}
