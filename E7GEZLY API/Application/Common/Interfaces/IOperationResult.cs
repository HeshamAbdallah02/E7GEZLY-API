namespace E7GEZLY_API.Application.Common.Interfaces
{
    /// <summary>
    /// Interface for operation results
    /// </summary>
    public interface IOperationResult
    {
        bool IsSuccess { get; }
        string? ErrorMessage { get; }
    }

    /// <summary>
    /// Generic interface for operation results with data
    /// </summary>
    /// <typeparam name="T">Type of data returned</typeparam>
    public interface IOperationResult<out T> : IOperationResult
    {
        T? Data { get; }
    }
}