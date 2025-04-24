using Newtonsoft.Json;
using System;

namespace PackBuilder.Common.JsonBuilding.Recipes;

// Either All or Any.
// If "All" is specified, ALL of the conditions will need to be met in order to activate the changes of this mod.
// If "Any" is specified, ANY of the conditions being met will activate the changes of this mod.

[JsonConverter(typeof(RecipeCriteriaConverter))]
internal enum RecipeCriteria
{
    All,
    Any
}

internal sealed class RecipeCriteriaConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value is RecipeCriteria criteria)
            writer.WriteValue(Enum.GetName(typeof(RecipeCriteria), criteria));
    }

    public override object ReadJson(
        JsonReader reader,
        Type objectType,
        object existingValue,
        JsonSerializer serializer
    )
    {
        var recipeCriteria = typeof(RecipeCriteria);
        var options = Enum.GetValues(recipeCriteria);
        var value = reader.Value?.ToString() ?? Enum.GetName(recipeCriteria, 0);

        foreach (var option in options)
        {
            if (Enum.GetName(recipeCriteria, option).Equals(value, StringComparison.OrdinalIgnoreCase))
                return option;
        }

        return null;
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(RecipeCriteria);
    }
}