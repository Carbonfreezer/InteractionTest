namespace SteeringLogging
{
    /// <summary>
    /// Contains the information for the log files.
    /// </summary>
    public class LogFileInfo
    {
        /// <summary>
        /// Gets the description string to be displayed.
        /// </summary>
        public string DescriptiveString => $"{m_day:d2}.{m_month:d2}.{2000 + m_year}  {m_hour:d2}:{m_minute:d2}";

        /// <summary>
        /// The complete filename.
        /// </summary>
        public string m_completeName;

        /// <summary>
        /// Year - 2000 when created.
        /// </summary>
        public byte m_year;

        /// <summary>
        /// Month when created.
        /// </summary>
        public byte m_month;

        /// <summary>
        /// Day when created.
        /// </summary>
        public byte m_day;

        /// <summary>
        /// Hour when created.
        /// </summary>
        public byte m_hour;

        /// <summary>
        /// Minute when created.
        /// </summary>
        public byte m_minute;
    }
}