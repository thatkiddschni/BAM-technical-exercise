using MediatR;
using Microsoft.EntityFrameworkCore;
using StargateAPI.DataAccess;
using StargateAPI.Controllers;
using System.Net;

namespace StargateAPI.Business.Commands
{
    public class CreateAstronautDuty : IRequest<CreateAstronautDutyResult>
    {
        public required string Name { get; set; }

        public required string Rank { get; set; }

        public required string DutyTitle { get; set; }

        public DateTime DutyStartDate { get; set; }
    }

    public class CreateAstronautDutyHandler : IRequestHandler<CreateAstronautDuty, CreateAstronautDutyResult>
    {
        private readonly StargateContext _context;

        public CreateAstronautDutyHandler(StargateContext context)
        {
            _context = context;
        }
        public async Task<CreateAstronautDutyResult> Handle(CreateAstronautDuty request, CancellationToken cancellationToken)
        {
            var person = await _context.People
                .SingleOrDefaultAsync(candidate => candidate.Name == request.Name, cancellationToken);

            if (person is null)
            {
                return new CreateAstronautDutyResult
                {
                    Success = false,
                    Message = $"Person '{request.Name}' was not found.",
                    ResponseCode = (int)HttpStatusCode.NotFound
                };
            }

            var dutyStartDate = request.DutyStartDate.Date;
            var isRetired = string.Equals(request.DutyTitle, "RETIRED", StringComparison.OrdinalIgnoreCase);
            var previousDuty = await _context.AstronautDuties
                .Where(duty => duty.PersonId == person.Id)
                .OrderByDescending(duty => duty.DutyStartDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (previousDuty is not null && dutyStartDate <= previousDuty.DutyStartDate.Date)
            {
                return new CreateAstronautDutyResult
                {
                    Success = false,
                    Message = "A new duty must start after the Person's current duty.",
                    ResponseCode = (int)HttpStatusCode.BadRequest
                };
            }

            var astronautDetail = await _context.AstronautDetails
                .SingleOrDefaultAsync(detail => detail.PersonId == person.Id, cancellationToken);

            if (astronautDetail == null)
            {
                astronautDetail = new AstronautDetail
                {
                    PersonId = person.Id,
                    CurrentDutyTitle = request.DutyTitle,
                    CurrentRank = request.Rank,
                    CareerStartDate = dutyStartDate,
                    CareerEndDate = isRetired
                        ? dutyStartDate.AddDays(-1)
                        : null
                };

                await _context.AstronautDetails.AddAsync(astronautDetail, cancellationToken);

            }
            else
            {
                astronautDetail.CurrentDutyTitle = request.DutyTitle;
                astronautDetail.CurrentRank = request.Rank;
                if (isRetired)
                {
                    astronautDetail.CareerEndDate = dutyStartDate.AddDays(-1);
                }
            }

            if (previousDuty is not null)
            {
                previousDuty.DutyEndDate = dutyStartDate.AddDays(-1);
            }

            var newAstronautDuty = new AstronautDuty()
            {
                PersonId = person.Id,
                Rank = request.Rank,
                DutyTitle = request.DutyTitle,
                DutyStartDate = dutyStartDate,
                DutyEndDate = null
            };

            await _context.AstronautDuties.AddAsync(newAstronautDuty, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return new CreateAstronautDutyResult()
            {
                Success = true,
                Id = newAstronautDuty.Id
            };
        }
    }

    public class CreateAstronautDutyResult : BaseResponse
    {
        public int? Id { get; set; }
    }
}
