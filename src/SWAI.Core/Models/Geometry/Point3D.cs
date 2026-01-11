using SWAI.Core.Models.Units;

namespace SWAI.Core.Models.Geometry;

/// <summary>
/// Represents a 3D point in space
/// </summary>
public readonly struct Point3D : IEquatable<Point3D>
{
    public Dimension X { get; }
    public Dimension Y { get; }
    public Dimension Z { get; }

    public Point3D(Dimension x, Dimension y, Dimension z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public Point3D(double x, double y, double z, UnitSystem unit = UnitSystem.Inches)
    {
        X = new Dimension(x, unit);
        Y = new Dimension(y, unit);
        Z = new Dimension(z, unit);
    }

    /// <summary>
    /// Origin point (0, 0, 0)
    /// </summary>
    public static Point3D Origin => new(0, 0, 0);

    /// <summary>
    /// Get coordinates in meters for SolidWorks API
    /// </summary>
    public (double X, double Y, double Z) ToMeters() => (X.Meters, Y.Meters, Z.Meters);

    /// <summary>
    /// Convert to another unit system
    /// </summary>
    public Point3D ConvertTo(UnitSystem unit) => new(
        X.ConvertTo(unit),
        Y.ConvertTo(unit),
        Z.ConvertTo(unit)
    );

    /// <summary>
    /// Calculate distance to another point
    /// </summary>
    public Dimension DistanceTo(Point3D other)
    {
        var dx = (X - other.X).Meters;
        var dy = (Y - other.Y).Meters;
        var dz = (Z - other.Z).Meters;
        var distance = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        return Dimension.MetersValue(distance);
    }

    public static Point3D operator +(Point3D a, Vector3D v) => new(
        a.X + v.X,
        a.Y + v.Y,
        a.Z + v.Z
    );

    public static Vector3D operator -(Point3D a, Point3D b) => new(
        a.X - b.X,
        a.Y - b.Y,
        a.Z - b.Z
    );

    public bool Equals(Point3D other)
    {
        return X == other.X && Y == other.Y && Z == other.Z;
    }

    public override bool Equals(object? obj) => obj is Point3D other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    public static bool operator ==(Point3D left, Point3D right) => left.Equals(right);

    public static bool operator !=(Point3D left, Point3D right) => !left.Equals(right);

    public override string ToString() => $"({X}, {Y}, {Z})";
}

/// <summary>
/// Represents a 3D vector
/// </summary>
public readonly struct Vector3D : IEquatable<Vector3D>
{
    public Dimension X { get; }
    public Dimension Y { get; }
    public Dimension Z { get; }

    public Vector3D(Dimension x, Dimension y, Dimension z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public Vector3D(double x, double y, double z, UnitSystem unit = UnitSystem.Inches)
    {
        X = new Dimension(x, unit);
        Y = new Dimension(y, unit);
        Z = new Dimension(z, unit);
    }

    /// <summary>
    /// Unit vectors
    /// </summary>
    public static Vector3D UnitX => new(1, 0, 0);
    public static Vector3D UnitY => new(0, 1, 0);
    public static Vector3D UnitZ => new(0, 0, 1);
    public static Vector3D Zero => new(0, 0, 0);

    /// <summary>
    /// Calculate the magnitude of this vector
    /// </summary>
    public Dimension Magnitude
    {
        get
        {
            var mag = Math.Sqrt(
                X.Meters * X.Meters +
                Y.Meters * Y.Meters +
                Z.Meters * Z.Meters
            );
            return Dimension.MetersValue(mag);
        }
    }

    public static Vector3D operator +(Vector3D a, Vector3D b) => new(
        a.X + b.X,
        a.Y + b.Y,
        a.Z + b.Z
    );

    public static Vector3D operator -(Vector3D a, Vector3D b) => new(
        a.X - b.X,
        a.Y - b.Y,
        a.Z - b.Z
    );

    public static Vector3D operator *(Vector3D v, double scalar) => new(
        v.X * scalar,
        v.Y * scalar,
        v.Z * scalar
    );

    public bool Equals(Vector3D other) => X == other.X && Y == other.Y && Z == other.Z;

    public override bool Equals(object? obj) => obj is Vector3D other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    public static bool operator ==(Vector3D left, Vector3D right) => left.Equals(right);

    public static bool operator !=(Vector3D left, Vector3D right) => !left.Equals(right);

    public override string ToString() => $"<{X}, {Y}, {Z}>";
}
