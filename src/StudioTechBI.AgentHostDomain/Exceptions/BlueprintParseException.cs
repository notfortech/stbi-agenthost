namespace StudioTechBI.AgentHostDomain.Exceptions;

public sealed class BlueprintParseException : Exception
{
    public string? RawJson { get; }

    public BlueprintParseException(string message, string? rawJson = null, Exception? inner = null)
        : base(message, inner)
    {
        RawJson = rawJson;
    }
}
