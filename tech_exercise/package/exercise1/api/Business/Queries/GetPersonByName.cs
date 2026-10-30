using Dapper;
using MediatR;
using StargateAPI.DataAccess;
using StargateAPI.Business.Dtos;
using StargateAPI.Controllers;
using System.Net;

namespace StargateAPI.Business.Queries
{
    public class GetPersonByName : IRequest<GetPersonByNameResult>
    {
        public required string Name { get; set; } = string.Empty;
    }

    public class GetPersonByNameHandler : IRequestHandler<GetPersonByName, GetPersonByNameResult>
    {
        private readonly StargateContext _context;
        public GetPersonByNameHandler(StargateContext context)
        {
            _context = context;
        }

        public async Task<GetPersonByNameResult> Handle(GetPersonByName request, CancellationToken cancellationToken)
        {
            var result = new GetPersonByNameResult();

            const string query = "SELECT a.Id as PersonId, a.Name, b.CurrentRank, b.CurrentDutyTitle, b.CareerStartDate, b.CareerEndDate FROM [Person] a LEFT JOIN [AstronautDetail] b on b.PersonId = a.Id WHERE a.Name = @Name";

            var person = await _context.Connection.QueryFirstOrDefaultAsync<PersonAstronaut>(
                new CommandDefinition(query, new { request.Name }, cancellationToken: cancellationToken));

            if (person is null)
            {
                result.Success = false;
                result.Message = $"Person '{request.Name}' was not found.";
                result.ResponseCode = (int)HttpStatusCode.NotFound;
                return result;
            }

            result.Person = person;
            result.Success = true;

            return result;
        }
    }

    public class GetPersonByNameResult : BaseResponse
    {
        public PersonAstronaut? Person { get; set; }
    }
}
