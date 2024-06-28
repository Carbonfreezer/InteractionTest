using SteeringLogging;
using System.IO;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Simple in game clock representation that gets synchronized over the network. Usable for real time effects.
/// </summary>
public class SyncedClock : NetworkVariableBase, ILoggable
{
    /// <summary>
    /// Call nack method when the time got changed.
    /// </summary>
    public delegate void TimeChanged();

    /// <summary>
    /// Event gets called when the time has changed (per second)
    /// </summary>
    public event TimeChanged OnTimeChanged;

    /// <summary>
    /// The hours of the day.
    /// </summary>
    public int Hours => (m_passedSecondsInDay / 3600);

    /// <summary>
    /// The minutes that have passed since the beginning.
    /// </summary>
    public int Minutes => (m_passedSecondsInDay % 3600) / 60;

    /// <summary>
    /// The additional seconds that have started since the beginning of the day.
    /// </summary>
    public int Seconds => (m_passedSecondsInDay % 60);

    /// <summary>
    /// General display text for hours and minutes passed.
    /// </summary>
    public string HoursMinutesText => $"{Hours:D2}:{Minutes:D2}";

    /// <summary>
    /// General display text for minutes and seconds passed.
    /// </summary>
    public string MinutesSecondsText => $"{Minutes:D2}:{Seconds:D2}";

    /// <summary>
    /// A scaling factor for real time.
    /// </summary>
    public float TimeScale { get; set; } = 1.0f;

    /// <summary>
    /// The seconds that have passed into the day.
    /// </summary>
    private int m_passedSecondsInDay;

    /// <summary>
    /// The time that has already passed from day.
    /// </summary>
    private float m_passedTimeInDay;

    /// <summary>
    /// Converts hours minutes and seconds into seconds. Easy for time comparison.
    /// </summary>
    /// <param name="hours">Hours of the day.</param>
    /// <param name="minutes">Minutes of the hour.</param>
    /// <param name="seconds">Additional seconds.</param>
    /// <returns>Total seconds.</returns>
    public static int ConvertToSeconds(int hours, int minutes, int seconds)
    {
        return 3600 * hours + 60 * minutes + seconds;
    }

    /// <summary>
    /// Starts the clock.
    /// </summary>
    /// <param name="hours">Hours for start.</param>
    /// <param name="minutes">Minutes for start.</param>
    public void StartClock(int hours, int minutes)
    {
        Debug.Assert(Platform.IsHost, "Should only be called on host.");
        m_passedSecondsInDay = 3600 * hours + 60 * minutes;
        m_passedTimeInDay = (float)m_passedSecondsInDay;
        OnTimeChanged?.Invoke();
        SetDirty(true);
    }

    /// <summary>
    /// Updates the time, should only be called on host and in mission mode.
    /// </summary>
    public void Update()
    {
        Debug.Assert(Platform.IsHost, "Should only be called on host.");
        m_passedTimeInDay += Time.deltaTime * TimeScale;
        int passedSeconds = (int)m_passedTimeInDay;
        if (passedSeconds == m_passedSecondsInDay)
            return;

        OnTimeChanged?.Invoke();

        m_passedSecondsInDay = passedSeconds;
        SetDirty(true);
    }

    #region Overrides of NetworkVariableBase

    /// <inheritdoc />
    public override void WriteDelta(FastBufferWriter writer)
    {
        WriteField(writer);
    }

    /// <inheritdoc />
    public override void WriteField(FastBufferWriter writer)
    {
        writer.WriteValueSafe(m_passedSecondsInDay);
    }

    /// <inheritdoc />
    public override void ReadField(FastBufferReader reader)
    {
        reader.ReadValueSafe(out m_passedSecondsInDay);

        OnTimeChanged?.Invoke();
    }

    /// <inheritdoc />
    public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
    {
        ReadField(reader);
    }

    #endregion

    #region Implementation of ILoggable

    /// <inheritdoc />
    public void Serialize(BinaryReader reader)
    {
        m_passedSecondsInDay = reader.ReadInt32();
        SetDirty(true);
        OnTimeChanged?.Invoke();
    }

    /// <inheritdoc />
    public void Serialize(BinaryWriter writer)
    {
        writer.Write(m_passedSecondsInDay);
    }

    #endregion
}