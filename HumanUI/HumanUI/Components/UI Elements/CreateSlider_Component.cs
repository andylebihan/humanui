using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using ToolStripDropDown = System.Windows.Forms.ToolStripDropDown;

namespace HumanUI.Components.UI_Elements
{
    /// <summary>
    /// Eto Slider extended with floating-point Value semantics so we can drive an
    /// integer-backed Eto.Forms.Slider from the typical GH_NumberSlider range.
    /// Resolution is fixed at 1000 ticks across the float range -- enough resolution for
    /// user-facing UI without needing per-component scale juggling.
    /// </summary>
    public sealed class HUI_FloatSlider : Slider
    {
        public const int Resolution = 1000;

        public double FloatMin { get; private set; }
        public double FloatMax { get; private set; }
        public int DecimalPlaces { get; set; }
        public bool IntegerSlider { get; set; }

        public HUI_FloatSlider()
        {
            MinValue = 0;
            MaxValue = Resolution;
            TickFrequency = 1;
        }

        public void Configure(double min, double max, double value, int decimalPlaces, bool integerSlider)
        {
            FloatMin = min;
            FloatMax = max;
            DecimalPlaces = decimalPlaces;
            IntegerSlider = integerSlider;
            MinValue = 0;
            MaxValue = integerSlider ? Math.Max(1, (int)Math.Round(max - min)) : Resolution;
            FloatValue = value;
        }

        public double FloatValue
        {
            get
            {
                if (MaxValue == MinValue) return FloatMin;
                double t = (double)(Value - MinValue) / (MaxValue - MinValue);
                return FloatMin + t * (FloatMax - FloatMin);
            }
            set
            {
                double clamped = Math.Max(FloatMin, Math.Min(FloatMax, value));
                if (FloatMax == FloatMin) Value = MinValue;
                else
                {
                    double t = (clamped - FloatMin) / (FloatMax - FloatMin);
                    Value = (int)Math.Round(MinValue + t * (MaxValue - MinValue));
                }
            }
        }

        public string FormatValue() => IntegerSlider
            ? FloatValue.ToString("0", CultureInfo.InvariantCulture)
            : FloatValue.ToString("F" + DecimalPlaces, CultureInfo.InvariantCulture);
    }

    public class CreateSlider_Component : GH_Component
    {
        private bool showTicks;
        private bool showTooltip;
        private bool showValueReadout = true;
        private bool showBounds;
        private bool showLabel = true;

        public CreateSlider_Component()
            : base("Create Slider", "Slider",
                "Create a slider with a label and a value readout.",
                "Human UI", "UI Elements")
        {
        }

        public void Menu_ShowLabelClicked(object sender, EventArgs e)
        {
            RecordUndoEvent("Show Label Toggle");
            showLabel = !showLabel;
            ExpireSolution(true);
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Slider", "Sl", "The slider(s) to add to the window.", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Snap Value", "Sn", "An optional value to round/snap slider to. This overrides the native settings on the GH slider.", GH_ParamAccess.list);
            pManager[1].Optional = true;
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Sliders", "S", "The Slider UI elements. Use in conjunction with an \"Add Elements\" component.", GH_ParamAccess.list);
        }

        int sliderIndex = 0;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (DA.Iteration == 0) sliderIndex = 0;

            var attachedSliders = new List<GH_NumberSlider>();
            try
            {
                attachedSliders = Params.Input[0].Sources.Cast<GH_NumberSlider>().ToList();
            }
            catch
            {
                // Caller passed slider objects via wrappers (e.g. Metahopper output) rather
                // than wiring the slider directly.
                DA.GetDataTree(0, out Grasshopper.Kernel.Data.GH_Structure<IGH_Goo> wrapped);
                foreach (IGH_Goo goo in wrapped)
                {
                    if (goo is GH_ObjectWrapper w && w.Value is GH_NumberSlider gs)
                        attachedSliders.Add(gs);
                }
            }

            var sliderPanels = new List<UIElement_Goo>();
            foreach (var sl in attachedSliders)
            {
                var name = string.IsNullOrWhiteSpace(sl.NickName) ? string.Empty : sl.ImpliedNickName;
                var panel = MakeSlider(sl);
                sliderPanels.Add(new UIElement_Goo(panel, name, InstanceGuid, sliderIndex));
                sliderIndex++;
            }

            DA.SetDataList("Sliders", sliderPanels);
        }

        private Control MakeSlider(GH_NumberSlider sl)
        {
            string name = sl.ImpliedNickName;
            bool integer = sl.Slider.Type == Grasshopper.GUI.Base.GH_SliderAccuracy.Integer;
            int decPlaces = sl.Slider.DecimalPlaces;

            var slider = new HUI_FloatSlider();
            slider.Configure((double)sl.Slider.Minimum, (double)sl.Slider.Maximum, (double)sl.Slider.Value, decPlaces, integer);
            if (showTicks)
                slider.SnapToTick = true;

            var stack = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Spacing = 4,
                MinimumSize = new Size(200, 0),
                ID = "GH_Slider",
            };

            if (showLabel && !string.IsNullOrWhiteSpace(name))
                stack.Items.Add(new StackLayoutItem(new Label { Text = name }, VerticalAlignment.Center));

            stack.Items.Add(new StackLayoutItem(slider, VerticalAlignment.Center, expand: true));

            if (showValueReadout)
            {
                var readout = new Label { Text = slider.FormatValue() };
                slider.ValueChanged += (s, e) => readout.Text = slider.FormatValue();
                stack.Items.Add(new StackLayoutItem(readout, VerticalAlignment.Center));
            }

            return stack;
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreateSlider;

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            GH_DocumentObject.Menu_AppendItem(menu, "Enable Ticks", menu_enableTicks, true, showTicks)
                .ToolTipText = "Snap the slider to integer ticks.";
            GH_DocumentObject.Menu_AppendItem(menu, "Enable Tooltip", menu_enableTooltip, true, showTooltip)
                .ToolTipText = "Display a tooltip above the slider displaying the value.";
            GH_DocumentObject.Menu_AppendItem(menu, "Enable Value Label", menu_enableValueLabel, true, showValueReadout)
                .ToolTipText = "Display a label to the right of the slider showing its current value.";
            GH_DocumentObject.Menu_AppendItem(menu, "Show Slider Limits", menu_showBounds, true, showBounds)
                .ToolTipText = "Display the min/max values below the slider.";
            GH_DocumentObject.Menu_AppendItem(menu, "Show Label", Menu_ShowLabelClicked, true, showLabel)
                .ToolTipText = "When checked, the UI Element will include the supplied label.";
        }

        private void menu_showBounds(object sender, EventArgs e) { RecordUndoEvent("Toggle Slider Bounds Display"); showBounds = !showBounds; ExpireSolution(true); }
        private void menu_enableValueLabel(object sender, EventArgs e) { RecordUndoEvent("Toggle Slider Value Label"); showValueReadout = !showValueReadout; ExpireSolution(true); }
        private void menu_enableTooltip(object sender, EventArgs e) { RecordUndoEvent("Toggle Slider Value Tooltip"); showTooltip = !showTooltip; ExpireSolution(true); }
        private void menu_enableTicks(object sender, EventArgs e) { RecordUndoEvent("Toggle Slider Tick Display"); showTicks = !showTicks; ExpireSolution(true); }

        public override Guid ComponentGuid => new Guid("{C77ACC8A-FE64-43F0-9485-D23744F6152E}");

        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean("ShowTicks", showTicks);
            writer.SetBoolean("ShowTooltip", showTooltip);
            writer.SetBoolean("ShowValLabel", showValueReadout);
            writer.SetBoolean("ShowBounds", showBounds);
            writer.SetBoolean("showLabel", showLabel);
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            reader.TryGetBoolean("ShowTicks", ref showTicks);
            reader.TryGetBoolean("ShowTooltip", ref showTooltip);
            reader.TryGetBoolean("ShowValLabel", ref showValueReadout);
            reader.TryGetBoolean("ShowBounds", ref showBounds);
            reader.TryGetBoolean("showLabel", ref showLabel);
            return base.Read(reader);
        }
    }
}
