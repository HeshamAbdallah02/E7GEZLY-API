// DTOs/Common/ApiResponseDtos.cs
namespace E7GEZLY_API.DTOs.Common
{
    public record ApiResponse<T>(
        bool Success,
        string Message,
        T? Data = default,
        IEnumerable<string>? Errors = null
    )
    {
        public static ApiResponse<T> CreateSuccess(T data, string message = "Success")
        {
            return new ApiResponse<T>(true, message, data);
        }

        public static ApiResponse<T> CreateError(string message)
        {
            return new ApiResponse<T>(false, message, default);
        }

        public static ApiResponse<T> CreateError(string message, IEnumerable<string> errors)
        {
            return new ApiResponse<T>(false, message, default, errors);
        }
    };

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