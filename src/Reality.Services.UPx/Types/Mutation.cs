﻿using HotChocolate;
using HotChocolate.Subscriptions;
using Reality.Common.Entities;
using Reality.Common.Services;
using Reality.Services.UPx.Repositories;

namespace Reality.Services.UPx.Types
{
    public class Mutation
    {
        public async Task<bool> RegisterStationUseAsync(int startTimestamp, int endTimestamp, int duration,
            double distributedWater, double economizedPlastic, double bottleQuantityEquivalent, string token,
            [Service] IUseRepository useRepository, [Service] IAuthorizationService authorizationService)
        {
            if (token.Length is 0)
                return false;

            var result = await authorizationService.CheckAuthorizationAsync(token);
            var roles = authorizationService.ExtractRoles(result).Select(r => (int)r);
            bool allowed;

            Console.WriteLine("Claims: " + String.Join(", ", result.Claims.Select(c => c.Key + ": " + c.Value)));

            if (!result.IsValid)
            {
                Console.WriteLine("Invalid token.");
                return false;
            }

            try
            {
                Console.WriteLine("Roles: " + String.Join(", ", roles));
                allowed = roles.Any(r => r <= 2);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                allowed = false;
            }

            // Check if role is Project or above.
            if (!allowed)
            {
                Console.WriteLine("Unauthorized.");
                return false;
            }

            Use use = new()
            {
                StartTimestamp = startTimestamp,
                EndTimestamp = endTimestamp,
                Duration = duration,
                DistributedWater = distributedWater,
                EconomizedPlastic = economizedPlastic,
                BottleQuantityEquivalent = bottleQuantityEquivalent
            };

            await useRepository.InsertAsync(use);

            return true;
        }
    }
}