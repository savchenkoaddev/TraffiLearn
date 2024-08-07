﻿using MediatR;
using TraffiLearn.Domain.Shared;

namespace TraffiLearn.Application.Commands.Tickets.Create
{
    public sealed record CreateTicketCommand(
        int? TicketNumber,
        List<Guid?>? QuestionIds) : IRequest<Result>;
}
