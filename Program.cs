using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AniimoAffinities {
    internal static class Program {
        [DllImport("user32.dll")] private static extern bool SetProcessDPIAware();
        [STAThread]
        private static int Main(string[] args) {
            try {
                SetProcessDPIAware(); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);Chrome.LoadAppearance();
                if(args.Length==2 && args[0].StartsWith("--preview-half-")) { Chrome.WindowScale=.5f;args[0]="--preview-"+args[0].Substring("--preview-half-".Length); }
                if(args.Length==2 && args[0].StartsWith("--preview-large-")) { Chrome.WindowScale=1.5f;args[0]="--preview-"+args[0].Substring("--preview-large-".Length); }
                if(args.Length==2 && args[0].StartsWith("--preview-light-")) { Chrome.Appearance=2;Chrome.ApplyAppearance();args[0]="--preview-"+args[0].Substring("--preview-light-".Length); }
                if(args.Length==2 && args[0].StartsWith("--preview-dark-")) { Chrome.Appearance=0;Chrome.ApplyAppearance();args[0]="--preview-"+args[0].Substring("--preview-dark-".Length); }
                if(args.Length==2 && args[0].StartsWith("--preview-tutorial")) {
                    using(var source=new MainForm())using(var tour=new TutorialOverlay(source)) {
                        source.Show();Application.DoEvents();tour.Show(source);Application.DoEvents();
                        int target=args[0]=="--preview-tutorial-final"?tour.StepCount-1:args[0]=="--preview-tutorial-display"?14:args[0]=="--preview-tutorial-selector"?1:0;
                        while(tour.StepIndex<target)tour.Move(1);
                        Application.DoEvents();using(var image=new Bitmap(tour.Width,tour.Height)) { tour.DrawToBitmap(image,new Rectangle(Point.Empty,tour.Size));image.Save(args[1]); }
                    }return 0;
                }
                if(args.Length==2 && args[0]=="--preview-lock-controls") {
                    using(var form=new UnlockWindow(()=>{},()=>{},false,()=>Point.Empty,point=>{})) { form.Show();Application.DoEvents();using(var image=new Bitmap(form.Width,form.Height)) { form.DrawToBitmap(image,new Rectangle(Point.Empty,form.Size));image.Save(args[1]); } }return 0;
                }
                if(args.Length==2 && args[0]=="--preview-tooltip") {
                    using(var owner=new Form())using(var bubble=new TipBubble()) {
                        var anchor=new Button { Text="Grass",Size=new Size(80,30) };owner.Controls.Add(anchor);owner.Show();Application.DoEvents();
                        bubble.Present(anchor,"Hold and drag to move overlay");Application.DoEvents();
                        using(var image=new Bitmap(bubble.Width,bubble.Height)) { bubble.DrawToBitmap(image,new Rectangle(Point.Empty,bubble.Size));image.Save(args[1]); }
                    }return 0;
                }
                if(args.Length==2 && args[0]=="--self-test") { Test(); File.WriteAllText(args[1],"PASS: 81 selections, 9 SVG icons, selection transitions, no capture modules."); return 0; }
                if(args.Length==2 && (args[0]=="--preview-radar" || args[0]=="--preview-radar-separate")) {
                    using(var form=new RadarDialog(Array.IndexOf(Model.Names,"Grass"),Array.IndexOf(Model.Names,"Water"),KeyBindings.Defaults())) {
                        form.Show();Application.DoEvents();
                        if(args[0]=="--preview-radar-separate")((IconButton)typeof(RadarDialog).GetField("separateButton",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(form)).PerformClick();
                        using(var image=new Bitmap(form.Width,form.Height)) { form.DrawToBitmap(image,new Rectangle(Point.Empty,form.Size));image.Save(args[1]); }
                    }return 0;
                }
                if(args.Length==2 && args[0]=="--preview-display") {
                    using(var form=new DisplayDialog()) { form.Show();Application.DoEvents();using(var image=new Bitmap(form.Width,form.Height)) { form.DrawToBitmap(image,new Rectangle(Point.Empty,form.Size));image.Save(args[1]); } }return 0;
                }
                if(args.Length==2 && args[0]=="--preview-matrix") {
                    using(var form=new MatrixDialog(-1)) { form.Show();Application.DoEvents();using(var image=new Bitmap(form.Width,form.Height)) { form.DrawToBitmap(image,new Rectangle(Point.Empty,form.Size));image.Save(args[1]); } }return 0;
                }
                if(args.Length==2 && (args[0]=="--preview-display-settings" || args[0]=="--preview-guide-settings")) {
                    using(var form=new SettingsDialog(KeyBindings.Defaults())) { form.Show();Application.DoEvents();if(args[0]=="--preview-guide-settings") { form.Controls.OfType<SettingsButton>().Single(button=>button.AccessibleName=="Tutorial").PerformClick();var settle=System.Diagnostics.Stopwatch.StartNew();while(settle.ElapsedMilliseconds<230) { Application.DoEvents();System.Threading.Thread.Sleep(5); } }else { form.TutorialSection("Display");var navigation=Message.Create(form.Handle,0x100,(IntPtr)Keys.Tab,IntPtr.Zero);form.PreFilterMessage(ref navigation); }using(var image=new Bitmap(form.Width,form.Height)) { form.DrawToBitmap(image,new Rectangle(Point.Empty,form.Size));image.Save(args[1]); } }return 0;
                }
                if(args.Length==2 && (args[0]=="--preview-settings" || args[0]=="--preview-window-settings")) {
                    using(var form=new SettingsDialog(KeyBindings.Defaults())) { form.Show();Application.DoEvents();if(args[0]=="--preview-window-settings")form.TutorialSection("Window & overlay");using(var image=new Bitmap(form.Width,form.Height)) { form.DrawToBitmap(image,new Rectangle(Point.Empty,form.Size));image.Save(args[1]); } }return 0;
                }
                if(args.Length==2 && (args[0]=="--preview" || args[0]=="--preview-inline-matrix" || args[0]=="--preview-inline-radar" || args[0]=="--preview-small" || args[0]=="--preview-empty" || args[0]=="--preview-overlay")) {
                    using(var form=new MainForm()) {
                        if(args[0]=="--preview-inline-radar")typeof(MainForm).GetMethod("ToggleInlineRadar",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(form,null);
                        if(args[0]=="--preview-inline-matrix")typeof(MainForm).GetMethod("ToggleInlineMatrix",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(form,null);
                        if(args[0]=="--preview-overlay")form.SetPinned(true);
                        if(args[0]=="--preview-small" || args[0]=="--preview-empty")form.Size=form.MinimumSize;
                        if(args[0]=="--preview-empty")((Selection)typeof(MainForm).GetField("selection",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(form)).Clear();
                        if(args[0]=="--preview-empty")typeof(MainForm).GetMethod("RefreshSelection",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(form,null);
                        form.Show(); Application.DoEvents(); using(var image=new Bitmap(form.Width,form.Height)) { form.DrawToBitmap(image,new Rectangle(Point.Empty,form.Size)); image.Save(args[1]); }
                    } return 0;
                }
                Application.Run(new MainForm(true)); return 0;
            } catch(Exception error) { if(args.Length>=2)File.WriteAllText(args[args.Length-1],error.ToString()); else MessageBox.Show(error.Message,"Aniimo Affinities"); return 1; }
        }
        // Inputs: embedded product. Output: regression assertions. Fallback: none. Raises: failed invariant. Effects: temporary in-memory images.
        private static void Test() {
            string tutorialTestPath=Path.Combine(Path.GetTempPath(),"aniimo-tutorial-"+Guid.NewGuid().ToString("N")+".txt");
            try {
                if(TutorialPreferences.Load(tutorialTestPath))throw new Exception("Tutorial must be offered before opt-out");
                TutorialPreferences.Save(true,tutorialTestPath);if(!TutorialPreferences.Load(tutorialTestPath))throw new Exception("Tutorial opt-out was not saved");
                TutorialPreferences.Save(false,tutorialTestPath);if(TutorialPreferences.Load(tutorialTestPath))throw new Exception("Tutorial opt-in was not saved");
            } finally { if(File.Exists(tutorialTestPath))File.Delete(tutorialTestPath); }
            var tourBubble=TutorialOverlay.PlaceBubble(new Size(760,570),new Rectangle(10,10,500,80),new Size(400,260));
            if(!new Rectangle(0,0,760,570).Contains(tourBubble) || tourBubble.IntersectsWith(new Rectangle(10,10,500,80)))throw new Exception("Tutorial bubble must fit and avoid target when space is available");
            using(var tourSource=new MainForm())using(var tutorial=new TutorialOverlay(tourSource)) {
                tourSource.Show();Application.DoEvents();tutorial.Show(tourSource);Application.DoEvents();
                var before=tourSource.TutorialSelection;
                for(int step=1;step<tutorial.StepCount;step++)tutorial.Move(1);
                if(tutorial.StepIndex!=tutorial.StepCount-1 || !tutorial.Controls.OfType<DrawingPanel>().SelectMany(panel=>panel.Controls.OfType<CheckBox>()).Single().Visible)throw new Exception("Final tutorial step must expose opt-out");
                tutorial.Move(-1);if(tutorial.StepIndex!=tutorial.StepCount-2)throw new Exception("Tutorial previous action failed");
                if(tourSource.TutorialSelection.Main!=before.Main || tourSource.TutorialSelection.Sub!=before.Sub || tourSource.TopMost)throw new Exception("Tutorial must preserve source state");
            }

            using(var initialMain=new MainForm()) {
                initialMain.Show();Application.DoEvents();
                var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
                var initialSelection=(Selection)typeof(MainForm).GetField("selection",flags).GetValue(initialMain);
                initialSelection.Main=Array.IndexOf(Model.Names,"Grass");initialSelection.Sub=Array.IndexOf(Model.Names,"Light");
                typeof(MainForm).GetMethod("RefreshSelection",flags).Invoke(initialMain,null);
                typeof(MainForm).GetField("hovered",flags).SetValue(initialMain,-1);
                var buttons=(IconButton[])typeof(MainForm).GetField("types",flags).GetValue(initialMain);
                buttons[Array.IndexOf(Model.Names,"Water")].Focus();buttons[Array.IndexOf(Model.Names,"Dark")].Focus();
                var preview=(Selection)typeof(MainForm).GetProperty("preview",flags).GetValue(initialMain,null);
                if(preview.Main!=initialSelection.Main || preview.Sub!=initialSelection.Sub)throw new Exception("Initial keyboard focus must not override selected types");
            }

            string sessionTestPath=Path.Combine(Path.GetTempPath(),"aniimo-session-"+Guid.NewGuid().ToString("N")+".txt");
            try {
                var savedSession=new SessionState { Main=Array.IndexOf(Model.Names,"Fire"),Sub=Array.IndexOf(Model.Names,"Dark"),Current="matrix",Radar="affinities",Matrix="radar" };
                savedSession.Save(sessionTestPath);var loadedSession=SessionState.Load(sessionTestPath);
                if(loadedSession.Main!=savedSession.Main || loadedSession.Sub!=savedSession.Sub || loadedSession.Current!="matrix" || loadedSession.Radar!="affinities" || loadedSession.Matrix!="radar")throw new Exception("Session did not round-trip");
                savedSession.Main=-1;savedSession.Sub=-1;savedSession.Save(sessionTestPath);loadedSession=SessionState.Load(sessionTestPath);
                if(loadedSession.Main!=-1 || loadedSession.Sub!=-1)throw new Exception("Empty selection must persist");
                File.WriteAllLines(sessionTestPath,new[]{"1","Unknown","","radar","radar","matrix"});loadedSession=SessionState.Load(sessionTestPath);
                if(loadedSession.Current!="affinities" || loadedSession.Main<0)throw new Exception("Invalid session must fall back safely");
            } finally { if(File.Exists(sessionTestPath))File.Delete(sessionTestPath); }

            int previewMain=Array.IndexOf(Model.Names,"Fire");var previewKeys=KeyBindings.Defaults();
            using(var displayPreview=new DisplayDialog(null,()=>previewMain,()=>previewKeys)) {
                var choices=displayPreview.Controls.OfType<Panel>().SelectMany(panel=>panel.Controls.OfType<DisplayChoice>()).Where(choice=>choice.BadgePreview).ToArray();
                if(choices.Length!=2 || choices.Any(choice=>choice.PreviewType()!=previewMain))throw new Exception("Badge previews must use selected Main");
                previewMain=Array.IndexOf(Model.Names,"Water");
                if(choices.Any(choice=>choice.PreviewType()!=previewMain))throw new Exception("Badge previews did not follow Main change");
                previewKeys[Array.IndexOf(KeyBindings.Actions,"Water")]=Keys.Q;
                if(choices.Any(choice=>choice.PreviewBindings()[Array.IndexOf(KeyBindings.Actions,"Water")]!=Keys.Q))throw new Exception("Badge previews must use draft rebinds");
                previewMain=-1;int fallback=choices[0].PreviewType();
                if(fallback<0 || fallback>=Model.Names.Length || choices.Any(choice=>choice.PreviewType()!=fallback))throw new Exception("Empty Main requires one stable shared fallback");
            }

            using(var settings=new SettingsDialog(KeyBindings.Defaults())) {
                settings.Show();Application.DoEvents();int initial=settings.ClientSize.Height;
                settings.Controls.OfType<SettingsButton>().Single(button=>button.Text=="Window & overlay").PerformClick();
                if(settings.ClientSize.Height!=initial)throw new Exception("Tab height should animate instead of jumping");
                settings.Controls.OfType<SettingsButton>().Single(button=>button.Text=="Display").PerformClick();
                var transition=System.Diagnostics.Stopwatch.StartNew();
                while(transition.ElapsedMilliseconds<260) { Application.DoEvents();System.Threading.Thread.Sleep(5); }
                if(settings.ClientSize.Height!=594)throw new Exception("Rapid tab changes must settle at latest height");
                settings.Controls.OfType<SettingsButton>().Single(button=>button.Text=="Types").PerformClick();
                transition.Restart();while(transition.ElapsedMilliseconds<260) { Application.DoEvents();System.Threading.Thread.Sleep(5); }
                if(settings.ClientSize.Height!=initial)throw new Exception("Types height was not restored after animation");
            }

            float[][] expectedZoomTargets={new[]{.75f,1f},new[]{.5f,1f},new[]{.5f,1.5f},new[]{1f,1.5f},new[]{1f,1.25f}};
            for(int i=0;i<5;i++)if(!WindowZoom.Neighbors(.5f+i*.25f).SequenceEqual(expectedZoomTargets[i]))throw new Exception("Locked scale targets are incorrect");
            using(var holder=new Form())using(var button=new HoldSizeButton { Target=.5f }) {
                holder.Controls.Add(button);holder.Show();Application.DoEvents();int commits=0;button.Commit+=value=>commits++;
                button.PerformClick();if(commits!=0)throw new Exception("Short click must not resize overlay");
                var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
                var down=typeof(HoldSizeButton).GetMethod("OnMouseDown",flags);
                down.Invoke(button,new object[]{new MouseEventArgs(MouseButtons.Left,1,10,10,0)});button.CancelHold();
                if(commits!=0)throw new Exception("Cancelled hold must not resize");
                down.Invoke(button,new object[]{new MouseEventArgs(MouseButtons.Left,1,10,10,0)});
                var watch=System.Diagnostics.Stopwatch.StartNew();while(watch.ElapsedMilliseconds<1100) { Application.DoEvents();System.Threading.Thread.Sleep(5); }
                if(commits!=1)throw new Exception("Completed hold must resize exactly once");
            }

            float savedZoom=Chrome.WindowScale;Chrome.WindowScale=1;
            try {
                using(var main=new MainForm())using(var matrix=new MatrixDialog(-1)) {
                    main.Show();matrix.Show();Application.DoEvents();
                    var mainSize=main.ClientSize;var matrixSize=matrix.ClientSize;
                    foreach(float zoom in new[]{.5f,1.5f,1f,.5f,1f}) {
                        Chrome.ApplyDisplay(Chrome.FontScale,Chrome.ShowBadges,zoom);Application.DoEvents();
                        if(Math.Abs(main.ClientSize.Width-mainSize.Width*zoom)>2 || Math.Abs(matrix.ClientSize.Height-matrixSize.Height*zoom)>2)throw new Exception("Window scale geometry or rollback failed");
                        using(var bitmap=new Bitmap(main.Width,main.Height))main.DrawToBitmap(bitmap,new Rectangle(Point.Empty,main.Size));
                    }
                }
            } finally { Chrome.WindowScale=savedZoom; }

            using(var main=new MainForm()) {
                var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
                var toggle=typeof(MainForm).GetMethod("ToggleInlineRadar",flags);var state=typeof(MainForm).GetField("inlineRadar",flags);
                toggle.Invoke(main,null);if(!(bool)state.GetValue(main))throw new Exception("Inline radar did not activate");
                var matrixToggle=typeof(MainForm).GetMethod("ToggleInlineMatrix",flags);
                matrixToggle.Invoke(main,null);
                if((string)typeof(MainForm).GetField("currentView",flags).GetValue(main)!="matrix" || (string)typeof(MainForm).GetField("matrixView",flags).GetValue(main)!="radar")throw new Exception("Matrix must swap with active radar");
                matrixToggle.Invoke(main,null);
                toggle.Invoke(main,null);if((bool)state.GetValue(main))throw new Exception("Affinity diagrams were not restored");
            }
            using(var origin=new MainForm())using(var settings=new SettingsDialog(KeyBindings.Defaults()))using(var display=new DisplayDialog())using(var probe=new Timer { Interval=40 }) {
                origin.Show();settings.Show(origin);
                typeof(DisplayDialog).GetField("scale",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(display,Chrome.FontScale==1.1f?0.9f:1.1f);
                int phase=0;Exception failed=null;
                probe.Tick+=(s,e)=> {
                    try {
                        if(phase==0) { phase=1;display.Controls.OfType<SettingsButton>().Single(button=>button.Text=="Save").PerformClick(); }
                        else if(phase==1) {
                            var confirmation=Application.OpenForms.OfType<DisplayConfirmationDialog>().FirstOrDefault();if(confirmation==null)return;
                            if(display.Visible || settings.Visible || !origin.Visible)throw new Exception("Display preview did not reveal origin");
                            phase=2;confirmation.Controls.OfType<SettingsButton>().Single(button=>button.Text=="Revert").PerformClick();
                        } else {
                            if(!display.Visible || !settings.Visible)throw new Exception("Settings were not restored after display preview");
                            probe.Stop();display.Close();
                        }
                    } catch(Exception error) { failed=error;probe.Stop();foreach(var confirmation in Application.OpenForms.OfType<DisplayConfirmationDialog>().ToArray())confirmation.Close();display.Close(); }
                };
                probe.Start();display.ShowDialog(settings);if(failed!=null)throw failed;
            }
            float savedScale=Chrome.FontScale;bool savedBadges=Chrome.ShowBadges;
            try {
                Chrome.ApplyDisplay(1.1f,false);
                using(var confirmation=new DisplayConfirmationDialog(0.1))
                    if(confirmation.ShowDialog()!=DialogResult.Cancel)throw new Exception("Display timeout must revert");
            } finally { Chrome.ApplyDisplay(savedScale,savedBadges); }
            if(Chrome.FontScale!=savedScale || Chrome.ShowBadges!=savedBadges)throw new Exception("Display rollback failed");
            if(PortableWindow.AttentionOffset(0)!=Point.Empty || PortableWindow.AttentionOffset(1)!=Point.Empty || PortableWindow.AttentionOffset(0.05).Y==0 || PortableWindow.AttentionOffset(0.05).X==0)throw new Exception("Attention motion must use both axes and settle exactly");
            using(var owner=new PortableWindow())using(var dialog=new PortableWindow())using(var timer=new Timer { Interval=30 }) {
                owner.Show();var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
                int phase=0;Point origin=Point.Empty;Exception failure=null;
                timer.Tick+=(s,e)=> {
                    try {
                        if(phase++==0) {
                            origin=dialog.Location;
                            var message=Message.Create(owner.Handle,0x20,owner.Handle,new IntPtr((0x201<<16)|0xfffe));
                            object[] parameters={message};typeof(PortableWindow).GetMethod("WndProc",flags).Invoke(owner,parameters);
                            var animation=(Timer)typeof(PortableWindow).GetField("attentionTimer",flags).GetValue(dialog);
                            if(!animation.Enabled)throw new Exception("Blocked dialog did not animate");
                            for(int click=0;click<10;click++) {
                                object[] repeat={Message.Create(owner.Handle,0x20,owner.Handle,new IntPtr((0x201<<16)|0xfffe))};
                                typeof(PortableWindow).GetMethod("WndProc",flags).Invoke(owner,repeat);
                                if(((Message)repeat[0]).Result!=new IntPtr(1))throw new Exception("Repeated blocked click was not silenced");
                            }
                            timer.Interval=450;
                        } else {
                            if(dialog.Location!=origin)throw new Exception("Dialog shake did not restore position");
                            timer.Stop();dialog.Close();
                        }
                    } catch(Exception error) { failure=error;timer.Stop();dialog.Close(); }
                };
                timer.Start();dialog.ShowDialog(owner);if(failure!=null)throw failure;
            }
            if(Math.Abs(RadarDialog.RadiusFraction(1)-1/2.56)>0.000001 || RadarDialog.RadiusFraction(2.56)!=1 || RadarDialog.RadiusFraction(0)!=0)throw new Exception("Radar scale mismatch");
            using(var radar=new RadarDialog(0,1,KeyBindings.Defaults())) {
                radar.Show();Application.DoEvents();
                var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
                var selected=(Selection)typeof(RadarDialog).GetField("selection",flags).GetValue(radar);
                if(selected.Main!=0 || selected.Sub!=1)throw new Exception("Radar lost dual selection");
                ((IconButton)typeof(RadarDialog).GetField("separateButton",flags).GetValue(radar)).PerformClick();
                if((bool)typeof(RadarDialog).GetField("combined",flags).GetValue(radar))throw new Exception("Radar separate view failed");
                ((IconButton)typeof(RadarDialog).GetField("combinedButton",flags).GetValue(radar)).PerformClick();
                if(!(bool)typeof(RadarDialog).GetField("combined",flags).GetValue(radar))throw new Exception("Radar combined view failed");
            }
            var matrixKeys=KeyBindings.Defaults();matrixKeys[1]=Keys.Q;matrixKeys[2]=Keys.H;matrixKeys[9]=Keys.None;
            using(var matrix=new MatrixDialog(-1,matrixKeys)) {
                var handler=typeof(MatrixDialog).GetMethod("ProcessCmdKey",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                var current=typeof(MatrixDialog).GetField("fixedType",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                var selectors=(IconButton[])typeof(MatrixDialog).GetField("selectors",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(matrix);
                var msg=Message.Create(matrix.Handle,0x100,IntPtr.Zero,IntPtr.Zero);
                handler.Invoke(matrix,new object[]{msg,Keys.D1});if((int)current.GetValue(matrix)!=-1)throw new Exception("Matrix retained old binding");
                handler.Invoke(matrix,new object[]{msg,Keys.Q});if((int)current.GetValue(matrix)!=Array.IndexOf(Model.Names,"Dark") || selectors[0].Shortcut!="Q")throw new Exception("Matrix rebind/badge mismatch");
                handler.Invoke(matrix,new object[]{msg,Keys.H});if((int)current.GetValue(matrix)!=Array.IndexOf(Model.Names,"Earth"))throw new Exception("Matrix H collision");
                if(selectors[8].Shortcut!="")throw new Exception("Matrix unbound badge visible");
            }
            for(int fixedType=-1;fixedType<9;fixedType++)for(int attacker=0;attacker<9;attacker++)for(int defender=0;defender<9;defender++) {
                Model.MatrixBest(fixedType,attacker,defender);
                double expected=Model.Factors(defender,-1)[9+attacker]*(fixedType<0 || fixedType==defender?1:Model.Factors(fixedType,-1)[9+attacker]);
                if(Math.Abs(Model.Factors(defender,fixedType)[9+attacker]-expected)>0.000001)throw new Exception("Matrix projection mismatch");
            }
            int originalAppearance=Chrome.Appearance;
            foreach(int mode in new[]{0,2}) {
                Chrome.Appearance=mode;Chrome.ApplyAppearance();
                foreach(double multiplier in new[]{.390625,.625,1,1.6,2.56})
                    if(Contrast(Chrome.MultiplierInk(multiplier),Chrome.MultiplierFill(multiplier))<4.5)throw new Exception("Matrix multiplier contrast below 4.5:1");
                foreach(var role in new[]{Chrome.Attack,Chrome.Defense})
                    if(Contrast(role,Chrome.Surface)<4.5 || Contrast(role,Chrome.Background)<4.5)throw new Exception("Attack/defense contrast below 4.5:1");
                foreach(var pair in new[]{new[]{Chrome.TextColor,Chrome.Surface},new[]{Chrome.TextColor,Chrome.Border},new[]{Chrome.Muted,Chrome.Background},new[]{Chrome.AccentForeground,Chrome.AccentSurface},new[]{Chrome.AccentInk,Chrome.Accent},new[]{Chrome.AccentInk,Chrome.AccentPressed}})
                    if(Contrast(pair[0],pair[1])<4.5)throw new Exception("Theme text contrast below 4.5:1");
            }
            Chrome.Appearance=originalAppearance;Chrome.ApplyAppearance();
            using(var settings=new SettingsDialog(KeyBindings.Defaults()))using(var unrelated=new Form()) {
                if(settings.KeyboardNavigation)throw new Exception("Navigation badges must start hidden");
                var input=Message.Create(settings.Handle,0x100,new IntPtr((int)Keys.Tab),IntPtr.Zero);
                if(settings.PreFilterMessage(ref input) || !settings.KeyboardNavigation)throw new Exception("Keyboard navigation not detected");
                input=Message.Create(unrelated.Handle,0x201,IntPtr.Zero,IntPtr.Zero);
                settings.PreFilterMessage(ref input);
                if(!settings.KeyboardNavigation)throw new Exception("Unrelated window changed navigation mode");
                input=Message.Create(settings.Handle,0x201,IntPtr.Zero,IntPtr.Zero);
                if(settings.PreFilterMessage(ref input) || settings.KeyboardNavigation)throw new Exception("Mouse input did not hide navigation badges");
            }
            var monitor=new Rectangle(0,0,1920,1040);
            if(PortableWindow.KeepCaptionVisible(new Rectangle(100,-200,860,500),monitor,54).Top!=0)throw new Exception("Caption escaped above monitor");
            if(PortableWindow.KeepCaptionVisible(new Rectangle(100,1100,860,500),monitor,54).Top!=986)throw new Exception("Caption escaped below monitor");
            var second=new Rectangle(1920,-300,1920,1400);
            var crossing=new Rectangle(1800,-100,860,500);
            if(PortableWindow.KeepCaptionVisible(crossing,second,54)!=crossing)throw new Exception("Cross-monitor movement blocked");
            var leftMonitor=new Rectangle(-1920,200,1920,1040);
            if(PortableWindow.KeepCaptionVisible(new Rectangle(-1500,0,860,500),leftMonitor,54).Top!=200)throw new Exception("Offset monitor caption escaped");
            var custom=KeyBindings.Defaults();custom[1]=Keys.Q;
            var unbound=KeyBindings.Defaults();unbound[1]=Keys.None;unbound[2]=Keys.None;
            if(!KeyBindings.Valid(unbound) || KeyBindings.Resolve(unbound,Keys.D1)!=-1 || KeyBindings.Resolve(unbound,Keys.None)!=-1 || KeyBindings.Label(Keys.None)!="")throw new Exception("Unbind failed");
            using(var capture=new KeyCapture()) {
                var handler=typeof(KeyCapture).GetMethod("ProcessCmdKey",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                var keyMessage=Message.Create(IntPtr.Zero,0x100,IntPtr.Zero,IntPtr.Zero);
                capture.Binding=Keys.D1;
                handler.Invoke(capture,new object[]{keyMessage,Keys.Space});
                handler.Invoke(capture,new object[]{keyMessage,Keys.Escape});
                handler.Invoke(capture,new object[]{keyMessage,Keys.R});
                if(capture.Binding!=Keys.D1)throw new Exception("Escape did not cancel capture");
                handler.Invoke(capture,new object[]{keyMessage,Keys.R});if(capture.Binding!=Keys.D1)throw new Exception("Inactive field captured a key");
                handler.Invoke(capture,new object[]{keyMessage,Keys.Space});
                handler.Invoke(capture,new object[]{keyMessage,Keys.R});capture.ReleaseCapturedKey(Keys.R);if(capture.Binding!=Keys.R)throw new Exception("Key capture failed");
                capture.ValidateBinding=key=>key!=Keys.D2;
                handler.Invoke(capture,new object[]{keyMessage,Keys.Space});
                handler.Invoke(capture,new object[]{keyMessage,Keys.D2});
                if(capture.Binding!=Keys.R || !capture.Invalid)throw new Exception("Duplicate binding was not rejected visually");
                handler.Invoke(capture,new object[]{keyMessage,Keys.R});
                capture.ReleaseCapturedKey(Keys.R);
                if(!capture.Validated || capture.Invalid)throw new Exception("Valid binding did not clear rejection");
                handler.Invoke(capture,new object[]{keyMessage,Keys.T});if(capture.Binding!=Keys.R)throw new Exception("Capture remained active after assignment");
                handler.Invoke(capture,new object[]{keyMessage,Keys.Space});
                handler.Invoke(capture,new object[]{keyMessage,Keys.Control|Keys.T});if(capture.Binding!=Keys.R)throw new Exception("Unsupported chord changed binding");
                handler.Invoke(capture,new object[]{keyMessage,Keys.Delete});if(capture.Binding!=Keys.None)throw new Exception("Delete did not unbind");
            }
            using(var capture=new KeyCapture { AllowModifiers=true,Binding=Keys.D1 }) {
                var handler=typeof(KeyCapture).GetMethod("ProcessCmdKey",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                var message=Message.Create(IntPtr.Zero,0x100,IntPtr.Zero,IntPtr.Zero);
                handler.Invoke(capture,new object[]{message,Keys.Space});
                handler.Invoke(capture,new object[]{message,Keys.Control|Keys.F});
                capture.ReleaseCapturedKey(Keys.F);
                if(capture.Binding!=Keys.D1)throw new Exception("Chord committed before Ctrl release");
                capture.ReleaseCapturedKey(Keys.ControlKey);
                if(capture.Binding!=(Keys.Control|Keys.F))throw new Exception("Released chord not captured");
                handler.Invoke(capture,new object[]{message,Keys.Space});
                handler.Invoke(capture,new object[]{message,Keys.Control|Keys.G});
                capture.ReleaseCapturedKey(Keys.ControlKey);
                if(capture.Binding!=(Keys.Control|Keys.F))throw new Exception("Chord committed before primary release");
                capture.ReleaseCapturedKey(Keys.G);
                if(capture.Binding!=(Keys.Control|Keys.G))throw new Exception("Reverse release order failed");
                handler.Invoke(capture,new object[]{message,Keys.Space});
                handler.Invoke(capture,new object[]{message,Keys.Control|Keys.H});
                handler.Invoke(capture,new object[]{message,Keys.Escape});
                capture.ReleaseCapturedKey(Keys.H);capture.ReleaseCapturedKey(Keys.ControlKey);
                if(capture.Binding!=(Keys.Control|Keys.G))throw new Exception("Pending chord not cancelled");
                handler.Invoke(capture,new object[]{message,Keys.Space});
                handler.Invoke(capture,new object[]{message,Keys.NumPad3});capture.ReleaseCapturedKey(Keys.NumPad3);
                if(capture.Binding!=Keys.D3)throw new Exception("Numpad release failed");
            }
            if(WindowZoom.Step(1,1)!=1.25f || WindowZoom.Step(1,-1)!=.75f || WindowZoom.Step(.5f,-1)!=.5f || WindowZoom.Step(1.5f,1)!=1.5f)throw new Exception("Window scale stepping failed");
            if(KeyBindings.Resolve(custom,Keys.Q)!=1 || KeyBindings.Resolve(custom,Keys.D1)!=-1 || KeyBindings.Resolve(custom,Keys.Control|Keys.Q)!=-1)throw new Exception("Rebinding failed");
            var duplicate=(Keys[])custom.Clone();duplicate[2]=Keys.Q;if(KeyBindings.Valid(duplicate))throw new Exception("Duplicate binding accepted");
            var config=Path.Combine(Path.GetTempPath(),"aniimo-keys-"+Guid.NewGuid().ToString("N"),"keys.txt");
            try {
                KeyBindings.Save(config,custom);if(!KeyBindings.Load(config).SequenceEqual(custom))throw new Exception("Bindings did not persist");
                KeyBindings.Save(config,unbound);if(!KeyBindings.Load(config).SequenceEqual(unbound))throw new Exception("Unbound bindings did not persist");
                KeyBindings.Save(config,KeyBindings.Defaults());if(!KeyBindings.Load(config).SequenceEqual(KeyBindings.Defaults()))throw new Exception("Defaults did not persist");
                var legacy=KeyBindings.Defaults().Take(15).ToArray();legacy[1]=Keys.Q;
                File.WriteAllLines(config,legacy.Select(key=>((int)key).ToString()).ToArray());
                var upgraded=KeyBindings.Load(config);
                if(!upgraded.Take(15).SequenceEqual(legacy) || upgraded[15]!=Keys.F10)throw new Exception("Radar shortcut migration lost bindings");
                legacy[1]=Keys.F10;File.WriteAllLines(config,legacy.Select(key=>((int)key).ToString()).ToArray());
                if(KeyBindings.Load(config)[15]!=Keys.None)throw new Exception("Radar migration duplicated a binding");
                File.WriteAllText(config,"invalid");if(!KeyBindings.Load(config).SequenceEqual(KeyBindings.Defaults()))throw new Exception("Invalid config fallback");
            } finally { if(File.Exists(config))File.Delete(config);Directory.Delete(Path.GetDirectoryName(config)); }
            using(var keyboard=new MainForm()) {
                typeof(MainForm).GetField("bindings",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(keyboard,KeyBindings.Defaults());
                keyboard.Show();Application.DoEvents();
                var method=typeof(MainForm).GetMethod("ProcessCmdKey",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                var field=typeof(MainForm).GetField("selection",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                var state=(Selection)field.GetValue(keyboard);
                Action<Keys,bool> press=(key,repeat)=> {
                    var message=Message.Create(keyboard.Handle,0x100,new IntPtr((int)(key&Keys.KeyCode)),new IntPtr(repeat?1L<<30:0));
                    method.Invoke(keyboard,new object[]{message,key});
                };
                string[] order={"Dark","Earth","Fire","Grass","Ice","Light","Lightning","Water","Wind"};
                for(int i=1;i<=9;i++) {
                    press(Keys.D0,false);press((Keys)((int)Keys.D0+i),false);
                    int expected=Array.IndexOf(Model.Names,order[i-1]);
                    if(state.Main!=expected || state.Sub!=-1)throw new Exception("Digit shortcut mismatch");
                    press((Keys)((int)Keys.D0+i),true);
                    if(state.Main!=expected)throw new Exception("Repeated shortcut toggled");
                    press(Keys.Control|Keys.D0,false);
                    if(state.Main!=expected)throw new Exception("Ctrl shortcut intercepted");
                    press(Keys.D0,false);press((Keys)((int)Keys.NumPad0+i),false);
                    if(state.Main!=expected)throw new Exception("Numpad shortcut mismatch");
                    press(Keys.D0,false);press((Keys)((int)Keys.D0+i)|Keys.Shift,false);
                    if(state.Main!=expected)throw new Exception("Shift number row mismatch");
                }
                var row=(Panel)typeof(MainForm).GetField("row",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(keyboard);
                var controls=row.Controls.OfType<IconButton>().OrderBy(button=>button.TabIndex).ToArray();
                controls[0].Focus();press(Keys.Left,false);
                if(!controls[9].Focused)throw new Exception("Left wrap failed");
                press(Keys.Right,false);if(!controls[0].Focused)throw new Exception("Right wrap failed");
                press(Keys.End,false);if(!controls[9].Focused)throw new Exception("End failed");
                press(Keys.Enter,false);if(state.Main!=-1)throw new Exception("Enter reset failed");
                if(controls[9].Visible)throw new Exception("Clear must hide for an empty selection");
                controls[0].Focus();press(Keys.Left,false);if(!controls[8].Focused)throw new Exception("Empty navigation included hidden clear");
                keyboard.Close();
            }
            using(var early=new MainForm()) { early.Show();early.Close();Application.DoEvents();if(!early.IsDisposed)throw new Exception("Early close failed"); }
            using(var ready=new MainForm()) {
                ready.Show();Application.DoEvents();
                ready.SetPinned(true);
                if(!ready.TopMost || ready.TransparencyKey!=Chrome.Background || ready.TransparencyKey==Chrome.Surface)throw new Exception("Pinned overlay must clear the background and retain cards");
                ready.SetPinned(false);
                if(ready.TopMost || ready.TransparencyKey!=Color.Empty)throw new Exception("Unpin must restore the opaque window");
                var close=ready.Controls.OfType<Panel>().SelectMany(panel=>panel.Controls.OfType<Button>()).SingleOrDefault(button=>button.AccessibleName=="Close (Alt+F4)");
                if(close==null || ready.FormBorderStyle!=FormBorderStyle.None)throw new Exception("Caption not ready");
                close.PerformClick();if(!ready.IsDisposed)throw new Exception("Custom close failed");
            }
            if(Model.Names.Length!=9)throw new Exception("Expected nine types");
            for(int a=0;a<9;a++)for(int b=-1;b<9;b++) {
                var values=Model.Factors(a,b);
                if(values.Length!=18 || values.Any(v=>double.IsNaN(v)||v<=0))throw new Exception("Invalid matchup");
            }
            int grass=Array.IndexOf(Model.Names,"Grass"),water=Array.IndexOf(Model.Names,"Water");
            if(Math.Abs(Model.Factors(grass,water)[9+water]-.390625)>1e-8)throw new Exception("Grass Water regression");
            using(var image=new Bitmap(64,64))using(var g=Graphics.FromImage(image))
                for(int type=0;type<9;type++)VectorIcon.Draw(g,type,new RectangleF(8,8,48,48));
            int ice=Array.IndexOf(Model.Names,"Ice"),fire=Array.IndexOf(Model.Names,"Fire"),wind=Array.IndexOf(Model.Names,"Wind"),dark=Array.IndexOf(Model.Names,"Dark");
            var origins=OriginGroups.Build(grass,ice,1,new[]{dark,fire,wind});
            if(origins.Single(v=>v.Type==fire).Mask!=3 || origins.Single(v=>v.Type==wind).Mask!=1 || origins.Single(v=>v.Type==dark).Mask!=1)throw new Exception("Grass Ice source contours");
            for(int a=0;a<9;a++)for(int b=-1;b<9;b++)for(int column=0;column<2;column++) {
                var factors=Model.Factors(a,b);
                var targets=Enumerable.Range(0,9).Where(t=>factors[column*9+t]!=1).ToArray();
                var groups=OriginGroups.Build(a,b,column,targets);
                if(groups.Length!=targets.Length || groups.Select(v=>v.Type).Distinct().Count()!=targets.Length || groups.Any(v=>v.Mask==0))throw new Exception("Origin coverage");
            }
            if(Math.Abs(Model.Factors(grass,ice)[water]-1.6)>1e-8)throw new Exception("Best available attack");
            if(Math.Abs(Model.Factors(grass,ice)[9+fire]-2.56)>1e-8)throw new Exception("Defense double weakness");
            if(Math.Abs(Model.Factors(grass,ice)[9+water]-.390625)>1e-8)throw new Exception("Defense double resistance");
            for(int a=0;a<9;a++)for(int b=0;b<9;b++) {
                if(a==b)continue;
                for(int i=0;i<9;i++) {
                    if(Math.Abs(Model.Factors(a,b)[i]-Math.Max(Model.Factors(a,-1)[i],Model.Factors(b,-1)[i]))>1e-8)throw new Exception("Attack parity");
                    if(Math.Abs(Model.Factors(a,b)[9+i]-Model.Factors(a,-1)[9+i]*Model.Factors(b,-1)[9+i])>1e-8)throw new Exception("Defense parity");
                }
            }
            var selection=new Selection();
            selection.Toggle(grass); selection.Toggle(water);
            var temporary=selection.Preview(ice);
            if(temporary.Main!=grass || temporary.Sub!=ice || selection.Sub!=water)throw new Exception("Hover must not commit");
            if(selection.Preview(-1).Sub!=water || selection.Preview(grass).Sub!=water)throw new Exception("Hover restore");
            if(new Selection().Preview(ice).Main!=ice)throw new Exception("Empty hover");
            if(selection.Main!=grass || selection.Sub!=water)throw new Exception("Main/Sub selection");
            selection.Toggle(grass);
            if(selection.Main!=water || selection.Sub!=-1)throw new Exception("Promotion");
            selection.Toggle(water);
            if(selection.Main!=-1)throw new Exception("Deselection");
            for(int a=0;a<9;a++)for(int b=0;b<9;b++)for(int c=0;c<9;c++) {
                selection.Clear();selection.Toggle(a);selection.Toggle(b);selection.Toggle(c);
                if(selection.Main<0 && selection.Sub>=0 || selection.Main>=0 && selection.Main==selection.Sub)throw new Exception("Selection invariant");
            }
            if(typeof(Program).Assembly.GetType("AniimoAffinities.CaptureSelection")!=null || typeof(Program).Assembly.GetType("AniimoAffinities.Recognition")!=null)throw new Exception("Capture must not ship");
            using(var form=new MainForm()) { form.Show();Application.DoEvents();using(var image=new Bitmap(form.Width,form.Height))form.DrawToBitmap(image,new Rectangle(Point.Empty,form.Size)); }
        }
        private static double Contrast(Color first,Color second) {
            Func<Color,double> luminance=color=> {
                Func<byte,double> channel=value=>value/255.0<=0.04045?value/255.0/12.92:Math.Pow((value/255.0+0.055)/1.055,2.4);
                return 0.2126*channel(color.R)+0.7152*channel(color.G)+0.0722*channel(color.B);
            };
            double a=luminance(first),b=luminance(second);return (Math.Max(a,b)+0.05)/(Math.Min(a,b)+0.05);
        }
    }
}
