using Domain.Common;

namespace Api;

internal static class ResultExtensions
{
    public static IResult ToHttpResult<T>(this Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();

    public static IResult ToHttpResult(this Result result) =>
        result.IsSuccess ? Results.Ok() : result.Error.ToProblem();

    private static IResult ToProblem(this Error error) => Results.Problem(
        title: error.Message,
        statusCode: error.Code switch
        {
            "Orders.NotFound" => StatusCodes.Status404NotFound,
            "Orders.TooManyOpen" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        },
        extensions: new Dictionary<string, object?> { ["code"] = error.Code });
}
