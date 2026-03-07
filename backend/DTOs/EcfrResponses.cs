using System.Text.Json.Serialization;

namespace EcfrAnalyzer.DTOs;

// GET /api/admin/v1/agencies
public record AgenciesResponse(
    [property: JsonPropertyName("agencies")] List<AgencyDto> Agencies
);

public record AgencyDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("short_name")] string? ShortName,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("cfr_references")] List<CfrReference>? CfrReferences,
    [property: JsonPropertyName("children")] List<AgencyDto>? Children
);

public record CfrReference(
    [property: JsonPropertyName("title")] int Title,
    [property: JsonPropertyName("chapter")] string? Chapter
);

// GET /api/versioner/v1/titles
public record TitlesResponse(
    [property: JsonPropertyName("titles")] List<TitleDto> Titles
);

public record TitleDto(
    [property: JsonPropertyName("number")] int Number,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("latest_amended_on")] string? LatestAmendedOn,
    [property: JsonPropertyName("up_to_date_as_of")] string? UpToDateAsOf,
    [property: JsonPropertyName("reserved")] bool Reserved
);

// GET /api/versioner/v1/versions/title-{n}.json
public record VersionsResponse(
    [property: JsonPropertyName("content_versions")] List<VersionDto>? ContentVersions
);

public record VersionDto(
    [property: JsonPropertyName("date")] string? Date,
    [property: JsonPropertyName("identifier")] string? Identifier,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("substantive")] bool Substantive,
    [property: JsonPropertyName("removed")] bool Removed
);
