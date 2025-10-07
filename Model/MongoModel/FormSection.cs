using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace FormBuilderAPI.Model.MongoModel
{
    public class FormSection
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        [BsonElement("fields")]
        public List<FormField> Fields { get; set; } = new();
    }

    public class FormField
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        [BsonElement("label")]
        public string Label { get; set; } = string.Empty;

        [BsonElement("type")]
        public string Type { get; set; } = string.Empty; // text, email, number, date, radio, etc.

        [BsonElement("required")]
        public bool Required { get; set; }

        [BsonElement("options")]
        public List<string> Options { get; set; } = new();
    }
}
