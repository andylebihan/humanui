using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Forms;
using Rhino.DocObjects;

namespace HumanUI
{
    /// <summary>
    /// Eto button that, when clicked, runs a Rhino object-pick prompt and stores
    /// the picked GUIDs. PickCompleted fires whether the user accepted or
    /// cancelled so downstream listeners (ValueListener) can refresh either way.
    /// </summary>
    public class HUI_RhPickButton : Button
    {
        public List<Guid> objIDs = new();
        private readonly string msg;
        private readonly bool allowMultiple;
        private readonly bool allowNone;
        private readonly ObjectType filter;

        public event EventHandler PickCompleted;

        public HUI_RhPickButton(string msg, bool allowMultiple, bool allowNone, ObjectType filter) : base()
        {
            this.msg = msg;
            this.allowMultiple = allowMultiple;
            this.allowNone = allowNone;
            this.filter = filter;
            Click += (_, _) => RunPick();
        }

        private void RunPick()
        {
            try
            {
                Rhino.Commands.Result result;
                ObjRef[] rhObjects;
                if (!allowMultiple)
                {
                    result = Rhino.Input.RhinoGet.GetOneObject(msg, allowNone, filter, out ObjRef rhObject);
                    rhObjects = new[] { rhObject };
                }
                else
                {
                    result = Rhino.Input.RhinoGet.GetMultipleObjects(msg, allowNone, filter, out rhObjects);
                }

                if (result == Rhino.Commands.Result.Success && rhObjects != null)
                {
                    objIDs = rhObjects.Where(o => o != null).Select(o => o.ObjectId).ToList();
                }
                PickCompleted?.Invoke(this, EventArgs.Empty);
            }
            catch
            {
                // Rhino picks can throw if the doc is mid-modification; just swallow
                // and fire PickCompleted so downstream solves see the cancel state.
                PickCompleted?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
