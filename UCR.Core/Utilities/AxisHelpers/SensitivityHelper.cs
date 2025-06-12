using System;

namespace HidWizards.UCR.Core.Utilities.AxisHelpers
{
    public class SensitivityHelper
    {
        private double _scaleFactor;
        private double _axisRange;

        public int Percentage
        {
            get => _percentage;
            set
            {
                // Do NOT throw if percentage is not in range 0..100, other values are valid!
                _percentage = value;
                PrecalculateValues();
            }
        }

        private int _percentage;

        public bool IsLinear { get; set; }

        public SensitivityHelper()
        {
            PrecalculateValues();
        }

        private void PrecalculateValues()
        {
            _scaleFactor = _percentage / 100.0;
            _axisRange = Constants.AxisMaxValue - Constants.AxisMinValue;
        }

        public short ApplyRangeSensitivity(short value)
        {
            if (IsLinear)
            {
                return Functions.ClampAxisRange((int)Math.Round(value * _scaleFactor));
            }

            // The non-linear sensitivity is implemented as a power curve.
            // The exponent 'a' controls the shape of the curve, where a = Percentage / 100.0
            // - a > 1: Ease-in (low sensitivity near the center)
            // - a = 1: Linear (constant sensitivity, 1:1 response)
            // - 0 < a < 1: Ease-out (high sensitivity near the center)
            double exponent = _percentage / 100.0;

            // An exponent of 0 would max out the axis, and a negative exponent is not sensible here.
            // Return the original value for invalid exponents to prevent unexpected behavior.
            if (exponent <= 0)
            {
                return value;
            }

            // Map input value from its short range (e.g., -32768..32767) to a double from -1.0 to 1.0
            double val11 = (((double)value - Constants.AxisMinValue) / _axisRange) * 2.0 - 1.0;

            // Preserve the sign, as Math.Pow is applied to the absolute value.
            // This correctly handles non-integer exponents with negative bases (which would otherwise be NaN).
            var sign = Math.Sign(val11);
            var absVal = Math.Abs(val11);

            // Apply the power curve to the absolute value
            var curvedVal = Math.Pow(absVal, exponent);

            // Re-apply the sign to the result
            var result11 = curvedVal * sign;

            // Map the processed value from -1.0..1.0 back to the original short axis range
            var finalValue = (short)Math.Round(((result11 + 1.0) / 2.0) * _axisRange + Constants.AxisMinValue);

            return finalValue;
        }
    }
}