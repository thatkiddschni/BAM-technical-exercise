using Dapper;
using MediatR;
using StargateAPI.DataAccess;
using StargateAPI.Business.Dtos;
using StargateAPI.Controllers;
using System.Net;

namespace StargateAPI.Business.Queries
{
    public class GetAstronautDutiesByName : IRequest<GetAstronautDutiesByNameResult>
    {
        public string Name { get; set; } = string.Empty;
    }

    public class GetAstronautDutiesByNameHandler : IRequestHandler<GetAstronautDutiesByName, GetAstronautDutiesByNameResult>
    {
        private readonly StargateContext _context;

        public GetAstronautDutiesByNameHandler(StargateContext context)
        {
            _context = context;
        }

        public async Task<GetAstronautDutiesByNameResult> Handle(GetAstronautDutiesByName request, CancellationToken cancellationToken)
        {

            var result = new GetAstronautDutiesByNameResult();

            const string query = "SELECT a.Id as PersonId, a.Name, b.CurrentRank, b.CurrentDutyTitle, b.CareerStartDate, b.CareerEndDate FROM [Person] a LEFT JOIN [AstronautDetail] b on b.PersonId = a.Id WHERE a.Name = @Name";

            var person = await _context.Connection.QueryFirstOrDefaultAsync<PersonAstronaut>(
                new CommandDefinition(query, new { request.Name }, cancellationToken: cancellationToken));

            result.Person = person;

            if (person is null)
            {
                result.Success = false;
                result.Message = $"Person '{request.Name}' was not found.";
                result.ResponseCode = (int)HttpStatusCode.NotFound;
                return result;
            }

            const string dutiesQuery = "SELECT * FROM [AstronautDuty] WHERE PersonId = @PersonId ORDER BY DutyStartDate DESC";
            var duties = await _context.Connection.QueryAsync<AstronautDuty>(
                new CommandDefinition(dutiesQuery, new { person.PersonId }, cancellationToken: cancellationToken));

            result.AstronautDuties = duties.ToList();
            result.Success = true;

            return result;

        }
    }

    public class GetAstronautDutiesByNameResult : BaseResponse
    {
        public PersonAstronaut? Person { get; set; }
        public List<AstronautDuty> AstronautDuties { get; set; } = new List<AstronautDuty>();
    }
}
