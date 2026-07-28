using System.Text.Json;
using System.Text.Json.Serialization;
using MongoDB.Bson;

namespace NexusTeam.DbSeeder.Models;

/// <summary>
/// Represents MongoDB collection configuration loaded from JSON files.
/// </summary>
public class MongoCollectionConfig
{
    /// <summary>
    /// Gets or sets the collection name.
    /// </summary>
    [JsonPropertyName("collectionName")]
    public string CollectionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON schema validator as a JsonElement.
    /// </summary>
    [JsonPropertyName("validator")]
    public JsonElement? Validator { get; set; }

    /// <summary>
    /// Gets or sets the validation level.
    /// </summary>
    [JsonPropertyName("validationLevel")]
    public string ValidationLevel { get; set; } = "moderate";

    /// <summary>
    /// Gets or sets the validation action.
    /// </summary>
    [JsonPropertyName("validationAction")]
    public string ValidationAction { get; set; } = "error";

    /// <summary>
    /// Gets or sets the list of indexes to create.
    /// </summary>
    [JsonPropertyName("indexes")]
    public List<MongoIndexConfig> Indexes { get; set; } = new();

    /// <summary>
    /// Converts validator JsonElement to BsonDocument.
    /// </summary>
    public BsonDocument? GetValidatorBsonDocument()
    {
        if (Validator == null || Validator.Value.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        var json = JsonSerializer.Serialize(Validator.Value);
        return BsonDocument.Parse(json);
    }
}

/// <summary>
/// Represents MongoDB index configuration.
/// </summary>
public class MongoIndexConfig
{
    /// <summary>
    /// Gets or sets the index name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the index key specification as a JsonElement.
    /// </summary>
    [JsonPropertyName("key")]
    public JsonElement Key { get; set; }

    /// <summary>
    /// Gets or sets whether the index is unique.
    /// </summary>
    [JsonPropertyName("unique")]
    public bool Unique { get; set; }

    /// <summary>
    /// Converts key JsonElement to BsonDocument.
    /// </summary>
    public BsonDocument GetKeyBsonDocument()
    {
        var json = JsonSerializer.Serialize(Key);
        return BsonDocument.Parse(json);
    }
}
