using UnityEngine;

/// <summary>
/// Contains a lighter version for Transformation that may be used for
/// coordinate system calculations
/// </summary>
public class TransformLight
{
    public Vector3 m_position;
    public Quaternion m_rotation;

    public TransformLight(Vector3 position, Quaternion rotation)
    {
        m_position = position;
        m_rotation = rotation;
    }

    /// <summary>
    /// Concatenation of coordinate systems
    /// </summary>
    /// <param name="left">First coordinate</param>
    /// <param name="right">Second coordinate</param>
    /// <returns>Returns product</returns>
    public static TransformLight operator *(TransformLight left, TransformLight right)
    {
        TransformLight result = new TransformLight(left.m_position + left.m_rotation * right.m_position,
            left.m_rotation * right.m_rotation);
        return result;
    }

    /// <summary>
    /// Computes the inverse of the transform without modifying it.
    /// </summary>
    /// <returns>Inverse</returns>
    public TransformLight Inverse()
    {
        Quaternion inverseRot = Quaternion.Inverse(m_rotation);
        TransformLight result = new TransformLight(-(inverseRot * m_position), inverseRot);

        return result;
    }

    /// <summary>
    /// Gets the transform light from world coordinates.
    /// </summary>
    /// <param name="trans">transform to get transform light from</param>
    public static explicit operator TransformLight(Transform trans)
    {
        return new TransformLight(trans.position, trans.rotation);
    }

    /// <summary>
    /// Transforms a position.
    /// </summary>
    /// <param name="position">Position to transform</param>
    /// <returns>Transformed position.</returns>
    public Vector3 TransformPosition(Vector3 position)
    {
        return m_position + m_rotation * position;
    }

    /// <summary>
    /// Transforms a direction.
    /// </summary>
    /// <param name="direction">Direction to transform</param>
    /// <returns>Transformed direction.</returns>
    public Vector3 TransformDirection(Vector3 direction)
    {
        return m_rotation * direction;
    }
}