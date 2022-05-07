using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeyondAuth.UserManagement.Helpers
{
    internal sealed class HttpValidationProblemDetailsJsonConverter : JsonConverter<HttpValidationProblemDetails>
    {
        private static readonly JsonEncodedText Errors = JsonEncodedText.Encode("errors");

        public override HttpValidationProblemDetails Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var problemDetails = new HttpValidationProblemDetails();
            return ReadProblemDetails(ref reader, options, problemDetails);
        }

        public static HttpValidationProblemDetails ReadProblemDetails(ref Utf8JsonReader reader, JsonSerializerOptions options, HttpValidationProblemDetails problemDetails)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Unexcepted end when reading JSON.");
            }

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.ValueTextEquals(Errors.EncodedUtf8Bytes))
                {
                    var errors = DeserializeErrors(ref reader, options);
                    if (errors != null)
                    {
                        foreach (var item in errors)
                        {
                            problemDetails.Errors[item.Key] = item.Value;
                        }
                    }
                }
                else
                {
                    ProblemDetailsJsonConverter.ReadValue(ref reader, problemDetails, options);
                }
            }

            if (reader.TokenType != JsonTokenType.EndObject)
            {
                throw new JsonException("Unexcepted end when reading JSON.");
            }

            return problemDetails;

            static Dictionary<string, string[]>? DeserializeErrors(ref Utf8JsonReader reader, JsonSerializerOptions options)
                => JsonSerializer.Deserialize<Dictionary<string, string[]>>(ref reader, options);
        }

        public override void Write(Utf8JsonWriter writer, HttpValidationProblemDetails value, JsonSerializerOptions options)
        {
            WriteProblemDetails(writer, value, options);
        }

        public static void WriteProblemDetails(Utf8JsonWriter writer, HttpValidationProblemDetails value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            ProblemDetailsJsonConverter.WriteProblemDetails(writer, value, options);

            writer.WritePropertyName(Errors);
            SerializeErrors(writer, value.Errors, options);

            writer.WriteEndObject();

            static void SerializeErrors(Utf8JsonWriter writer, IDictionary<string, string[]> errors, JsonSerializerOptions options)
                => JsonSerializer.Serialize(writer, errors, options);
        }
    }
}
