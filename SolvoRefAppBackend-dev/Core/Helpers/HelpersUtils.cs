using System.Text;
using System.Text.Json;

namespace Core.Helpers
{
    public class HelpersUtils
    {
        public static string ReplaceAccentsAndSpecialChars(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var replacements = new Dictionary<char, char>
        {
            { 'á', 'a' }, { 'é', 'e' }, { 'í', 'i' }, { 'ó', 'o' }, { 'ú', 'u' },
            { 'Á', 'A' }, { 'É', 'E' }, { 'Í', 'I' }, { 'Ó', 'O' }, { 'Ú', 'U' },
            { 'ñ', 'n' }, { 'Ñ', 'N' }
        };
            var result = new StringBuilder(input.Length);
            foreach (var c in input)
            {
                result.Append(replacements.ContainsKey(c) ? replacements[c] : c);
            }

            return result.ToString();
        }

        public static string GetFlattenUtcDate()
        {
            DateTime now = DateTime.UtcNow;
            return now.ToString("yyyyMMddHHmmss");
        }

        public class PublicHoliday
        {
            public DateTime Date { get; set; }
            public string LocalName { get; set; }
            public string Name { get; set; }
            public string CountryCode { get; set; }
            public bool Fixed { get; set; }
            public bool Global { get; set; }
            public string[] Counties { get; set; }
            public int? LaunchYear { get; set; }
            public string[] Types { get; set; }
        }


        public static async Task<HashSet<DateTime>> HolidaysAsync(int year, string countryCode)
        {
            HashSet<DateTime> holidaysData = [];
            if (countryCode == "IN")
            {
                holidaysData.Add(new DateTime(year, 1, 26));
                holidaysData.Add(new DateTime(year, 2, 26));
                holidaysData.Add(new DateTime(year, 3, 14));
                holidaysData.Add(new DateTime(year, 3, 31));
                holidaysData.Add(new DateTime(year, 4, 6));
                holidaysData.Add(new DateTime(year, 4, 10));
                holidaysData.Add(new DateTime(year, 4, 18));
                holidaysData.Add(new DateTime(year, 5, 12));
                holidaysData.Add(new DateTime(year, 6, 7));
                holidaysData.Add(new DateTime(year, 8, 15));
                holidaysData.Add(new DateTime(year, 10, 2));
                holidaysData.Add(new DateTime(year, 10, 21));
            }
            else
            {
                var jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                using var httpClient = new HttpClient();
                using var response = await httpClient.GetAsync($"https://date.nager.at/api/v3/publicholidays/{year}/{countryCode}");
                if (response.IsSuccessStatusCode)
                {
                    using var jsonStream = await response.Content.ReadAsStreamAsync();
                    var publicHolidays = JsonSerializer.Deserialize<PublicHoliday[]>(jsonStream, jsonSerializerOptions);
                    holidaysData = [.. publicHolidays.Select(h => h.Date.Date)];
                }
                else
                {
                    throw new Exception("error in nager request please check with a admin");
                }
            }
            return holidaysData;
        }
    }
}