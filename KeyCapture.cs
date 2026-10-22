using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities {
    internal sealed class KeyCapture : Control, IMessageFilter {
        private Keys binding;
        private bool listening,hover;
        private Keys pending,heldModifiers,primaryKey;
        private bool primaryReleased;
        private void BeginCapture() { pending=Keys.None;listening=true;Invalidate(); }
        internal void ReleaseCapturedKey(Keys key) {
            if(pending==Keys.None)return;
            if(key==primaryKey)primaryReleased=true;
            if(key==Keys.ControlKey || key==Keys.LControlKey || key==Keys.RControlKey)heldModifiers&=~Keys.Control;
            if(key==Keys.ShiftKey || key==Keys.LShiftKey || key==Keys.RShiftKey)heldModifiers&=~Keys.Shift;
            if(key==Keys.Menu || key==Keys.LMenu || key==Keys.RMenu)heldModifiers&=~Keys.Alt;
            if(primaryReleased && heldModifiers==Keys.None) {
                Keys result=pending;pending=Keys.None;
                Binding=result;ShowValidation(false,true);if(Feedback!=null)Feedback("");
            }
        }
        public bool PreFilterMessage(ref Message message) {
            if(message.Msg==0x1C && message.WParam==IntPtr.Zero && listening) { RefreshLabel();return false; }
            if(pending==Keys.None || !ContainsFocus)return false;
            if(message.Msg==0x101 || message.Msg==0x105) { ReleaseCapturedKey((Keys)message.WParam.ToInt32());return true; }
            return false;
        }
        internal Action<string> Feedback;
        internal bool AllowModifiers;
        internal event EventHandler BindingChanged,CaptureEnded;
        private readonly Timer validationTimer=new Timer { Interval=1500 };
        internal Func<Keys,bool> ValidateBinding;
        internal bool Invalid,Validated;
        internal void ShowValidation(bool invalid,bool validated) { validationTimer.Stop();Invalid=invalid;Validated=validated;if(validated)validationTimer.Start();Invalidate(); }
        internal Keys Binding { get { return binding; } set { binding=value;RefreshLabel();if(CaptureEnded!=null)CaptureEnded(this,EventArgs.Empty);if(BindingChanged!=null)BindingChanged(this,EventArgs.Empty); } }
        internal KeyCapture() {
            Application.AddMessageFilter(this);
            validationTimer.Tick+=(s,e)=>ShowValidation(false,false);
            SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.Selectable,true);
            TabStop=true;Height=28;BackColor=Chrome.Background;ForeColor=Chrome.TextColor;Cursor=Cursors.Hand;AccessibleRole=AccessibleRole.Text;
        }
        private void RefreshLabel() { pending=Keys.None;listening=false;Invalid=false;Validated=false;Text=binding==Keys.None?"Unassigned":KeyBindings.Label(binding);Invalidate(); }
        protected override void OnEnter(EventArgs e) { base.OnEnter(e);Invalidate(); }
        protected override void OnLeave(EventArgs e) { base.OnLeave(e);RefreshLabel();if(CaptureEnded!=null)CaptureEnded(this,EventArgs.Empty); }
        protected override void Dispose(bool disposing) { if(disposing) { Application.RemoveMessageFilter(this);validationTimer.Dispose(); }base.Dispose(disposing); }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e);if(e.Button==MouseButtons.Left) { Focus();BeginCapture(); } }
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e);hover=true;Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e);hover=false;Invalidate(); }
        protected override void OnPaint(PaintEventArgs e) {
            var g=e.Graphics;g.Clear(Chrome.Background);g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Chrome.Box(g,new RectangleF(2,2,Width-5,Height-5),listening?Chrome.AccentSurface:Chrome.Surface,listening?Chrome.AccentForeground:Focused?Chrome.AccentForeground:hover?Chrome.StrongBorder:Chrome.Border,6);
            Chrome.Text(g,pending!=Keys.None?KeyBindings.Label(pending)+" …":listening?"Press a key…":Text,Font,listening?Chrome.AccentForeground:binding==Keys.None?Chrome.Muted:Chrome.TextColor,new RectangleF(8,0,Width-16,Height),true);
            if(Invalid || Validated) {
                Color color=Invalid?Chrome.Error:Chrome.AccentForeground;
                using(var outline=Chrome.Round(new RectangleF(2,2,Width-5,Height-5),6))using(var pen=new Pen(color,1.5f))g.DrawPath(pen,outline);
                Chrome.Text(g,Invalid?"!":"✓",Font,color,new RectangleF(Width-24,0,20,Height),true);
            }
        }
        // Inputs: focused field keystroke. Output: one binding or Delete unbind. Fallback: Tab/Escape retain navigation/cancel. Raises: none.
        protected override bool ProcessCmdKey(ref Message message,Keys keyData) {
            Keys key=keyData&Keys.KeyCode,modifiers=keyData&Keys.Modifiers;
            if(key==Keys.Escape && listening) { RefreshLabel();if(CaptureEnded!=null)CaptureEnded(this,EventArgs.Empty);return true; }
            if(pending!=Keys.None)return true;
            if(key==Keys.Tab || key==Keys.Escape)return base.ProcessCmdKey(ref message,keyData);
            if((message.LParam.ToInt64()&(1L<<30))!=0)return true;
            if(key==Keys.Space && modifiers==Keys.None) { BeginCapture();return true; }
            if(key==Keys.Delete && modifiers==Keys.None) { Binding=Keys.None;if(Feedback!=null)Feedback("");return true; }
            if(!listening)return base.ProcessCmdKey(ref message,keyData);
            if(key==Keys.ControlKey || key==Keys.ShiftKey || key==Keys.Menu)return true;
            if(key==Keys.F4 && modifiers==Keys.Alt)return base.ProcessCmdKey(ref message,keyData);
            Keys physicalKey=key;
            if(key>=Keys.NumPad0 && key<=Keys.NumPad9)key=(Keys)((int)Keys.D0+(int)key-(int)Keys.NumPad0);
            if((AllowModifiers || modifiers==Keys.None || modifiers==Keys.Shift && key>=Keys.D0 && key<=Keys.D9) && KeyBindings.Choices().Contains(key)) {
                if(AllowModifiers)key|=modifiers;
                if(ValidateBinding!=null && !ValidateBinding(key)) { ShowValidation(true,false);return true; }
                pending=key;primaryKey=physicalKey;heldModifiers=modifiers;primaryReleased=false;Invalidate();
            } else ShowValidation(true,false);
            return true;
        }
    }
}
