using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;

namespace Web.Configuration
{
    public class CustomExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<CustomExceptionHandler> logger;
        public CustomExceptionHandler(ILogger<CustomExceptionHandler> logger)
        {
            this.logger = logger;
        }
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var exceptionMessage = exception.ToString();
            logger.LogError(
                "Error Message: {exceptionMessage}, Time of occurrence {time}",
                exceptionMessage, DateTime.Now);
            httpContext.Response.StatusCode = exception switch {
                UnauthorizedAccessException => StatusCodes.Status403Forbidden,
                KeyNotFoundException => StatusCodes.Status404NotFound,
                Microsoft.Data.SqlClient.SqlException { Number: 1205 } => StatusCodes.Status409Conflict,
                InvalidOperationException or InvalidDataException or ArgumentException => StatusCodes.Status400BadRequest,
                Microsoft.EntityFrameworkCore.DbUpdateException => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError
            };
            await httpContext.Response.WriteAsJsonAsync(new {
                message = exception is Microsoft.Data.SqlClient.SqlException { Number: 1205 }
                    ? "Dữ liệu đang được cập nhật bởi thao tác khác. Vui lòng thử lại."
                    : httpContext.Response.StatusCode == 500 ? "Đã xảy ra lỗi hệ thống." : exception.Message
            }, cancellationToken);
            return true;
        }

    }
}
