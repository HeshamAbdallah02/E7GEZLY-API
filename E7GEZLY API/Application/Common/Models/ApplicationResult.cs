namespace E7GEZLY_API.Application.Common.Models
{
    /// <summary>
    /// Represents a result from an application operation
    /// </summary>
    public class ApplicationResult
    {
        internal ApplicationResult(bool succeeded, IEnumerable<string> errors)
        {
            Succeeded = succeeded;
            Errors = errors.ToArray();
        }

        public bool Succeeded { get; set; }
        public bool IsSuccess => Succeeded;
        public string[] Errors { get; set; }
        public string? ErrorMessage => Errors?.FirstOrDefault();

        public static ApplicationResult Success()
        {
            return new ApplicationResult(true, Array.Empty<string>());
        }

        public static ApplicationResult Failure(IEnumerable<string> errors)
        {
            return new ApplicationResult(false, errors);
        }

        public static ApplicationResult Failure(params string[] errors)
        {
            return new ApplicationResult(false, errors);
        }
    }

    /// <summary>
    /// Represents a result from an application operation with data
    /// </summary>
    public class ApplicationResult<T> : ApplicationResult
    {
        internal ApplicationResult(bool succeeded, T? data, IEnumerable<string> errors)
            : base(succeeded, errors)
        {
            Data = data;
        }

        public T? Data { get; set; }

        public static ApplicationResult<T> Success(T data)
        {
            return new ApplicationResult<T>(true, data, Array.Empty<string>());
        }

        public static new ApplicationResult<T> Failure(IEnumerable<string> errors)
        {
            return new ApplicationResult<T>(false, default, errors);
        }

        public static new ApplicationResult<T> Failure(params string[] errors)
        {
            return new ApplicationResult<T>(false, default, errors);
        }
    }
}