using System.Windows.Forms;

namespace ERFX_Q03UDV_20260121_01
{
    /// <summary>
    /// DataGridView with double buffering enabled for smoother rendering
    /// </summary>
    public class DoubleBufferedDataGridView : DataGridView
    {
        public DoubleBufferedDataGridView()
        {
            // Enable double buffering to reduce flickering
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);
            UpdateStyles();
        }
    }
}
