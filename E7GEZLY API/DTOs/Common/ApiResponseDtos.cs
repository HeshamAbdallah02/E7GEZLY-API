// DTOs/Common/ApiResponseDtos.cs
namespace E7GEZLY_API.DTOs.Common
{
    public record ApiResponse<T>(
        bool Success,
        string Message,
        T? Data = default
    );

    public record SuccessResponse(
        string Message,
        object? Data = null
    ) : ApiResponse<object>(true, Message, Data);

    public record ErrorResponse(
        string Message,
        object? Details = null
    ) : ApiResponse<object>(false, Message, Details);

    public record ValidationErrorResponse(
        string Message,
        IEnumerable<string> Errors
    ) : ApiResponse<IEnumerable<string>>(false, Message, Errors);

    public record PagedResponse<T>(
        IEnumerable<T> Items,
        int TotalCount,
        int PageNumber,
        int PageSize,
        int TotalPages
    );
}