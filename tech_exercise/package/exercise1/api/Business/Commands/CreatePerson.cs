using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StargateAPI.DataAccess;
using StargateAPI.Controllers;
using System.Net;

namespace StargateAPI.Business.Commands
{
    public class CreatePerson : IRequest<CreatePersonResult>
    {
        public required string Name { get; set; } = string.Empty;
    }

    public class CreatePersonHandler : IRequestHandler<CreatePerson, CreatePersonResult>
    {
        private readonly StargateContext _context;

        public CreatePersonHandler(StargateContext context)
        {
            _context = context;
        }
        public async Task<CreatePersonResult> Handle(CreatePerson request, CancellationToken cancellationToken)
        {

            var newPerson = new Person()
            {
                Name = request.Name
            };

            await _context.People.AddAsync(newPerson);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when
                (exception.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 })
            {
                return new CreatePersonResult
                {
                    Success = false,
                    Message = $"A person named '{request.Name}' already exists.",
                    ResponseCode = (int)HttpStatusCode.Conflict
                };
            }

            return new CreatePersonResult()
            {
                Success = true,
                Id = newPerson.Id
            };

        }
    }

    public class CreatePersonResult : BaseResponse
    {
        public int Id { get; set; }
    }
}
