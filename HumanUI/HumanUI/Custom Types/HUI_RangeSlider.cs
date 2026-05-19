using System;
using Eto.Drawing;
using Eto.Forms;
using HumanUI.Components.UI_Elements;

namespace HumanUI
{
    /// <summary>
    /// Eto range-slider composite: two HUI_FloatSliders, one for the lower bound
    /// and one for the upper bound, kept consistent so the lower never crosses
    /// above the upper. Replaces MahApps' two-thumb RangeSlider. The control
    /// reports its current bounds via LowerValue / UpperValue and fires
    /// RangeChanged whenever either slider moves.
    /// </summary>
    public class HUI_RangeSlider : Panel
    {
        private readonly HUI_FloatSlider _lower;
        private readonly HUI_FloatSlider _upper;
        private bool _suppress;

        public event EventHandler RangeChanged;

        public double FloatMinimum => _lower.FloatMin;
        public double FloatMaximum => _lower.FloatMax;
        public double LowerValue
        {
            get => _lower.FloatValue;
            set
            {
                _suppress = true;
                _lower.FloatValue = Math.Min(value, _upper.FloatValue);
                _suppress = false;
            }
        }
        public double UpperValue
        {
            get => _upper.FloatValue;
            set
            {
                _suppress = true;
                _upper.FloatValue = Math.Max(value, _lower.FloatValue);
                _suppress = false;
            }
        }
        public int DecimalPlaces
        {
            get => _lower.DecimalPlaces;
            set { _lower.DecimalPlaces = value; _upper.DecimalPlaces = value; }
        }
        public bool IntegerSlider
        {
            get => _lower.IntegerSlider;
            set { _lower.IntegerSlider = value; _upper.IntegerSlider = value; }
        }

        public HUI_RangeSlider(double min, double max, double lower, double upper)
        {
            _lower = new HUI_FloatSlider();
            _upper = new HUI_FloatSlider();
            _lower.Configure(min, max, lower, 3, false);
            _upper.Configure(min, max, upper, 3, false);

            _lower.ValueChanged += OnLowerChanged;
            _upper.ValueChanged += OnUpperChanged;

            var stack = new StackLayout
            {
                Orientation = Orientation.Vertical,
                Spacing = 2,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
            };
            stack.Items.Add(new StackLayoutItem(_lower, HorizontalAlignment.Stretch, true));
            stack.Items.Add(new StackLayoutItem(_upper, HorizontalAlignment.Stretch, true));

            Content = stack;
            Padding = new Padding(2);
            ID = "GH_RangeSlider";
        }

        private void OnLowerChanged(object sender, EventArgs e)
        {
            if (_suppress) return;
            if (_lower.FloatValue > _upper.FloatValue)
            {
                _suppress = true;
                _upper.FloatValue = _lower.FloatValue;
                _suppress = false;
            }
            RangeChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnUpperChanged(object sender, EventArgs e)
        {
            if (_suppress) return;
            if (_upper.FloatValue < _lower.FloatValue)
            {
                _suppress = true;
                _lower.FloatValue = _upper.FloatValue;
                _suppress = false;
            }
            RangeChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
