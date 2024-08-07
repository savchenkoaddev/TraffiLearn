﻿using FluentValidation;

namespace TraffiLearn.Application.Commands.Questions.AddTopicToQuestion
{
    internal sealed class AddTopicToQuestionCommandValidator : AbstractValidator<AddTopicToQuestionCommand>
    {
        public AddTopicToQuestionCommandValidator()
        {
            RuleFor(x => x.TopicId)
                .NotEmpty();

            RuleFor(x => x.QuestionId)
                .NotEmpty();
        }
    }
}
