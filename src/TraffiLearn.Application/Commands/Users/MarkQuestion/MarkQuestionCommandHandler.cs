﻿using MediatR;
using Microsoft.Extensions.Logging;
using TraffiLearn.Application.Abstractions.Auth;
using TraffiLearn.Application.Abstractions.Data;
using TraffiLearn.Application.Identity;
using TraffiLearn.Domain.Errors;
using TraffiLearn.Domain.Errors.Users;
using TraffiLearn.Domain.RepositoryContracts;
using TraffiLearn.Domain.Shared;

namespace TraffiLearn.Application.Commands.Users.MarkQuestion
{
    internal sealed class MarkQuestionCommandHandler
        : IRequestHandler<MarkQuestionCommand, Result>
    {
        private readonly IAuthService<ApplicationUser> _authService;
        private readonly IUserRepository _userRepository;
        private readonly IQuestionRepository _questionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MarkQuestionCommandHandler> _logger;

        public MarkQuestionCommandHandler(
            IAuthService<ApplicationUser> authService,
            IUserRepository userRepository,
            IQuestionRepository questionRepository,
            IUnitOfWork unitOfWork,
            ILogger<MarkQuestionCommandHandler> logger)
        {
            _authService = authService;
            _userRepository = userRepository;
            _questionRepository = questionRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(
            MarkQuestionCommand request,
            CancellationToken cancellationToken)
        {
            var userIdResult = _authService.GetAuthenticatedUserId();

            if (userIdResult.IsFailure)
            {
                return userIdResult.Error;
            }

            var userId = userIdResult.Value;

            var question = await _questionRepository.GetByIdAsync(
                questionId: request.QuestionId.Value,
                cancellationToken);

            if (question is null)
            {
                return UserErrors.QuestionNotFound;
            }

            var user = await _userRepository.GetByIdAsync(
                userId,
                cancellationToken,
                includeExpressions: user => user.MarkedQuestions);

            if (user is null)
            {
                _logger.LogCritical(InternalErrors.AuthenticatedUserNotFound.Description);

                return InternalErrors.AuthenticatedUserNotFound;
            }

            var markResult = user.MarkQuestion(question);

            if (markResult.IsFailure)
            {
                return markResult.Error;
            }

            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Succesfully marked question. User's username: {username}", user.Username.Value);

            return Result.Success();
        }
    }
}
