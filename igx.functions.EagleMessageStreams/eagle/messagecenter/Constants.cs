namespace igx.functions.EagleMessageStreams
{
    public enum MapFormat
    {
        CSV,
        Bloomberg,
        Fixed,
        SIRS,
        Star,
        Swift,
        TagValue,
        XML,
        Unknown
    }

    public enum MapDirection
    {
        Input,
        Output
    }

    public enum RulesetFileType
    {
        Default,
        Conditional
    }

    public enum RelationshipColumnType
    {
        Mapping,
        Constant
    }

    public enum RelationshipExpressionType
    {
        ConstantValue, // some string in a value
        DirectMapping, // star tag - bloomberg mnemonic
        ConditionalMapping, // some expression if this then that yada yada
        Unknown
    }
}
