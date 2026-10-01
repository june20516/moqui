namespace Moqui.Core.Data
{
    public static class TuningLoader
    {
        public const string FilePath = "tuning.json";
        public const int SupportedFormatVersion = 1;

        private const string FormatVersionField = "formatVersion";
        private const string ValuesField = "values";

        public static Tuning Load(IDataSource source)
        {
            return Parse(source.ReadText(FilePath));
        }

        public static Tuning Parse(string json)
        {
            var root = JsonReader.Parse(json);
            if (root.Kind != JsonKind.Object)
            {
                throw new DataFormatException($"{FilePath}: root must be an object.");
            }

            if (!root.TryGetMember(FormatVersionField, out var version)
                || version.Kind != JsonKind.Number
                || version.NumberValue != SupportedFormatVersion)
            {
                throw new DataFormatException($"{FilePath}: {FormatVersionField} must be {SupportedFormatVersion}.");
            }

            if (!root.TryGetMember(ValuesField, out var values) || values.Kind != JsonKind.Object)
            {
                throw new DataFormatException($"{FilePath}: '{ValuesField}' object is missing.");
            }

            return new Tuning(values.Members);
        }
    }
}
