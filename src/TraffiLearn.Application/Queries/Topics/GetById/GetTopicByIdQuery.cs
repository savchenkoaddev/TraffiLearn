﻿using MediatR;
using TraffiLearn.Application.DTO.Topics;
using TraffiLearn.Domain.Shared;

namespace TraffiLearn.Application.Queries.Topics.GetById
{
    public sealed record GetTopicByIdQuery(
        Guid? TopicId) : IRequest<Result<TopicResponse>>;
}
