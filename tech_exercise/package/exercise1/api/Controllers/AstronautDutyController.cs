using MediatR;
using Microsoft.AspNetCore.Mvc;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Queries;
using System.Net;
using StargateAPI.Services;

namespace StargateAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AstronautDutyController : ControllerBase
    {
        private const int MaxPersonNameLength = 50;

        private readonly IMediator _mediator;
        private readonly ProcessLogger _processLogger;

        public AstronautDutyController(IMediator mediator, ProcessLogger processLogger)
        {
            _mediator = mediator;
            _processLogger = processLogger;
        }

        [HttpGet("GetAstronautDutiesByName/{name}")]
        public async Task<IActionResult> GetAstronautDutiesByName(string? name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return await RespondAsync(new BaseResponse()
                {
                    Message = "Person name must not be null or empty.",
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.BadRequest
                });
            }

            if (name.Length > MaxPersonNameLength)
            {
                return await RespondAsync(new BaseResponse()
                {
                    Message = $"Person name must not exceed {MaxPersonNameLength} characters.",
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.BadRequest
                });
            }

            try
            {
                var result = await _mediator.Send(new GetAstronautDutiesByName()
                {
                    Name = name
                });

                if (result.Person is null)
                {
                    return await RespondAsync(result);
                }

                if (result.AstronautDuties.Count == 0)
                {
                    result.Success = false;
                    result.Message = $"No astronaut duties were found for person '{name}'.";
                    result.ResponseCode = (int)HttpStatusCode.NotFound;
                    return await RespondAsync(result);
                }

                return await GetSuccessResponse(result);
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex);
                return await RespondAsync(new BaseResponse()
                {
                    Message = ex.Message,
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.InternalServerError
                });
            }
        }

        [HttpPost("CreateAstronautDuty")]
        public async Task<IActionResult> CreateAstronautDuty([FromBody] CreateAstronautDuty? request)
        {
            if (request is null)
            {
                return await RespondAsync(new BaseResponse()
                {
                    Message = "An astronaut duty request is required.",
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.BadRequest
                });
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return await RespondAsync(new BaseResponse()
                {
                    Message = "Person name must not be null or empty.",
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.BadRequest
                });
            }

            if (request.Name.Length > MaxPersonNameLength)
            {
                return await RespondAsync(new BaseResponse()
                {
                    Message = $"Person name must not exceed {MaxPersonNameLength} characters.",
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.BadRequest
                });
            }

            if (string.IsNullOrWhiteSpace(request.Rank) || string.IsNullOrWhiteSpace(request.DutyTitle))
            {
                return await RespondAsync(new BaseResponse()
                {
                    Message = "Rank and duty title must not be null or empty.",
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.BadRequest
                });
            }

            if (request.DutyStartDate == default)
            {
                return await RespondAsync(new BaseResponse()
                {
                    Message = "Duty start date is required.",
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.BadRequest
                });
            }

            try
            {
                var result = await _mediator.Send(request);
                return result.Id > 0
                    ? await GetSuccessResponse(result)
                    : await RespondAsync(result);
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex);
                return await RespondAsync(new BaseResponse()
                {
                    Message = ex.Message,
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.InternalServerError
                });
            }
        }

        private Task<IActionResult> GetSuccessResponse(BaseResponse response)
        {
            response.Success = true;
            response.Message = "Successful";
            response.ResponseCode = (int)HttpStatusCode.OK;
            return RespondAsync(response);
        }

        private async Task<IActionResult> RespondAsync(BaseResponse response)
        {
            await _processLogger.LogAsync(
                ControllerContext.ActionDescriptor.ActionName,
                GetType().Name,
                response.Success ? "Success" : "Failure",
                response.Message);
            return this.GetResponse(response);
        }

        private Task LogExceptionAsync(Exception exception)
        {
            return _processLogger.LogAsync(
                ControllerContext.ActionDescriptor.ActionName,
                GetType().Name,
                "Exception",
                exception.Message,
                exception);
        }
    }
}