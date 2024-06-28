using SteeringLogging;
using System.IO;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// A helper class to synchronize coordinate systems across networks in combination with a common anchor coordinate system.
/// </summary>
public class SyncedCoordinateSystem : NetworkVariableBase, ILoggable
{
    /// <summary>
    /// The coordinates we are anchored to. 
    /// </summary>
    private Transform m_anchorCoordinates;

    /// <summary>
    /// The quaternion we have.
    /// </summary>
    private Quaternion m_containedOrientation;

    /// <summary>
    /// The position we have.
    /// </summary>
    private Vector3 m_containedPosition;

    /// <summary>
    /// The scale we have.
    /// </summary>
    private Vector3 m_containedScale;

    /// <summary>
    /// The coordinate system we want o manipulate / read from.
    /// </summary>
    private Transform m_referredCoordinates;

    /// <summary>
    /// Flags the dirty status (bit 0: orientation, bit 1: position, bit 2 scale).
    /// </summary>
    private ushort m_dirtyStatus;

    /// <summary>
    /// Flags the system as dirty because of reading to transmit the positions.
    /// </summary>
    private bool m_readDirty;

    #region Tolerance values.

    /// <summary>
    /// The value we tolerate till we generate an update message on the server,
    /// </summary>
    private const double PositionServerTolerance = 0.005f;

    /// <summary>
    /// The tolerance when to update the rotation an transmit the  data.
    /// </summary>
    private const double QuaternionServerAngleTolerance = 1.5;

    /// <summary>
    /// The tolerance we take on the server side for the scaling.
    /// </summary>
    private const double ScaleServerTolerance = 0.01;

    /// <summary>
    /// The distance when we do a hard set on the client side.
    /// </summary>
    private const double PositionClientTolerance = 0.1;

    /// <summary>
    /// The damping for the adjustment of values.
    /// </summary>
    private const float DampingConstant = 0.8f;

    /// <summary>
    /// The constant we use for scaling tolerance.
    /// </summary>
    private const double ScaleClientTolerance = 0.05;

    /// <summary>
    /// The rotation tolerance we have on the client.
    /// </summary>
    private const double RotationClientTolerance = 25.0;

    #endregion

    /// <summary>
    /// Sets the momentary coordinate system from the ourside we want to read from / want to influence.
    /// Has to be set on awake.
    /// </summary>
    public Transform CurrentCoordinateSystem
    {
        set
        {
            m_anchorCoordinates = Utility.GetAnchorTransform(value);
            m_referredCoordinates = value;
        }
    }

    /// <summary>
    /// Has to be called from the outside every frame both on host and on client.
    /// </summary>
    public void SyncCoordinates()
    {
#pragma warning disable CS0162
        if (Platform.IsHost)
            ServerCheckCoordinateSystem();
        else
            ClientCheckCoordinateSystem();
#pragma warning restore CS0162
    }

    /// <summary>
    /// Updates the coordinate system if we are the client.
    /// </summary>
    private void ClientCheckCoordinateSystem()
    {
        Vector3 worldTarget = m_anchorCoordinates.TransformPoint(m_containedPosition);
        Quaternion quaternionTarget = m_anchorCoordinates.rotation * m_containedOrientation;

        if ((m_referredCoordinates.localScale - m_containedScale).magnitude > ScaleClientTolerance)
            m_referredCoordinates.localScale = m_containedScale;
        else
            m_referredCoordinates.localScale =
                Vector3.Lerp(m_containedScale, m_referredCoordinates.localScale, DampingConstant);

        if ((m_referredCoordinates.position - worldTarget).magnitude > PositionClientTolerance)
            m_referredCoordinates.position = worldTarget;
        else
            m_referredCoordinates.position = Vector3.Lerp(worldTarget, m_referredCoordinates.position, DampingConstant);

        if (Quaternion.Angle(quaternionTarget, m_referredCoordinates.rotation) > RotationClientTolerance)
            m_referredCoordinates.rotation = quaternionTarget;
        else
            m_referredCoordinates.rotation =
                Quaternion.Lerp(quaternionTarget, m_referredCoordinates.rotation, DampingConstant);
    }

    /// <summary>
    /// Does the check of the coordinate system on the server side.
    /// </summary>
    private void ServerCheckCoordinateSystem()
    {
        Vector3 localPosition = m_anchorCoordinates.InverseTransformPoint(m_referredCoordinates.position);
        Quaternion localOrientation = Quaternion.Inverse(m_anchorCoordinates.rotation) * m_referredCoordinates.rotation;
        Vector3 localScale = m_referredCoordinates.localScale;

        if (m_readDirty || ((localPosition - m_containedPosition).magnitude > PositionServerTolerance))
        {
            m_containedPosition = localPosition;
            m_dirtyStatus |= 2;
            SetDirty(true);
        }

        if (m_readDirty || (Quaternion.Angle(localOrientation, m_containedOrientation) > QuaternionServerAngleTolerance))
        {
            m_containedOrientation = localOrientation;
            m_dirtyStatus |= 1;
            SetDirty(true);
        }

        if (m_readDirty || ((localScale - m_containedScale).magnitude > ScaleServerTolerance))
        {
            m_containedScale = localScale;
            m_dirtyStatus |= 4;
            SetDirty(true);
        }

        m_readDirty = false;
    }

    #region Overrides of NetworkVariableBase

    /// <inheritdoc/>
    public override void ResetDirty()
    {
        base.ResetDirty();
        m_dirtyStatus = 0;
    }

    /// <inheritdoc />
    public override void WriteDelta(FastBufferWriter writer)
    {
        writer.WriteValueSafe(m_dirtyStatus);

        if ((m_dirtyStatus & 1) != 0)
            writer.WriteValueSafe(m_containedOrientation);
        if ((m_dirtyStatus & 2) != 0)
            writer.WriteValueSafe(m_containedPosition);
        if ((m_dirtyStatus & 4) != 0)
            writer.WriteValueSafe(m_containedScale);
    }

    /// <inheritdoc />
    public override void WriteField(FastBufferWriter writer)
    {
        writer.WriteValueSafe(m_containedOrientation);
        writer.WriteValueSafe(m_containedPosition);
        writer.WriteValueSafe(m_containedScale);
    }

    /// <inheritdoc />
    public override void ReadField(FastBufferReader reader)
    {
        reader.ReadValueSafe(out m_containedOrientation);
        reader.ReadValueSafe(out m_containedPosition);
        reader.ReadValueSafe(out m_containedScale);
    }

    /// <inheritdoc />
    public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
    {
        Debug.Assert(!keepDirtyDelta,
            "Tracing in the code this can happen only if the variable does not get manipulated by the server. This is not the case in our situation.");

        reader.ReadValueSafe(out m_dirtyStatus);

        if ((m_dirtyStatus & 1) != 0)
            reader.ReadValueSafe(out m_containedOrientation);
        if ((m_dirtyStatus & 2) != 0)
            reader.ReadValueSafe(out m_containedPosition);
        if ((m_dirtyStatus & 4) != 0)
            reader.ReadValueSafe(out m_containedScale);

        m_dirtyStatus = 0;
    }

    #endregion

    #region Implementation of ILoggable

    /// <inheritdoc />
    public void Serialize(BinaryReader reader)
    {
        m_containedOrientation.x = reader.ReadSingle();
        m_containedOrientation.y = reader.ReadSingle();
        m_containedOrientation.z = reader.ReadSingle();
        m_containedOrientation.w = reader.ReadSingle();

        m_containedPosition.x = reader.ReadSingle();
        m_containedPosition.y = reader.ReadSingle();
        m_containedPosition.z = reader.ReadSingle();

        m_containedScale.x = reader.ReadSingle();
        m_containedScale.y = reader.ReadSingle();
        m_containedScale.z = reader.ReadSingle();

        Vector3 worldTarget = m_anchorCoordinates.TransformPoint(m_containedPosition);
        Quaternion quaternionTarget = m_anchorCoordinates.rotation * m_containedOrientation;
        m_referredCoordinates.localScale = m_containedScale;
        m_referredCoordinates.position = worldTarget;
        m_referredCoordinates.rotation = quaternionTarget;

        m_readDirty = true;

    }

    /// <inheritdoc />
    public void Serialize(BinaryWriter writer)
    {
        writer.Write(m_containedOrientation.x);
        writer.Write(m_containedOrientation.y);
        writer.Write(m_containedOrientation.z);
        writer.Write(m_containedOrientation.w);

        writer.Write(m_containedPosition.x);
        writer.Write(m_containedPosition.y);
        writer.Write(m_containedPosition.z);

        writer.Write(m_containedScale.x);
        writer.Write(m_containedScale.y);
        writer.Write(m_containedScale.z);
    }

    #endregion
}