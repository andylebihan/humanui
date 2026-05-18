using Eto.Forms;

namespace HumanUI
{
    /// <summary>
    /// Marker subclass of Eto.Forms.Button so type-based switch statements in
    /// ValueListener / SetButton can identify "true-only" semantics.
    /// </summary>
    public class TrueOnlyButton : Button
    {
        public TrueOnlyButton() : base() { }
    }
}
