namespace AniimoAffinities {
    internal sealed class Selection {
        internal int Main = -1, Sub = -1;
        // Inputs: clicked type. Output: at most two distinct types. Fallback: clicking selected removes it and promotes Sub. Raises: none.
        internal void Toggle(int type) {
            if(type == Main) { Main = Sub; Sub = -1; }
            else if(type == Sub) Sub = -1;
            else if(Main < 0) Main = type;
            else Sub = type;
        }
        // Inputs: hovered type (-1 when absent). Output: detached temporary selection; selected types preserve the committed pair. Fallback: committed selection. Raises: none.
        internal Selection Preview(int type) {
            var result=new Selection { Main=Main,Sub=Sub };
            if(type>=0 && type!=Main && type!=Sub)result.Toggle(type);
            return result;
        }
        internal void Clear() { Main = -1; Sub = -1; }
    }
}
