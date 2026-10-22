using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities {
    internal sealed class MainForm : PortableWindow {
        private readonly Selection selection=new Selection();
        private Keys[] bindings=KeyBindings.Load(KeyBindings.FilePath);
        private int hovered=-1;
        private readonly System.Collections.Generic.List<Tuple<RectangleF,int,int>> affinityHits=new System.Collections.Generic.List<Tuple<RectangleF,int,int>>();
        private readonly System.Collections.Generic.List<Tuple<RectangleF,int,int>> originHits=new System.Collections.Generic.List<Tuple<RectangleF,int,int>>();
        private int affinityMask,affinityColumn=-1;
        private bool hoveringOrigin;
        private Selection preview { get { return selection.Preview(hovered); } }
        private readonly IconButton[] types=new IconButton[9];
        private readonly TypeSelector row=new TypeSelector();
        private readonly DrawingPanel canvas=new DrawingPanel();
        private readonly ApplicationTips tips=ApplicationTips.Create();
        private readonly bool rememberSession;
        private void SaveSession() {
            if(rememberSession)new SessionState { Main=selection.Main,Sub=selection.Sub,Current=currentView,Radar=radarView,Matrix=matrixView }.Save(SessionState.FilePath);
        }
        internal MainForm(bool remember=false) {
            Text="Aniimo Affinities"; ClientSize=new Size(902,500); MinimumSize=new Size(878,500);
            StartPosition=FormStartPosition.CenterScreen; BackColor=Chrome.Background; ForeColor=Chrome.TextColor;
            Font=Chrome.UiFont("Segoe UI",9);AutoScaleMode=AutoScaleMode.Dpi;
            row.Dock=DockStyle.Top;row.Height=62;canvas.Dock=DockStyle.Fill;
            EnableWindowDrag(this);EnableWindowDrag(row);EnableWindowDrag(canvas);
            var reset=new IconButton { Glyph="reset",Shortcut="0",TabIndex=9,Segment=true,SegmentEnd=true,Width=40,Height=36,AccessibleName="Clear types" };
            reset.Location=new Point(13,11);reset.Click+=(s,e)=>{hovered=-1;selection.Clear();RefreshSelection();};tips.SetToolTip(reset,"All elements");row.Controls.Add(reset);
            string[] order={"Dark","Earth","Fire","Grass","Ice","Light","Lightning","Water","Wind"};
            foreach(string name in order) {
                int type=Array.IndexOf(Model.Names,name);
                var button=new IconButton { Type=type,Shortcut=(Array.IndexOf(order,name)+1).ToString(),Segment=true,SegmentStart=name=="Dark",Height=36,AccessibleName=name,TabIndex=Array.IndexOf(order,name) };
                button.Click+=(s,e)=>{hovered=-1;selection.Toggle(type);RefreshSelection();};tips.SetToolTip(button,name);types[type]=button;row.Controls.Add(button);
                button.MouseEnter+=(s,e)=>{hovered=type;canvas.Invalidate();};
                button.MouseLeave+=(s,e)=>{if(hovered==type)hovered=-1;canvas.Invalidate();};
                // Focus indicates keyboard position; only actual pointer hover previews a type.
                button.GotFocus+=(s,e)=>button.Invalidate();
                button.LostFocus+=(s,e)=>button.Invalidate();
                button.AccessibleDescription="Shortcut: "+button.Shortcut;
            }
            row.Resize+=(s,e)=>LayoutRow();canvas.Paint+=PaintAffinities;
            canvas.MouseMove+=(s,e)=> {
                var origin=originHits.FirstOrDefault(item=>item.Item1.Contains(e.Location));
                var hit=origin ?? affinityHits.FirstOrDefault(item=>item.Item1.Contains(e.Location));
                int mask=hit==null?0:hit.Item2,column=hit==null?-1:hit.Item3;
                bool fromOrigin=origin!=null;
                if(mask!=affinityMask || column!=affinityColumn || hoveringOrigin!=fromOrigin) { hoveringOrigin=fromOrigin;affinityMask=mask;affinityColumn=column;canvas.Invalidate(); }
            };
            canvas.MouseLeave+=(s,e)=> { hoveringOrigin=false;affinityMask=0;affinityColumn=-1;canvas.Invalidate(); };
            Deactivate+=(s,e)=>{hovered=-1;canvas.Invalidate();foreach(Control control in row.Controls)control.Invalidate();};
            Activated+=(s,e)=>{hovered=-1;canvas.Invalidate();foreach(Control control in row.Controls)control.Invalidate();};
            Controls.Add(canvas);Controls.Add(row);CaptionContent=row;
            var saved=remember?SessionState.Load(SessionState.FilePath):new SessionState();
            selection.Main=saved.Main;selection.Sub=saved.Sub;currentView=saved.Current;radarView=saved.Radar;matrixView=saved.Matrix;inlineRadar=currentView=="radar";
            if(currentView=="matrix") { diagramSize=Size;Height=680; }
            RefreshSelection();rememberSession=remember;
            RefreshShortcuts();
        }
        internal Selection TutorialSelection { get { return new Selection { Main=selection.Main<0?Array.IndexOf(Model.Names,"Grass"):selection.Main,Sub=selection.Main<0?Array.IndexOf(Model.Names,"Water"):selection.Sub }; } }
        internal Keys[] TutorialBindings { get { return (Keys[])bindings.Clone(); } }
        internal MainForm CreateTutorialPreview() {
            var form=new MainForm();var selected=TutorialSelection;form.selection.Main=selected.Main;form.selection.Sub=selected.Sub;form.bindings=TutorialBindings;form.RefreshSelection();form.RefreshShortcuts();return form;
        }
        internal Rectangle TutorialTarget(string name) {
            Control target=name=="types"?row:name=="content"?canvas:TutorialCaptionTarget(name);
            return target==null?ClientRectangle:RectangleToClient(target.RectangleToScreen(target.ClientRectangle));
        }
        private void StartTutorial() { using(var tour=new TutorialOverlay(this))tour.ShowDialog(Form.ActiveForm??this); }
        protected override void OnShown(EventArgs e) {
            base.OnShown(e);
            if(rememberSession && !TutorialPreferences.Hidden)BeginInvoke(new Action(()=> { if(!IsDisposed && Visible)StartTutorial(); }));
        }
        protected override void OpenSettings() {
            hovered=-1;canvas.Invalidate();
            using(var dialog=new SettingsDialog(bindings,()=>selection.Main,StartTutorial))if(dialog.ShowDialog(this)==DialogResult.OK) { bindings=dialog.Result;RefreshShortcuts(); }
        }
        protected override void OpenMatrix() {
            OpenView(matrixView);
        }
        protected override Keys MatrixShortcut { get { return bindings[14]; } }
        protected override Keys RadarShortcut { get { return bindings[15]; } }
        private bool inlineRadar;
        private string currentView="affinities",radarView="radar",matrixView="matrix";
        private Size diagramSize;
        protected override string RadarViewGlyph { get { return radarView; } }
        protected override string MatrixViewGlyph { get { return matrixView; } }
        protected override bool InlineRadar { get { return inlineRadar; } }
        protected override void ToggleInlineRadar() { SwapView(ref radarView); }
        protected override void ToggleInlineMatrix() { SwapView(ref matrixView); }
        private void SwapView(ref string slot) {
            string previous=currentView;currentView=slot;slot=previous;inlineRadar=currentView=="radar";
            if(currentView=="matrix") { diagramSize=new Size((int)(Width/AppliedWindowScale),(int)(Height/AppliedWindowScale));Height=Math.Max(Height,(int)(680*AppliedWindowScale)); }
            else if(previous=="matrix" && diagramSize.Width>0)Size=new Size((int)(diagramSize.Width*AppliedWindowScale),(int)(diagramSize.Height*AppliedWindowScale));
            hovered=-1;affinityMask=0;RefreshRadarShortcut();RefreshMatrixShortcut();canvas.Invalidate();SaveSession();
        }
        protected override void OpenRadar() {
            OpenView(radarView);
        }
        private void OpenView(string view) {
            hovered=-1;canvas.Invalidate();
            if(view=="radar")using(var dialog=new RadarDialog(selection.Main,selection.Sub,bindings))dialog.ShowDialog(this);
            else if(view=="matrix")using(var dialog=new MatrixDialog(selection.Sub>=0?selection.Main:-1,bindings,selection.Main,selection.Sub))dialog.ShowDialog(this);
            else using(var dialog=new MainForm()) { dialog.selection.Main=selection.Main;dialog.selection.Sub=selection.Sub;dialog.bindings=(Keys[])bindings.Clone();dialog.RefreshSelection();dialog.RefreshShortcuts();dialog.ShowDialog(this); }
        }
        protected override void OnOverlayLockChanged() { hovered=-1;canvas.Invalidate(); }
        private void RefreshShortcuts() {
            RefreshMatrixShortcut();
            RefreshRadarShortcut();
            foreach(var button in row.Controls.OfType<IconButton>()) {
                button.Shortcut=KeyBindings.Label(bindings[button.Type<0?0:Array.IndexOf(KeyBindings.Actions,Model.Names[button.Type])]);
                button.AccessibleDescription=button.Shortcut.Length>0?"Shortcut: "+button.Shortcut:"No shortcut";
                tips.SetToolTip(button,(button.Type<0?"All elements":Model.Names[button.Type])+(button.Shortcut.Length>0?" · "+button.Shortcut:""));
                button.Invalidate();
            }
        }
        // Inputs: window-local key. Output: advertised digit or focused-row navigation. Fallback: native Tab/Space/Alt+F4. Raises: none.
        protected override bool ProcessCmdKey(ref Message message,Keys keyData) {
            int action=Array.IndexOf(bindings,keyData);
            if(action>=10) {
                if((message.LParam.ToInt64()&(1L<<30))==0) {
                    if(action==10)SetPinned(!TopMost);
                    else if(action==11)SetOverlayLocked(!OverlayLocked);
                    else if(action==12)OpenSettings();
                    else if(action==13)WindowState=FormWindowState.Minimized;
                    else if(action==14)OpenMatrix();
                    else if(action==15)OpenRadar();
                }
                return true;
            }
            if(OverlayLocked)return base.ProcessCmdKey(ref message,keyData);
            Keys key=keyData&Keys.KeyCode, modifiers=keyData&Keys.Modifiers;
            if((modifiers&(Keys.Control|Keys.Alt))!=0)return base.ProcessCmdKey(ref message,keyData);
            var buttons=row.Controls.OfType<IconButton>().Where(button=>button.Visible).OrderBy(button=>button.TabIndex).ToArray();
            int digit=KeyBindings.Resolve(bindings,keyData);
            if(digit>=0) {
                var target=row.Controls.OfType<IconButton>().FirstOrDefault(button=>digit==0?button.Type<0:button.Type>=0 && Model.Names[button.Type]==KeyBindings.Actions[digit]);
                if(target!=null && target.Visible && (message.LParam.ToInt64()&(1L<<30))==0)target.PerformClick();
                return true;
            }
            int focused=Array.FindIndex(buttons,button=>button.Focused);
            if(focused>=0 && modifiers==Keys.None) {
                int next=key==Keys.Left?(focused+buttons.Length-1)%buttons.Length:key==Keys.Right?(focused+1)%buttons.Length:key==Keys.Home?0:key==Keys.End?buttons.Length-1:-1;
                if(next>=0) { buttons[next].Focus();return true; }
                if(key==Keys.Enter) { if((message.LParam.ToInt64()&(1L<<30))==0)buttons[focused].PerformClick();return true; }
            }
            return base.ProcessCmdKey(ref message,keyData);
        }
        // Inputs: current selection. Output: Main/Sub badges and accessible state. Fallback: empty prompt. Raises: none.
        private void RefreshSelection() {
            hoveringOrigin=false;affinityMask=0;affinityColumn=-1;
            foreach(var button in types) { button.Badge=button.Type==selection.Main?"Main":button.Type==selection.Sub?"Sub":"";button.Active=button.Badge.Length>0;button.AccessibleName=Model.Names[button.Type]+(button.Active?" · "+button.Badge:"");button.Invalidate(); }
            LayoutRow();canvas.Invalidate();SaveSession();
        }
        private void LayoutRow() { row.Arrange(selection.Main>=0); }
        private void PaintAffinities(object sender,PaintEventArgs e) {
            affinityHits.Clear();
            originHits.Clear();
            var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
            if(preview.Main<0 && currentView=="matrix") { DrawInlineMatrix(g,8);return; }
            if(preview.Main<0) { PaintEmptyState(g);return; }
            int a=preview.Main,b=preview.Sub;string name=Model.Names[a]+(b>=0?" + "+Model.Names[b]:"");
            if(!TopMost) {
                VectorIcon.Draw(g,a,new RectangleF(18,12,36,36));if(b>=0)VectorIcon.Draw(g,b,new RectangleF(64,12,36,36));
                using(var title=Chrome.UiFont("Segoe UI",17,FontStyle.Bold))Chrome.Text(g,name,title,Chrome.TextColor,new RectangleF(b>=0?116:70,5,500,35),false);
                Chrome.Text(g,hovered>=0 && hovered!=selection.Main && hovered!=selection.Sub?"Temporary preview":"Type affinities",Font,Chrome.Muted,new RectangleF(b>=0?118:72,39,300,20),false);
            }
            float width=(canvas.Width-36)/2f;var factors=Model.Factors(a,b);
            float cardTop=TopMost?8:78;
            if(currentView=="matrix") {
                DrawInlineMatrix(g,cardTop);return;
            }
            for(int column=0;column<2;column++)DrawCard(g,new RectangleF(12+column*(width+12),cardTop,width,canvas.Height-cardTop-12),column,factors,name);
        }
        private void DrawInlineMatrix(Graphics g,float cardTop) {
                float scale=Math.Min((canvas.Width-32)/680f,(canvas.Height-cardTop-16)/440f);
                float left=(canvas.Width-680*scale)/2;
                for(int r=0;r<10;r++)for(int c=0;c<10;c++) {
                    var state=g.Save();g.TranslateTransform(left+c*68*scale,cardTop+r*44*scale);g.ScaleTransform(scale,scale);
                    MatrixDialog.DrawCell(g,r,c,new Rectangle(0,0,64,40),preview.Sub>=0?preview.Main:-1);g.Restore(state);
                }
        }
        // Inputs: shared projections. Output: two directed diagrams, grouping equal multipliers. Fallback: dash for empty groups. Raises: none.
        private void DrawCard(Graphics g,RectangleF box,int column,double[] factors,string name) {
            Chrome.Box(g,box,Chrome.Surface,Chrome.Border,Chrome.PanelRadius);
            Chrome.Box(g,new RectangleF(box.X+16,box.Y+16,32,32),Chrome.AccentSurface,Chrome.AccentSurface,9);
            Chrome.Glyph(g,column==0?"atk":"def",new RectangleF(box.X+22,box.Y+22,20,20),Chrome.AccentForeground);
            using(var bold=Chrome.UiFont("Segoe UI",13,FontStyle.Bold))Chrome.Text(g,column==0?"Attack":"Defense",bold,column==0?Chrome.Attack:Chrome.Defense,new RectangleF(box.X+60,box.Y+11,box.Width-76,27),false);
            using(var caption=Chrome.UiFont("Segoe UI",8.5f))Chrome.Text(g,column==0?"Damage dealt · best available type":"Damage received · combined types",caption,Chrome.Muted,new RectangleF(box.X+60,box.Y+37,box.Width-76,20),false);
            using(var line=new Pen(Chrome.Border))g.DrawLine(line,box.X+16,box.Y+68,box.Right-16,box.Y+68);
            float cy=box.Y+68+(box.Height-68)/2, cx=box.X+box.Width/2;
            if(inlineRadar) {
                float radius=Math.Max(30,Math.Min(box.Width/2-80,(box.Height-68)/2-65));
                RadarDialog.DrawRadar(g,new PointF(cx,cy-4),radius,column==0,column==1,preview);
                return;
            }
            float centerHeight=preview.Sub>=0?112:64;
            Chrome.Box(g,new RectangleF(cx-29,cy-centerHeight/2,58,centerHeight),Chrome.Background,Chrome.Border,16);
            if(preview.Sub>=0) {
                HighlightOrigin(g,new RectangleF(cx-22,cy-49,44,44),column,0);
                HighlightOrigin(g,new RectangleF(cx-22,cy+5,44,44),column,1);
                VectorIcon.Draw(g,preview.Main,new RectangleF(cx-18,cy-45,36,36));
                VectorIcon.Draw(g,preview.Sub,new RectangleF(cx-18,cy+9,36,36));
                using(var line=new Pen(Chrome.Border))g.DrawLine(line,cx-12,cy,cx+12,cy);
            } else {
                HighlightOrigin(g,new RectangleF(cx-23,cy-23,46,46),column,0);
                VectorIcon.Draw(g,preview.Main,new RectangleF(cx-19,cy-19,38,38));
            }
            using(var small=Chrome.UiFont("Segoe UI",8.5f,FontStyle.Bold))Chrome.Text(g,name.Replace(" + ","\n"),small,Chrome.TextColor,new RectangleF(cx-54,cy+centerHeight/2+10,108,38),true);
            for(int side=0;side<2;side++) {
                var entries=Enumerable.Range(0,9).Where(t=>side==0?factors[column*9+t]<1:factors[column*9+t]>1).ToArray();
                Color color=column==side?Chrome.Muted:Chrome.AccentForeground;
                float outer=side==0?box.X+12:box.Right-66;
                DrawStack(g,entries,factors,column,outer,cy,box.Height-100);
                float arrowX=side==0?box.X+78:cx+39, arrowW=box.Width/2-117;
                if(entries.Length>0) {
                    bool right=column==0?side==1:side==0;
                    using(var pen=new Pen(color,1.3f)) { float end=right?arrowX+arrowW:arrowX;g.DrawLine(pen,arrowX,cy,arrowX+arrowW,cy);g.DrawLine(pen,end,cy,end+(right?-6:6),cy-6);g.DrawLine(pen,end,cy,end+(right?-6:6),cy+6); }
                    var multipliers=entries.Select(t=>factors[column*9+t]).Distinct().OrderBy(multiplier=>multiplier).ToArray();
                    string value=multipliers.Length==1?Model.Factor(multipliers[0]):Model.Factor(multipliers[0])+"–"+Model.Factor(multipliers[multipliers.Length-1]);
                    var valueBox=new RectangleF(arrowX-7,cy-40,arrowW+14,26);
                    Chrome.Box(g,valueBox,Chrome.Background,Chrome.Border,7);
                    using(var bold=Chrome.UiFont("Segoe UI",multipliers.Length==1?10:8.5f,FontStyle.Bold))Chrome.Text(g,value,bold,color,valueBox,true);
                }
                string label=column==0?(side==0?"Not effective":"Super effective"):(side==0?"Resistance":"Weakness");
                using(var small=Chrome.UiFont("Segoe UI",8,FontStyle.Regular))Chrome.Text(g,label,small,color,new RectangleF(arrowX-12,cy+12,arrowW+24,34),true);
            }
        }
        private void HighlightOrigin(Graphics g,RectangleF bounds,int column,int origin) {
            originHits.Add(Tuple.Create(bounds,1<<origin,column));
            if(affinityColumn!=column || (affinityMask&(1<<origin))==0)return;
            Chrome.Box(g,bounds,Chrome.AccentSurface,Chrome.Accent,12);
        }
        private void DrawStack(Graphics g,int[] entries,double[] values,int column,float x,float cy,float available) {
            if(entries.Length==0) { Chrome.Text(g,"—",Font,Chrome.Muted,new RectangleF(x,cy-12,54,24),true);return; }
            var ordered=OriginGroups.Build(preview.Main,preview.Sub,column,entries);
            float cell=Math.Min(62,available/entries.Length),height=cell*entries.Length;
            float y=cy-height/2;
            Chrome.Box(g,new RectangleF(x,y,54,height),Chrome.Background,Chrome.Border,11);
            if(hoveringOrigin && affinityColumn==column) {
                for(int first=0;first<ordered.Length;first++) {
                    if((ordered[first].Mask&affinityMask)==0)continue;
                    int last=first;
                    while(last+1<ordered.Length && (ordered[last+1].Mask&affinityMask)!=0)last++;
                    Chrome.Box(g,new RectangleF(x+1,y+first*cell+1,52,(last-first+1)*cell-2),Chrome.AccentSurface,Chrome.Accent,10);
                    first=last;
                }
            }
            // Parallel source rails preserve shared membership without intersecting outlines.
            for(int origin=0;origin<(preview.Sub<0?1:2);origin++) {
                int bit=1<<origin,type=origin==0?preview.Main:preview.Sub;
                using(var rail=new Pen(Model.Colors[type],2) { StartCap=LineCap.Round,EndCap=LineCap.Round }) {
                    for(int first=0;first<ordered.Length;first++) {
                        if((ordered[first].Mask&bit)==0)continue;
                        int last=first;
                        while(last+1<ordered.Length && (ordered[last+1].Mask&bit)!=0)last++;
                        float railX=x+4+origin*4;
                        g.DrawLine(rail,railX,y+first*cell+9,railX,y+(last+1)*cell-9);
                        first=last;
                    }
                }
            }
            using(var divider=new Pen(Chrome.Border))for(int i=1;i<ordered.Length;i++)
                g.DrawLine(divider,x+15,y+i*cell,x+43,y+i*cell);
            using(var small=Chrome.UiFont("Segoe UI",8.5f,FontStyle.Bold))for(int i=0;i<entries.Length;i++) {
                affinityHits.Add(Tuple.Create(new RectangleF(x,y+i*cell,54,cell),ordered[i].Mask,column));
                float icon=Math.Min(30,cell-21);VectorIcon.Draw(g,ordered[i].Type,new RectangleF(x+4+(54-icon)/2,y+i*cell+5,icon,icon));
                Chrome.Text(g,Model.Factor(values[column*9+ordered[i].Type]),small,Chrome.TextColor,new RectangleF(x+9,y+(i+1)*cell-18,43,16),true);
            }
        }
        // Inputs: empty selection and viewport. Output: centered illustrative invitation, with no invented matchup values. Fallback: none. Raises: none.
        private void PaintEmptyState(Graphics g) {
            float cx=canvas.Width/2f,cy=canvas.Height/2f-54;
            using(var orbit=new Pen(Chrome.Border,1)) {
                g.DrawEllipse(orbit,cx-69,cy-69,138,138);
                g.DrawEllipse(orbit,cx-45,cy-45,90,90);
            }
            string[] order={"Dark","Earth","Fire","Grass","Ice","Light","Lightning","Water","Wind"};
            for(int i=0;i<order.Length;i++) {
                double angle=(-90+i*40)*Math.PI/180;
                float x=cx+(float)Math.Cos(angle)*69,y=cy+(float)Math.Sin(angle)*69;
                using(var halo=new SolidBrush(Chrome.Background))g.FillEllipse(halo,x-17,y-17,34,34);
                VectorIcon.Draw(g,Array.IndexOf(Model.Names,order[i]),new RectangleF(x-11,y-11,22,22));
            }
            Chrome.Glyph(g,"atk",new RectangleF(cx-31,cy-13,26,26),Chrome.Muted);
            Chrome.Glyph(g,"def",new RectangleF(cx+6,cy-13,26,26),Chrome.Muted);
            using(var heading=Chrome.UiFont("Segoe UI",17,FontStyle.Bold))Chrome.Text(g,"Explore type affinities",heading,Chrome.TextColor,new RectangleF(0,cy+99,canvas.Width,34),true);
            using(var body=Chrome.UiFont("Segoe UI",10))Chrome.Text(g,"Hover to explore · Click to select",body,Chrome.Muted,new RectangleF(0,cy+139,canvas.Width,25),true);
            using(var hint=Chrome.UiFont("Segoe UI",8.5f))Chrome.Text(g,"Choose a main type, then a second if needed.",hint,Chrome.Muted,new RectangleF(0,cy+174,canvas.Width,22),true);
        }
        protected override void Dispose(bool disposing) { if(disposing)tips.Dispose();base.Dispose(disposing); }
    }
}

