using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities {
    // Native view of the same build-generated factors and ranking as ElementMatrixDialog.
    internal sealed class MatrixDialog : PortableWindow {
        protected override bool CompactCaption { get { return true; } }
        protected override string CaptionGlyph { get { return "matrix"; } }
        private static readonly int[] order=new[]{"Fire","Water","Grass","Lightning","Ice","Earth","Wind","Light","Dark"}.Select(name=>Array.IndexOf(Model.Names,name)).ToArray();
        private readonly ApplicationTips tips=ApplicationTips.Create();
        private readonly DrawingPanel[,] cells=new DrawingPanel[10,10];
        private readonly IconButton[] selectors=new IconButton[9];
        private readonly IconButton single;
        private readonly SettingsButton highlight;
        private readonly TypeSelector selector=new TypeSelector();
        private int fixedType,hoverRow=-1,hoverColumn=-1;
        private bool bestPinned,bestPreview;
        private readonly Keys[] bindings;
        internal MatrixDialog(int initialBase,Keys[] configuredBindings=null,int radarMain=-1,int radarSub=-1) {
            bindings=(Keys[])(configuredBindings ?? KeyBindings.Load(KeyBindings.FilePath)).Clone();
            fixedType=initialBase;Text="Effectiveness matrix";ClientSize=new Size(720,592);
            StartPosition=FormStartPosition.CenterParent;MaximizeBox=false;MinimizeBox=false;ShowInTaskbar=false;
            BackColor=Chrome.Background;ForeColor=Chrome.TextColor;Font=Chrome.UiFont("Segoe UI",9);
            AddAxisLegend(20,"rows","atk","Rows: attacker");
            AddAxisLegend(96,"columns","def","Columns: defender");
            Controls.Add(new Label { Text="× Damage multiplier",ForeColor=Chrome.Muted,Location=new Point(176,46),Size=new Size(180,20) });
            selector.Location=new Point(8,66);selector.Height=54;Controls.Add(selector);
            tips.SetToolTip(selector,"Select a type to compare dual-type defenders.");
            single=new IconButton { Glyph="reset",Shortcut=KeyBindings.Label(bindings[0]),TabIndex=9,AccessibleName="Single types" };
            single.Click+=(s,e)=>SelectBase(-1);selector.Controls.Add(single);SetShortcutTip(single,"Single-type matrix");
            for(int i=0;i<9;i++) {
                int type=Array.IndexOf(Model.Names,KeyBindings.Actions[i+1]);
                var button=new IconButton { Type=type,Shortcut=KeyBindings.Label(bindings[i+1]),TabIndex=i,AccessibleName=Model.Names[type] };
                button.Click+=(s,e)=>SelectBase(fixedType==type?-1:type);selectors[i]=button;selector.Controls.Add(button);
                SetShortcutTip(button,Model.Names[type]+" + each defending column");
            }
            highlight=new SettingsButton { Text="Best matchups",Location=new Point(556,80),Size=new Size(140,30),TabIndex=10 };
            highlight.Click+=(s,e)=>{bestPinned=!bestPinned;RefreshGrid();};
            highlight.MouseEnter+=(s,e)=>{bestPreview=true;RefreshGrid();};highlight.MouseLeave+=(s,e)=>{bestPreview=false;RefreshGrid();};
            Controls.Add(highlight);tips.SetToolTip(highlight,"Highlight strongest attacking rows and resisting columns"+(bindings.Contains(Keys.H)?"":" · H"));
            var radar=new SettingsButton { Glyph="radar",Location=new Point(510,80),Size=new Size(34,30),TabIndex=11,AccessibleName="Affinity radar" };
            radar.Click+=(s,e)=>{bool original=fixedType==initialBase && radarMain>=0;using(var dialog=new RadarDialog(original?radarMain:fixedType,original?radarSub:-1,bindings))dialog.ShowDialog(this);};
            Controls.Add(radar);tips.SetToolTip(radar,"Affinity radar · Compare attack and defense");
            Activated+=(s,e)=>selector.Invalidate(true);Deactivate+=(s,e)=>selector.Invalidate(true);
            for(int row=0;row<10;row++)for(int column=0;column<10;column++) {
                int r=row,c=column;
                var cell=new DrawingPanel { Location=new Point(20+column*68,124+row*44),Size=new Size(64,40),TabStop=false,AccessibleRole=AccessibleRole.StaticText };
                cell.Paint+=(s,e)=>PaintCell(e.Graphics,r,c,cell.ClientRectangle);
                cell.MouseEnter+=(s,e)=>{hoverRow=r;hoverColumn=c;InvalidateGrid();};
                cell.MouseLeave+=(s,e)=>{hoverRow=-1;hoverColumn=-1;InvalidateGrid();};
                cells[row,column]=cell;Controls.Add(cell);
            }
            RefreshGrid();
        }
        private void AddAxisLegend(int left,string axis,string role,string description) {
            var legend=new DrawingPanel { Location=new Point(left,42),Size=new Size(64,24),TabStop=false,AccessibleRole=AccessibleRole.StaticText,AccessibleName=description };
            legend.Paint+=(s,e)=> {
                e.Graphics.Clear(Chrome.Background);
                Chrome.Glyph(e.Graphics,axis,new RectangleF(0,2,20,20),Chrome.Muted);
                Chrome.Glyph(e.Graphics,role,new RectangleF(30,2,20,20),Chrome.TextColor);
            };
            Controls.Add(legend);tips.SetToolTip(legend,description);
        }
        private void SelectBase(int type) { fixedType=type;RefreshGrid(); }
        private void SetShortcutTip(IconButton button,string label) {
            button.AccessibleDescription=button.Shortcut.Length>0?"Shortcut: "+button.Shortcut:"No shortcut";
            tips.SetToolTip(button,label+(button.Shortcut.Length>0?" · "+button.Shortcut:""));
        }
        private double Factor(int attack,int defense) { return Model.Factors(defense,fixedType)[9+attack]; }
        private void RefreshGrid() {
            selector.Arrange(fixedType>=0);highlight.Primary=bestPinned;highlight.Invalidate();
            foreach(var button in selectors) { button.Active=button.Type==fixedType;button.Invalidate(); }
            for(int row=0;row<10;row++)for(int column=0;column<10;column++) {
                var cell=cells[row,column];if(cell==null)continue;
                string label;
                if(row==0 && column==0)label="Rows attack · Columns defend";
                else if(column==0)label=Model.Names[order[row-1]]+" attack";
                else {
                    int defense=order[column-1];string defenders=Model.Names[defense]+(fixedType>=0 && fixedType!=defense?" + "+Model.Names[fixedType]:"");
                    label=defenders+" defense";
                    if(row>0) {
                        int attack=order[row-1];label=Model.Names[attack]+" → "+defenders+": "+Model.Factor(Factor(attack,defense));
                        if(fixedType>=0 && fixedType!=defense)label+=" ("+Model.Factor(Model.Factors(defense,-1)[9+attack])+" × "+Model.Factor(Model.Factors(fixedType,-1)[9+attack])+")";
                    }
                }
                cell.AccessibleName=label;tips.SetToolTip(cell,label);cell.Invalidate();
            }
        }
        private void InvalidateGrid() { foreach(var cell in cells)cell.Invalidate(); }
        private void PaintCell(Graphics g,int row,int column,Rectangle area) {
            g.Clear(Chrome.Background);DrawCell(g,row,column,area,fixedType,hoverRow,hoverColumn,bestPinned||bestPreview);
        }
        internal static void DrawCell(Graphics g,int row,int column,Rectangle area,int fixedType,int hoverRow=-1,int hoverColumn=-1,bool highlight=false) {
            g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            bool tracked=(hoverRow>0 && row==hoverRow)||(hoverColumn>0 && column==hoverColumn);
            Color fill=tracked?Chrome.AccentSurface:Chrome.Surface,border=tracked?Chrome.Accent:Chrome.Border;
            if(row==0 || column==0) {
                if(row!=column && !tracked) { fill=row==0?Chrome.DefenseSurface:Chrome.AttackSurface;border=row==0?Chrome.Defense:Chrome.Attack; }
                Chrome.Box(g,new RectangleF(1,1,area.Width-2,area.Height-2),fill,border,8);
                if(row==0 && column==0) {
                    using(var outline=Chrome.Round(new RectangleF(1,1,area.Width-2,area.Height-2),8))
                    using(var lower=new System.Drawing.Drawing2D.GraphicsPath())
                    using(var upper=new System.Drawing.Drawing2D.GraphicsPath()) {
                        lower.AddPolygon(new[]{new PointF(0,0),new PointF(area.Width,area.Height),new PointF(0,area.Height)});
                        upper.AddPolygon(new[]{new PointF(0,0),new PointF(area.Width,0),new PointF(area.Width,area.Height)});
                        var state=g.Save();g.SetClip(lower,System.Drawing.Drawing2D.CombineMode.Intersect);
                        using(var pen=new Pen(Chrome.Attack,1))g.DrawPath(pen,outline);
                        g.Restore(state);
                        state=g.Save();g.SetClip(upper,System.Drawing.Drawing2D.CombineMode.Intersect);
                        using(var pen=new Pen(Chrome.Defense,1))g.DrawPath(pen,outline);
                        g.Restore(state);
                    }
                    using(var divider=new Pen(Chrome.Border,1))g.DrawLine(divider,6,4,area.Width-6,area.Height-4);
                    Chrome.Glyph(g,"atk",new RectangleF(6,area.Height-22,18,18),Chrome.Muted);
                    Chrome.Glyph(g,"def",new RectangleF(area.Width-24,4,18,18),Chrome.Muted);
                    return;
                }
                int type=order[(row==0?column:row)-1];
                bool pair=row==0 && fixedType>=0 && fixedType!=type;
                VectorIcon.Draw(g,type,new RectangleF(pair?7:20,8,24,24));
                if(pair)VectorIcon.Draw(g,fixedType,new RectangleF(34,8,24,24));return;
            }
            int attack=order[row-1],defense=order[column-1];double value=Model.Factors(defense,fixedType)[9+attack];
            bool best=Model.MatrixBest(fixedType,attack,defense);
            bool muted=highlight&&!best;
            fill=muted?Chrome.Surface:Chrome.MultiplierFill(value);
            border=tracked || highlight&&best?Chrome.Accent:muted?Chrome.Border:Chrome.MultiplierBorder(value);
            Chrome.Box(g,new RectangleF(1,1,area.Width-2,area.Height-2),fill,border,8);
            Color ink=muted?Chrome.Muted:Chrome.MultiplierInk(value);
            using(var font=Chrome.UiFont("Segoe UI",10,FontStyle.Bold))Chrome.Text(g,Model.Factor(value),font,ink,area,true);
        }
        protected override bool ProcessCmdKey(ref Message message,Keys input) {
            if(input==Keys.Escape) { Close();return true; }
            int index=KeyBindings.Resolve(bindings,input);
            if(index>=0 && index<10) {
                if((message.LParam.ToInt64()&(1L<<30))==0) {
                    int type=index==0?-1:selectors[index-1].Type;
                    SelectBase(type==fixedType?-1:type);
                }
                return true;
            }
            if(input==Keys.H && !bindings.Contains(Keys.H)) { if((message.LParam.ToInt64()&(1L<<30))==0) { bestPinned=!bestPinned;RefreshGrid(); }return true; }
            return base.ProcessCmdKey(ref message,input);
        }
        protected override void Dispose(bool disposing) { if(disposing)tips.Dispose();base.Dispose(disposing); }
    }
}
