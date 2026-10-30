using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Controllers;
using StargateAPI.DataAccess;
using System.Net;

namespace StargateAPI.Business.Commands
{
    public class UpdatePersonById : IRequest<UpdatePersonByIdResult>
    {
        public int PersonId { get; set; }

        public required string NewName { get; set; }
    }

    public class UpdatePersonByIdHandler : IRequestHandler<UpdatePersonById, UpdatePersonByIdResult>
    {
        private readonly StargateContext _context;

        public UpdatePersonByIdHandler(StargateContext context)
        {
            _context = context;
        }

        public async Task<UpdatePersonByIdResult> Handle(
            UpdatePersonById request,
            CancellationToken cancellationToken)
        {
            var person = await _context.People
                .SingleOrDefaultAsync(candidate => candidate.Id == request.PersonId, cancellationToken);

            if (person is null)
            {
                return new UpdatePersonByIdResult
                {
                    Success = false,
                    Message = $"Person with ID '{request.PersonId}' was not found.",
                    ResponseCode = (int)HttpStatusCode.NotFound
                };
            }

            person.Name = request.NewName;

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when
                (exception.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 })
            {
                return new UpdatePersonByIdResult
                {
                    Success = false,
                    Message = $"A person named '{request.NewName}' already exists.",
                    ResponseCode = (int)HttpStatusCode.Conflict
                };
            }

            return new UpdatePersonByIdResult
            {
                Success = true,
                Id = person.Id
            };
        }
    }

    public class UpdatePersonByIdResult : BaseResponse
    {
        public int Id { get; set; }
    }
}
