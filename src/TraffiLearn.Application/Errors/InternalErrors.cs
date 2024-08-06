﻿using TraffiLearn.Domain.Shared;

namespace TraffiLearn.Application.Errors
{
    public static class InternalErrors
    {
        public static readonly Error AuthenticatedUserNotFound =
            Error.InternalFailure(
                code: "Internal.AuthenticatedUserNotFound",
                description: "Authenticated user has not been found. This is probably due to some data inconsistency issues.");

        public static readonly Error NotEnoughRecords =
            Error.InternalFailure(
                code: "Internal.NotEnoughRecords",
                description: "Cannot perform the operation because there are not enough existing records.");

        public static readonly Error AuthorizationFailure =
            Error.InternalFailure(
                code: "Internal.AuthorizationFailure",
                description: "The user is not authenticated. This is probably due to some authorization failures.");

        public static readonly Error DataConsistencyError =
            Error.InternalFailure(
                code: "Internal.DataConsistencyError",
                description: "There are some data consistency errors.");

        public static Error ClaimMissing(string claimName) => 
           Error.InternalFailure(
               code: "Internal.ClaimMissing",
               description: $"Couldn't fetch the {claimName} from http context. This is probably due to the token generation issues.");

        public static Error RoleAssigningFailure(string roleName) =>
           Error.InternalFailure(
               code: "Internal.ClaimMissing",
               description: $"Failed to assign role {roleName} to user.");
    }
}
