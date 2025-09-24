using Common;

namespace ProcessingModule
{
    /// <summary>
    /// Class containing logic for alarm processing.
    /// </summary>
    public class AlarmProcessor
    {
        /// <summary>
        /// Processes the alarm for analog point.
        /// </summary>
        /// <param name="eguValue">The EGU value of the point.</param>
        /// <param name="configItem">The configuration item.</param>
        /// <returns>The alarm indication.</returns>
        public AlarmType GetAlarmForAnalogPoint(double eguValue, IConfigItem configItem)
        {
            // prvo provjera razumnosti
            if (eguValue < configItem.EGU_Min || eguValue > configItem.EGU_Max)
            {
                return AlarmType.REASONABILITY_FAILURE;
            }

            // zatim provjera limita
            if (eguValue < configItem.LowLimit)
            {
                return AlarmType.LOW_ALARM;
            }
            else if (eguValue > configItem.HighLimit)
            {
                return AlarmType.HIGH_ALARM;
            }

            return AlarmType.NO_ALARM;
        }

        /// <summary>
        /// Processes the alarm for digital point.
        /// </summary>
        /// <param name="state">The digital point state.</param>
        /// <param name="configItem">The configuration item.</param>
        /// <returns>The alarm indication.</returns>
        public AlarmType GetAlarmForDigitalPoint(ushort state, IConfigItem configItem)
        {
            // Nominalno stanje je definisano u configItem.DefaultValue
            // Ako je trenutna vrednost različita → AbnormalValue alarm
            if (state != configItem.DefaultValue)
            {
                return AlarmType.ABNORMAL_VALUE;
            }

            return AlarmType.NO_ALARM;
        }
    }
}
