using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorImage.Models
{


    internal sealed class Result<T>
    {
        public bool IsSuccess { get; }
        public T Value { get; }
        public string? Error { get; }
        public Result()
        {
            Value = default!;
            Error = null;
        }

        private Result(bool isSuccess, T value, string? error)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
        }
        public static Result<T> Success(T value) => new(true, value, null);
        public static Result<T> Failure(string error) => new(false, default!, error);
    }
}
