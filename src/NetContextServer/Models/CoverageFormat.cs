using System.Text.Json.Serialization;

namespace NetContextServer.Models;

/// <summary>
/// Represents the supported formats for code coverage reports.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CoverageFormat
{
    /// <summary>
    /// Coverlet JSON format (default)
    /// </summary>
    CoverletJson,

    /// <summary>
    /// LCOV format (.info files)
    /// </summary>
    Lcov,

    /// <summary>
    /// Cobertura XML format
    /// </summary>
    CoberturaXml
} 