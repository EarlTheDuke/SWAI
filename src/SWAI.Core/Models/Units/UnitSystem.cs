namespace SWAI.Core.Models.Units;

/// <summary>
/// Supported unit systems for dimensions
/// </summary>
public enum UnitSystem
{
    Inches,
    Millimeters,
    Centimeters,
    Meters,
    Feet
}

/// <summary>
/// Unit conversion utilities
/// </summary>
public static class UnitConverter
{
    /// <summary>
    /// Conversion factors to meters (base unit)
    /// </summary>
    private static readonly Dictionary<UnitSystem, double> ToMeters = new()
    {
        { UnitSystem.Meters, 1.0 },
        { UnitSystem.Millimeters, 0.001 },
        { UnitSystem.Centimeters, 0.01 },
        { UnitSystem.Inches, 0.0254 },
        { UnitSystem.Feet, 0.3048 }
    };

    /// <summary>
    /// Convert a value from one unit to another
    /// </summary>
    public static double Convert(double value, UnitSystem from, UnitSystem to)
    {
        if (from == to) return value;
        
        // Convert to meters first, then to target unit
        var inMeters = value * ToMeters[from];
        return inMeters / ToMeters[to];
    }

    /// <summary>
    /// Convert to meters (SolidWorks internal unit)
    /// </summary>
    public static double ToMetersValue(double value, UnitSystem from)
    {
        return value * ToMeters[from];
    }

    /// <summary>
    /// Convert from meters to specified unit
    /// </summary>
    public static double FromMetersValue(double meters, UnitSystem to)
    {
        return meters / ToMeters[to];
    }

    /// <summary>
    /// Get the unit abbreviation for display
    /// </summary>
    public static string GetAbbreviation(UnitSystem unit) => unit switch
    {
        UnitSystem.Inches => "in",
        UnitSystem.Millimeters => "mm",
        UnitSystem.Centimeters => "cm",
        UnitSystem.Meters => "m",
        UnitSystem.Feet => "ft",
        _ => throw new ArgumentOutOfRangeException(nameof(unit))
    };

    /// <summary>
    /// Parse unit from string
    /// </summary>
    public static UnitSystem? ParseUnit(string text)
    {
        var normalized = text.ToLowerInvariant().Trim();
        
        return normalized switch
        {
            "in" or "inch" or "inches" or "\"" => UnitSystem.Inches,
            "mm" or "millimeter" or "millimeters" => UnitSystem.Millimeters,
            "cm" or "centimeter" or "centimeters" => UnitSystem.Centimeters,
            "m" or "meter" or "meters" => UnitSystem.Meters,
            "ft" or "foot" or "feet" or "'" => UnitSystem.Feet,
            _ => null
        };
    }
}
