﻿using MediatR;
using Microsoft.AspNetCore.Http;
using TraffiLearn.Application.DTO.Answers;

namespace TraffiLearn.Application.Commands.Questions.Create
{
    public sealed record CreateQuestionCommand(
        string? Content,
        string? Explanation,
        int? TicketNumber,
        int? QuestionNumber,
        List<Guid?>? TopicsIds,
        List<AnswerRequest?>? Answers,
        IFormFile? Image) : IRequest;
}
