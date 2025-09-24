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
        public double ConvertToEGU(double A, double B, ushort rawValue)
        {
            return A * rawValue + B;
        }

        /// <summary>
        /// Converts the point value from EGU to raw form.
        /// </summary>
        /// <param name="scalingFactor">The scaling factor (A).</param>
        /// <param name="deviation">The deviation (B).</param>
        /// <param name="eguValue">The EGU value.</param>
        /// <returns>The raw value.</returns>
        public ushort ConvertToRaw(double A, double B, double eguValue)
        {
            return (ushort)((eguValue - B) / A);
        }
    }
}
