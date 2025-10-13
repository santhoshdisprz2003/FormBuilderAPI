using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FormBuilderAPI.Model.MongoModel
{
    [JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public enum FormStatus
    {
        Draft,
        Published
    }
    public class Form
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("sections")]
        public List<FormSection> Sections { get; set; } = new();

        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public FormStatus Status { get; set; } = FormStatus.Draft;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } 

        [BsonElement("updatedAt")]
        public DateTime? UpdatedAt { get; set; } 

        [BsonElement("publishedAt")]
        public DateTime? PublishedAt { get; set; }
    }
}
