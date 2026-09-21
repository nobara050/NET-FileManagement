using System.Diagnostics;
using Drive.Application.Common.Telemetry;
using MediatR;

namespace Drive.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior tự động tạo một Activity (span) cho mỗi handler
/// trong Application layer, giúp trace từng command/query riêng biệt trong Tempo.
///
/// Span được đặt tên theo convention: "Handler {CommandName}"
/// Ví dụ: "Handler CreateFileCommand", "Handler GetDriveItemQuery"
///
/// Thứ tự behavior trong pipeline (DI registration order):
///   TracingBehavior → ValidationBehavior → Handler
/// </summary>
public sealed class TracingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var operationName = $"Handler {requestName}";

        using var activity = ActivitySources.Application.StartActivity(
            operationName,
            ActivityKind.Internal);

        // Tag thêm thông tin để filter/search trong Tempo
        activity?.SetTag("handler.name", typeof(TRequest).FullName);
        activity?.SetTag("handler.type", requestName);

        try
        {
            var response = await next(cancellationToken);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return response;
        }
        catch (Exception ex)
        {
            // Đánh dấu span là lỗi để dễ phát hiện trong Tempo
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            // Ghi exception vào span dùng BCL thuần (không cần OTel package)
            var tags = new ActivityTagsCollection
            {
                { "exception.type", ex.GetType().FullName },
                { "exception.message", ex.Message },
                { "exception.stacktrace", ex.StackTrace }
            };
            activity?.AddEvent(new ActivityEvent("exception", tags: tags));

            throw;
        }
    }
}
