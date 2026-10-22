using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal sealed class TourStep
    {
        internal readonly string View, Target, Title, Body;
        internal TourStep(string view, string target, string title, string body)
        {
            View = view;
            Target = target;
            Title = title;
            Body = body;
        }
    }
}
