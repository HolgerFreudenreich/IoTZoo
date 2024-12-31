public string GetName(string json)
{
   JsonElement element = JsonSerializer.Deserialize<JsonElement>(json);

   double temperature = element.GetProperty("Temperature").GetDouble();
   int ledIndex = element.GetProperty("LedIndex").GetInt32();
   if (temperature < 0)
   {
      return "#000000";
   }

   return "#111111";
}

