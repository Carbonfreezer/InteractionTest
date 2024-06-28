using System.IO;

namespace SteeringLogging
{
    /// <summary>
    /// Interface definition for loggabele entities in the action replay system.
    /// Every component that is relevant for logging should implement that interface. 
    /// </summary>
    public interface ILoggable
    {
        /// <summary>
        /// Reads in the state data from a reader.  (Reading from log).
        /// </summary>
        /// <param name="reader">Reader to read data from.</param>
        public void Serialize(BinaryReader reader);

        /// <summary>
        /// Writes in the state data to a writer. (Logging)
        /// </summary>
        /// <param name="writer">Writer to write data to.</param>
        public void Serialize(BinaryWriter writer);
    }
}