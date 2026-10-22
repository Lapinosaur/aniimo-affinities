using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AniimoAffinities
{
    internal static class MatrixTests
    {
        internal static void Run()
        {
            var matrixKeys = KeyBindings.Defaults();
            matrixKeys[1] = Keys.Q;
            matrixKeys[2] = Keys.H;
            matrixKeys[9] = Keys.None;
            using (var matrix = new MatrixDialog(-1, matrixKeys))
            {
                var handler = typeof(MatrixDialog).GetMethod("ProcessCmdKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var current = typeof(MatrixDialog).GetField("fixedType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var selectors = (IconButton[])typeof(MatrixDialog).GetField("selectors", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(matrix);
                var msg = Message.Create(matrix.Handle, 0x100, IntPtr.Zero, IntPtr.Zero);
                handler.Invoke(matrix, new object[] { msg, Keys.D1 });
                if ((int)current.GetValue(matrix) != -1)
                    throw new Exception("Matrix retained old binding");
                handler.Invoke(matrix, new object[] { msg, Keys.Q });
                if ((int)current.GetValue(matrix) != Array.IndexOf(Model.Names, "Dark") || selectors[0].Shortcut != "Q")
                    throw new Exception("Matrix rebind/badge mismatch");
                handler.Invoke(matrix, new object[] { msg, Keys.H });
                if ((int)current.GetValue(matrix) != Array.IndexOf(Model.Names, "Earth"))
                    throw new Exception("Matrix H collision");
                if (selectors[8].Shortcut != "")
                    throw new Exception("Matrix unbound badge visible");
            }
            for (int fixedType = -1; fixedType < 9; fixedType++)
                for (int attacker = 0; attacker < 9; attacker++)
                    for (int defender = 0; defender < 9; defender++)
                    {
                        Model.MatrixBest(fixedType, attacker, defender);
                        double expected = Model.Factors(defender, -1)[9 + attacker] * (fixedType < 0 || fixedType == defender ? 1 : Model.Factors(fixedType, -1)[9 + attacker]);
                        if (Math.Abs(Model.Factors(defender, fixedType)[9 + attacker] - expected) > 0.000001)
                            throw new Exception("Matrix projection mismatch");
                    }
        }
    }
}
