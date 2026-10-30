using MediatR;
using StargateAPI.Controllers;
using StargateAPI.Services;

namespace StargateAPI.Business
{
    public class ProcessLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly ProcessLogger _processLogger;

        public ProcessLoggingBehavior(ProcessLogger processLogger)
        {
            _processLogger = processLogger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            TResponse response;
            try
            {
                response = await next();
            }
            catch (Exception exception)
            {
                await _processLogger.LogAsync(
                    typeof(TRequest).Name,
                    $"{typeof(TRequest).Name}Handler",
                    "Exception",
                    exception.Message,
                    exception,
                    CancellationToken.None);
                throw;
            }

            var result = response as BaseResponse;
            var succeeded = result?.Success ?? true;
            await _processLogger.LogAsync(
                typeof(TRequest).Name,
                $"{typeof(TRequest).Name}Handler",
                succeeded ? "Success" : "Failure",
                result?.Message ?? "Operation completed.",
                cancellationToken: cancellationToken);

            return response;
        }
    }
}
