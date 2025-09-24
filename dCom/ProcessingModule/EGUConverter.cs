using System;

namespace ProcessingModule
{
    /// <summary>
    /// Class containing logic for engineering unit conversion.
    /// </summary>
    public class EGUConverter
    {
        /// <summary>
        /// Converts the point value from raw to EGU form.
        /// </summary>
        /// <param name="scalingFactor">The scaling factor (A).</param>
        /// <param name="deviation">The deviation (B).</param>
        /// <param name="rawValue">The raw value.</param>
        /// <returns>The value in engineering units.</returns>
        public double ConvertToEGU(double scalingFactor, double deviation, ushort rawValue)
        {
            return scalingFactor * rawValue + deviation;
        }

        /// <summary>
        /// Converts the point value from EGU to raw form.
        /// </summary>
        /// <param name="scalingFactor">The scaling factor (A).</param>
        /// <param name="deviation">The deviation (B).</param>
        /// <param name="eguValue">The EGU value.</param>
        /// <returns>The raw value.</returns>
        public ushort ConvertToRaw(double scalingFactor, double deviation, double eguValue)
        {
            if (scalingFactor == 0) throw new ArgumentException("Scaling factor A cannot be zero.");

            double raw = (eguValue - deviation) / scalingFactor;

            if (raw < 0) raw = 0;
            if (raw > ushort.MaxValue) raw = ushort.MaxValue;

            return (ushort)raw;
        }
    }
}
