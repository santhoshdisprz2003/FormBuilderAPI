using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace FormBuilderAPI.Model.MongoModel
{
    public enum FormStatus
    {
        Draft,
        Published
    }

    // 📝 Main Form Document
    public class Form
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!; // MongoDB will generate this if null

        [BsonElement("config")]
        public FormConfig Config { get; set; } = new();

        [BsonElement("layout")]
        public FormLayout Layout { get; set; } = new();

        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public FormStatus Status { get; set; } = FormStatus.Draft;

        [BsonElement("created_by")]
        public string CreatedBy { get; set; } = string.Empty;

        [BsonElement("published_by")]
        public string? PublishedBy { get; set; }

        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [BsonElement("published_at")]
        public DateTime? PublishedAt { get; set; }
    }

    // 🔹 Form Configuration
    public class FormConfig
    {
        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;
    }

    // 🔹 Form Layout
    public class FormLayout
    {
        [BsonElement("headerCard")]
        public FormHeaderCard HeaderCard { get; set; } = new();

        [BsonElement("fields")]
        public List<FormField> Fields { get; set; } = new();
    }

    // 🔹 Header Card
    public class FormHeaderCard
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;
    }

    // 🔹 Form Field
    public class FormField
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
       public string QuestionId { get; set; } = ObjectId.GenerateNewId().ToString();

        [BsonElement("label")]
        public string Label { get; set; } = string.Empty;

        [BsonElement("type")]
        public string Type { get; set; } = string.Empty;

        [BsonElement("description_enabled")]
        public bool DescriptionEnabled { get; set; } = false;

        [BsonElement("description")]
        public string? Description { get; set; }

        [BsonElement("single_choice")]
        public bool SingleChoice { get; set; } = false;

        [BsonElement("multiple_choice")]
        public bool MultipleChoice { get; set; } = false;

        [BsonElement("options")]
        public List<FieldOption> Options { get; set; } = new();

        [BsonElement("format")]
        public string? Format { get; set; }

        [BsonElement("required")]
        public bool Required { get; set; }

        [BsonElement("order")]
        public int Order { get; set; }
    }

    // 🔹 Option for multiple choice fields
    public class FieldOption
    {
        [BsonId]
         [BsonIgnoreIfNull] 
        [BsonRepresentation(BsonType.ObjectId)]
        public string? OptionId { get; set; } = ObjectId.GenerateNewId().ToString();
        [BsonElement("value")]
        public string Value { get; set; } = string.Empty;
    }
}
