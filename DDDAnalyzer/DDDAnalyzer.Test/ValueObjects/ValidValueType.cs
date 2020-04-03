namespace DDDAnalyzer.Test.ValueObjects
{
    [DDDAnalyzer.Attributes.ValueObject]
    public class ValidValueType
    {
        public ValidValueType(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }
}
