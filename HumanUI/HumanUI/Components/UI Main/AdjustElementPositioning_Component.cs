using System;
using System.Collections.Generic;
using Eto.Drawing;
using Eto.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace HumanUI.Components.UI_Main
{
    /// <summary>
    /// Adjust margin / size / alignment for a Control. WPF had Margin (Thickness),
    /// HorizontalAlignment, VerticalAlignment, and an Absolute Positioning escape
    /// hatch that re-parented onto a Grid. Eto.Forms uses Padding on parents and
    /// HorizontalContentAlignment on the layout item; rather than re-parenting
    /// every Set call, we apply alignment via the parent StackLayoutItem when
    /// reachable, and the size/padding directly on the control. Absolute
    /// positioning isn't supported in the Eto port — we surface a warning.
    /// </summary>
    public class AdjustElementPositioning_Component : GH_Component
    {
        public AdjustElementPositioning_Component()
            : base("Adjust Element Positioning", "AdjustPos",
                "Adjust the margins, sizing, and other positioning information of an element. \nAbsolute positioning can get a little wonky, use at your own risk.",
                "Human UI", "UI Main")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Elements to adjust", "E", "The UIElement you want to reposition.", GH_ParamAccess.item);
            pManager.AddTextParameter("Margin", "M", "The margin value. Input a single number to \naffect margins on all sides, or four values separated by commas\nto set Left, Top, Right, and Bottom individually.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Absolute Positioning", "Abs", "Set to true to position relative to the upper left corner of the document", GH_ParamAccess.item, false);
            pManager.AddNumberParameter("Width", "W", "Override the element width", GH_ParamAccess.item);
            pManager.AddNumberParameter("Height", "H", "Override the element height", GH_ParamAccess.item);
            pManager[1].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;

            pManager.AddIntegerParameter("Horizontal Alignment", "HA", "Horizontal alignment", GH_ParamAccess.item);
            pManager[5].Optional = true;
            var horizAlign = (Param_Integer)pManager[5];
            horizAlign.AddNamedValue("Left", 0);
            horizAlign.AddNamedValue("Center", 1);
            horizAlign.AddNamedValue("Right", 2);
            horizAlign.AddNamedValue("Stretch", 3);

            pManager.AddIntegerParameter("Vertical Alignment", "VA", "Vertical alignment", GH_ParamAccess.item);
            pManager[6].Optional = true;
            var vertAlign = (Param_Integer)pManager[6];
            vertAlign.AddNamedValue("Bottom", 0);
            vertAlign.AddNamedValue("Center", 1);
            vertAlign.AddNamedValue("Top", 2);
            vertAlign.AddNamedValue("Stretch", 3);
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object elem = null;
            string margin = "0";
            bool absolute = false;
            double width = 0;
            double height = 0;
            int horizAlignVal = 0;
            int vertAlignVal = 0;
            if (!DA.GetData("Elements to adjust", ref elem)) return;
            DA.GetData("Absolute Positioning", ref absolute);

            var ctrl = HUI_Util.GetUIElement<Control>(elem);
            if (ctrl == null) return;

            if (DA.GetData("Margin", ref margin))
            {
                var pad = paddingFromString(margin);
                if (ctrl is Panel p) p.Padding = pad;
                else WrapInPaddedPanel(ctrl, pad);
            }

            if (DA.GetData("Width", ref width)) ctrl.Width = (int)width;
            if (DA.GetData("Height", ref height)) ctrl.Height = (int)height;

            if (DA.GetData("Horizontal Alignment", ref horizAlignVal))
            {
                ApplyHorizontalAlignment(ctrl, horizAlignVal);
            }
            if (DA.GetData("Vertical Alignment", ref vertAlignVal))
            {
                ApplyVerticalAlignment(ctrl, vertAlignVal);
            }

            if (absolute)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "Absolute positioning is not supported in the Eto port — element kept in its layout container.");
            }
        }

        private static void WrapInPaddedPanel(Control ctrl, Padding pad)
        {
            // The Eto port stores padding directly on the control's parent stack item
            // when available. The original WPF used Margin on the control; we mimic by
            // setting the parent StackLayoutItem padding when reachable, otherwise no-op.
            var parent = ctrl.Parent;
            if (parent is StackLayout sl)
            {
                for (int i = 0; i < sl.Items.Count; i++)
                {
                    if (ReferenceEquals(sl.Items[i].Control, ctrl))
                    {
                        sl.Padding = pad;
                        return;
                    }
                }
            }
        }

        private static void ApplyHorizontalAlignment(Control ctrl, int v)
        {
            var alignment = v switch
            {
                0 => HorizontalAlignment.Left,
                1 => HorizontalAlignment.Center,
                2 => HorizontalAlignment.Right,
                _ => HorizontalAlignment.Stretch,
            };
            // Layout-item alignment is set on the parent stack's StackLayoutItem.
            if (ctrl.Parent is StackLayout sl)
            {
                for (int i = 0; i < sl.Items.Count; i++)
                {
                    if (ReferenceEquals(sl.Items[i].Control, ctrl))
                    {
                        var existing = sl.Items[i];
                        sl.Items[i] = new StackLayoutItem(ctrl, alignment, existing.Expand);
                        return;
                    }
                }
            }
        }

        private static void ApplyVerticalAlignment(Control ctrl, int v)
        {
            // Eto stack vertical alignment is controlled by the parent stack's
            // VerticalContentAlignment on a horizontal stack. For vertical stacks the
            // semantics don't map cleanly, so we apply only on horizontal parents.
            if (ctrl.Parent is StackLayout sl && sl.Orientation == Orientation.Horizontal)
            {
                sl.VerticalContentAlignment = v switch
                {
                    0 => VerticalAlignment.Bottom,
                    1 => VerticalAlignment.Center,
                    2 => VerticalAlignment.Top,
                    _ => VerticalAlignment.Stretch,
                };
            }
        }

        /// <summary>
        /// Parse the legacy "L,T,R,B" / single-number / Rhino-point string into an Eto
        /// Padding. Mirrors the original WPF logic.
        /// </summary>
        private Padding paddingFromString(string margin)
        {
            if (margin.Contains("{"))
            {
                var vals = margin.Split(",{}".ToCharArray());
                List<double> margins = new();
                foreach (var v in vals)
                {
                    if (double.TryParse(v, out var tempV)) margins.Add(tempV);
                }
                if (margins.Count >= 2) return new Padding((int)margins[0], (int)margins[1], 0, 0);
            }
            else if (margin.Contains(","))
            {
                var vals = margin.Split(',');
                if (vals.Length == 4)
                {
                    var nums = new int[4];
                    for (int i = 0; i < 4; i++) { double.TryParse(vals[i], out var d); nums[i] = (int)d; }
                    return new Padding(nums[0], nums[1], nums[2], nums[3]);
                }
            }
            else if (double.TryParse(margin, out var single))
            {
                return new Padding((int)single);
            }
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Trouble parsing the margin input. Try a single value, a Point, or A,B,C,D format.");
            return new Padding(0);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.AdjustPositioning;

        public override Guid ComponentGuid => new Guid("{0e1bdb06-2fe7-4fbc-b194-15227efdc8a7}");
    }
}
