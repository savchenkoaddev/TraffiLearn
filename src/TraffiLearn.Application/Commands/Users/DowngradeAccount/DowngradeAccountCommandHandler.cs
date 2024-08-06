﻿using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Transactions;
using TraffiLearn.Application.Abstractions.Auth;
using TraffiLearn.Application.Abstractions.Data;
using TraffiLearn.Application.Errors;
using TraffiLearn.Application.Identity;
using TraffiLearn.Application.Options;
using TraffiLearn.Domain.Errors.Users;
using TraffiLearn.Domain.RepositoryContracts;
using TraffiLearn.Domain.Shared;

namespace TraffiLearn.Application.Commands.Users.DowngradeAccount
{
    internal sealed class DowngradeAccountCommandHandler
        : IRequestHandler<DowngradeAccountCommand, Result>
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuthService<ApplicationUser> _authService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuthSettings _authSettings;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DowngradeAccountCommandHandler> _logger;

        public DowngradeAccountCommandHandler(
            IUserRepository userRepository,
            IAuthService<ApplicationUser> authService,
            UserManager<ApplicationUser> userManager,
            IOptions<AuthSettings> authSettings,
            IUnitOfWork unitOfWork,
            ILogger<DowngradeAccountCommandHandler> logger)
        {
            _userRepository = userRepository;
            _authService = authService;
            _userManager = userManager;
            _authSettings = authSettings.Value;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(
            DowngradeAccountCommand request,
            CancellationToken cancellationToken)
        {
            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                Result<Guid> downgraderIdResult = _authService.GetAuthenticatedUserId();

                if (downgraderIdResult.IsFailure)
                {
                    return downgraderIdResult.Error;
                }

                var downgraderId = downgraderIdResult.Value;

                var downgrader = await _userRepository.GetByIdAsync(
                    downgraderId,
                    cancellationToken);

                if (downgrader is null)
                {
                    _logger.LogCritical(InternalErrors.AuthenticatedUserNotFound.Description);

                    return InternalErrors.AuthenticatedUserNotFound;
                }

                if (downgrader.Role < _authSettings.MinimumAllowedRoleToDowngradeAccounts)
                {
                    return UserErrors.NotAllowedToPerformAction;
                }

                var user = await _userRepository.GetByIdAsync(
                    userId: request.UserId.Value,
                    cancellationToken);

                if (user is null)
                {
                    return UserErrors.NotFound;
                }

                if (user.Role < _authSettings.MinimumRoleForDowngrade)
                {
                    return UserErrors.AccountCannotBeDowngraded;
                }

                var identityUser = await _userManager.FindByIdAsync(user.Id.ToString());

                if (identityUser is null)
                {
                    return InternalErrors.DataConsistencyError;
                }

                var previousRole = user.Role;
                var downgradeResult = user.DowngradeRole();

                if (downgradeResult.IsFailure)
                {
                    return downgradeResult.Error;
                }

                await _userRepository.UpdateAsync(user);

                var removeRoleResult = await _authService.RemoveRole(
                    identityUser,
                    previousRole);

                if (removeRoleResult.IsFailure)
                {
                    return removeRoleResult.Error;
                }

                var assigningResult = await _authService.AssignRoleToUser(
                    identityUser,
                    user.Role);

                if (assigningResult.IsFailure)
                {
                    return assigningResult.Error;
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Successfully downgraded an account with email {Email}. Previous role: {PreviousRole}. New role: {NewRole}",
                    user.Email.Value,
                    previousRole,
                    user.Role);

                transaction.Complete();
            }

            return Result.Success();
        }
    }
}
