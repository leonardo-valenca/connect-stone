namespace ConnectStone.Sdk.Internal;

/// <summary>
/// Best-effort shape of Pagar.me's error envelope (<c>{ "message": "...", "errors": {...} }</c>).
/// The Connect Stone docs don't publish an error-response reference, so this is inferred from the
/// platform's general API conventions rather than a documented contract. If your account's actual
/// error responses differ, adjust this type and <see cref="ConnectStone.Sdk.Exceptions.ConnectStoneApiException"/>
/// accordingly. All properties are optional so parsing never throws on an unexpected shape.
/// </summary>
internal sealed record ApiErrorResponse(string? Message, Dictionary<string, List<string>>? Errors);
