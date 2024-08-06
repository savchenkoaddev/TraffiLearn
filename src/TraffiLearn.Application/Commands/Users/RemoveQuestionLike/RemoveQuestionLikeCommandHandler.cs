﻿using MediatR;
using Microsoft.Extensions.Logging;
using TraffiLearn.Application.Abstractions.Auth;
using TraffiLearn.Application.Abstractions.Data;
using TraffiLearn.Application.Commands.Users.LikeQuestion;
using TraffiLearn.Application.Errors;
using TraffiLearn.Application.Identity;
using TraffiLearn.Domain.Errors.Users;
using TraffiLearn.Domain.RepositoryContracts;
using TraffiLearn.Domain.Shared;

namespace TraffiLearn.Application.Commands.Users.RemoveQuestionLike
{
    internal sealed class RemoveQuestionLikeCommandHandler
        : IRequestHandler<RemoveQuestionLikeCommand, Result>
    {
        private readonly IAuthService<ApplicationUser> _authService;
        private readonly IQuestionRepository _questionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<LikeQuestionCommandHandler> _logger;

        public RemoveQuestionLikeCommandHandler(
            IAuthService<ApplicationUser> authService,
            IQuestionRepository questionRepository,
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            ILogger<LikeQuestionCommandHandler> logger)
        {
            _authService = authService;
            _questionRepository = questionRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(
            RemoveQuestionLikeCommand request,
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
                cancellationToken,
                includeExpressions:
                    [question => question.LikedByUsers,
                        question => question.DislikedByUsers]);

            if (question is null)
            {
                return UserErrors.QuestionNotFound;
            }

            var user = await _userRepository.GetByIdAsync(
                userId,
                cancellationToken,
                includeExpressions:
                    [user => user.LikedQuestions,
                        user => user.DislikedQuestions]);

            if (user is null)
            {
                _logger.LogCritical(InternalErrors.AuthenticatedUserNotFound.Description);

                return InternalErrors.AuthenticatedUserNotFound;
            }

            var removeQuestionLikeResult = user.RemoveQuestionLike(question);

            if (removeQuestionLikeResult.IsFailure)
            {
                return removeQuestionLikeResult.Error;
            }

            var removeLikeResult = question.RemoveLike(user);

            if (removeLikeResult.IsFailure)
            {
                return removeLikeResult.Error;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}