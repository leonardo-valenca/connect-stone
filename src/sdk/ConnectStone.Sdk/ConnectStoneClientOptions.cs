namespace ConnectStone.Sdk;

public sealed class ConnectStoneClientOptions
{
    public const string SectionName = "ConnectStone";

    /// <summary>
    /// The Pagar.me secret key (from id.pagar.me &gt; Account Settings &gt; Keys). Sent as the
    /// username half of HTTP Basic auth with an empty password.
    /// </summary>
    public required string SecretKey { get; set; }

    /// <summary>
    /// Unique partner reference id assigned by the Stone Partner program, sent as the
    /// <c>ServiceRefererName</c> header on every request. Connect Stone's docs list this as
    /// required, but a working reference integration sends requests without it, so it's treated
    /// as optional here: leave it null/empty to omit the header entirely rather than sending it
    /// blank.
    /// </summary>
    public string? ServiceRefererName { get; set; }

    public string BaseUrl { get; set; } = "https://api.pagar.me";
}
