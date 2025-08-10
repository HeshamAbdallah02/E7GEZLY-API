namespace E7GEZLY_API.Application.Common.Interfaces
{
    /// <summary>
    /// Represents the result of an operation
    /// </summary>
    public class OperationResult : IOperationResult
    {
        protected OperationResult(bool isSuccess, string? errorMessage = null)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        public bool IsSuccess { get; }
        public string? ErrorMessage { get; }

        public static OperationResult Success()
        {
            return new OperationResult(true);
        }

        public static OperationResult Failure(string errorMessage)
        {
            return new OperationResult(false, errorMessage);
        }
    }

    /// <summary>
    /// Represents the result of an operation with data
    /// </summary>
    /// <typeparam name="T">Type of data returned</typeparam>
    public class OperationResult<T> : OperationResult, IOperationResult<T>
    {
        private OperationResult(bool isSuccess, T? data, string? errorMessage = null)
            : base(isSuccess, errorMessage)
        {
            Data = data;
        }

        public T? Data { get; }

        public static OperationResult<T> Success(T data)
        {
            return new OperationResult<T>(true, data);
        }

        public static new OperationResult<T> Failure(string errorMessage)
        {
            return new OperationResult<T>(false, default, errorMessage);
        }
    }
}