using System.Text.RegularExpressions;

namespace SWAI.Core.Models.Units;

/// <summary>
/// Represents a dimensional value with units.
/// Immutable value type for safe handling of measurements.
/// </summary>
public readonly partial struct Dimension : IEquatable<Dimension>, IComparable<Dimension>
{
    /// <summary>
    /// The numeric value in the specified unit
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// The unit system for this dimension
    /// </summary>
    public UnitSystem Unit { get; }

    /// <summary>
    /// Get the value in meters (SolidWorks internal unit)
    /// </summary>
    public double Meters => UnitConverter.ToMetersValue(Value, Unit);

    public Dimension(double value, UnitSystem unit)
    {
        Value = value;
        Unit = unit;
    }

    /// <summary>
    /// Create dimension in inches
    /// </summary>
    public static Dimension Inches(double value) => new(value, UnitSystem.Inches);

    /// <summary>
    /// Create dimension in millimeters
    /// </summary>
    public static Dimension Millimeters(double value) => new(value, UnitSystem.Millimeters);

    /// <summary>
    /// Create dimension in meters
    /// </summary>
    public static Dimension MetersValue(double value) => new(value, UnitSystem.Meters);

    /// <summary>
    /// Zero dimension
    /// </summary>
    public static Dimension Zero => new(0, UnitSystem.Inches);

    /// <summary>
    /// Convert this dimension to another unit
    /// </summary>
    public Dimension ConvertTo(UnitSystem targetUnit)
    {
        var converted = UnitConverter.Convert(Value, Unit, targetUnit);
        return new Dimension(converted, targetUnit);
    }

    /// <summary>
    /// Parse a dimension from natural language string.
    /// Supports formats like: "36 inches", "36\"", "36 in", "3/4 inch", "0.75in", "500mm"
    /// </summary>
    public static Dimension Parse(string input, UnitSystem defaultUnit = UnitSystem.Inches)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Dimension string cannot be empty", nameof(input));

        var normalized = input.Trim().ToLowerInvariant();

        // Try to parse fractional format first (e.g., "3/4 inch", "1 1/2 inches")
        var fractionMatch = FractionRegex().Match(normalized);
        if (fractionMatch.Success)
        {
            var value = ParseFraction(fractionMatch);
            var unitStr = fractionMatch.Groups["unit"].Value;
            var unit = UnitConverter.ParseUnit(unitStr) ?? defaultUnit;
            return new Dimension(value, unit);
        }

        // Try decimal format (e.g., "36 inches", "0.75in", "500mm")
        var decimalMatch = DecimalRegex().Match(normalized);
        if (decimalMatch.Success)
        {
            var value = double.Parse(decimalMatch.Groups["value"].Value);
            var unitStr = decimalMatch.Groups["unit"].Value;
            var unit = string.IsNullOrEmpty(unitStr) 
                ? defaultUnit 
                : UnitConverter.ParseUnit(unitStr) ?? defaultUnit;
            return new Dimension(value, unit);
        }

        // Try just a number
        if (double.TryParse(normalized, out var numericValue))
        {
            return new Dimension(numericValue, defaultUnit);
        }

        throw new FormatException($"Unable to parse dimension: '{input}'");
    }

    /// <summary>
    /// Try to parse a dimension, returns null if parsing fails
    /// </summary>
    public static Dimension? TryParse(string input, UnitSystem defaultUnit = UnitSystem.Inches)
    {
        try
        {
            return Parse(input, defaultUnit);
        }
        catch
        {
            return null;
        }
    }

    private static double ParseFraction(Match match)
    {
        double whole = 0;
        if (match.Groups["whole"].Success && !string.IsNullOrEmpty(match.Groups["whole"].Value))
        {
            whole = double.Parse(match.Groups["whole"].Value);
        }

        var numerator = double.Parse(match.Groups["num"].Value);
        var denominator = double.Parse(match.Groups["den"].Value);

        return whole + (numerator / denominator);
    }

    // Regex patterns for parsing
    [GeneratedRegex(@"^(?<whole>\d+\s+)?(?<num>\d+)/(?<den>\d+)\s*(?<unit>[a-z""']+)?$")]
    private static partial Regex FractionRegex();

    [GeneratedRegex(@"^(?<value>-?\d+\.?\d*)\s*(?<unit>[a-z""']+)?$")]
    private static partial Regex DecimalRegex();

    // Operators
    public static Dimension operator +(Dimension a, Dimension b)
    {
        var bConverted = b.ConvertTo(a.Unit);
        return new Dimension(a.Value + bConverted.Value, a.Unit);
    }

    public static Dimension operator -(Dimension a, Dimension b)
    {
        var bConverted = b.ConvertTo(a.Unit);
        return new Dimension(a.Value - bConverted.Value, a.Unit);
    }

    public static Dimension operator *(Dimension a, double scalar)
    {
        return new Dimension(a.Value * scalar, a.Unit);
    }

    public static Dimension operator /(Dimension a, double scalar)
    {
        return new Dimension(a.Value / scalar, a.Unit);
    }

    public static bool operator ==(Dimension a, Dimension b) => a.Equals(b);
    public static bool operator !=(Dimension a, Dimension b) => !a.Equals(b);
    public static bool operator <(Dimension a, Dimension b) => a.CompareTo(b) < 0;
    public static bool operator >(Dimension a, Dimension b) => a.CompareTo(b) > 0;
    public static bool operator <=(Dimension a, Dimension b) => a.CompareTo(b) <= 0;
    public static bool operator >=(Dimension a, Dimension b) => a.CompareTo(b) >= 0;

    // Equality and comparison (compare in meters for consistency)
    public bool Equals(Dimension other)
    {
        const double tolerance = 1e-9;
        return Math.Abs(Meters - other.Meters) < tolerance;
    }

    public override bool Equals(object? obj) => obj is Dimension other && Equals(other);

    public override int GetHashCode() => Meters.GetHashCode();

    public int CompareTo(Dimension other) => Meters.CompareTo(other.Meters);

    public override string ToString() => $"{Value:G} {UnitConverter.GetAbbreviation(Unit)}";

    public string ToString(string format) => $"{Value.ToString(format)} {UnitConverter.GetAbbreviation(Unit)}";
}
