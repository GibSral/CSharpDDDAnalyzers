using DDDAnalyzer.Test.Entities;

namespace DDDAnalyzer.Test.ValueObjects
{
    [DDDAnalyzer.Attributes.ValueObject]
    public class InvalidValueType
    {
        public InvalidValueType(SomeEntity entity)
        {
            Entity = entity;
        }

        public SomeEntity Entity { get; }
    }
}
