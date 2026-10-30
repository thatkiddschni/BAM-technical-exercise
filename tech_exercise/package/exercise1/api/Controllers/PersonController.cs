using MediatR;
using Microsoft.AspNetCore.Mvc;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Queries;
using System.Net;
using System;
using StargateAPI.Services;

namespace StargateAPI.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class PersonController : ControllerBase
    {
        private const int MaxPersonNameLength = 50;
        private readonly IMediator _mediator;
        private readonly ProcessLogger _processLogger;

        public PersonController(IMediator mediator, ProcessLogger processLogger)
        {
            _mediator = mediator;
            _processLogger = processLogger;
        }

        [HttpGet("GetPeople")]
        public async Task<IActionResult> GetPeople()
        {
            try
            {
                var result = await _mediator.Send(new GetPeople()
                {

                });

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

        [HttpGet("GetPersonByName/{name}")]
        public async Task<IActionResult> GetPersonByName(string? name)
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
                var result = await _mediator.Send(new GetPersonByName()
                {
                    Name = name
                });

                return result.Person is null
                    ? await RespondAsync(result)
                    : await GetSuccessResponse(result);
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

        [HttpPost("CreatePerson")]
        public async Task<IActionResult> CreatePerson([FromBody] string name)
        {
            try
            {
                var result = await _mediator.Send(new CreatePerson()
                {
                    Name = name
                });

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

        [HttpPatch("UpdatePersonById/{personId:int}")]
        public async Task<IActionResult> UpdatePersonById(int personId, [FromBody] string? newName)
        {
            if (personId <= 0)
            {
                return await RespondAsync(new BaseResponse()
                {
                    Message = "Person ID must be greater than zero.",
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.BadRequest
                });
            }

            if (string.IsNullOrEmpty(newName))
            {
                return await RespondAsync(new BaseResponse()
                {
                    Message = "Updated person name must not be null or empty.",
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.BadRequest
                });
            }

            if (newName.Length > MaxPersonNameLength)
            {
                return await RespondAsync(new BaseResponse()
                {
                    Message = $"Updated person name must not exceed {MaxPersonNameLength} characters.",
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.BadRequest
                });
            }

            try
            {
                var result = await _mediator.Send(new UpdatePersonById
                {
                    PersonId = personId,
                    NewName = newName
                });

                return result.Success
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