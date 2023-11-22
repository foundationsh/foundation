using System.Net;
using Foundation.Core.SDK.Database.Mongo;
using Foundation.Services.UPx.Services;
using Foundation.Services.UPx.Types.Payloads;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SDKAuth = Foundation.Core.SDK.Auth.JWT;

namespace Foundation.Services.UPx.Controllers;

public class EcobucksController : Controller
{
    private SDKAuth::IAuthorizationService AuthorizationService { get; }

    private IEcobucksLocationService EcobucksLocationService { get; }

    private IRepository<DisposalClaim> DisposalRepository { get; }
    private IRepository<EcobucksProfile> ProfileRepository { get; }
    private IRepository<Transaction> TransactionRepository { get; }

    private ILogger<EcobucksController> Logger { get; }

    public EcobucksController(
        SDKAuth::IAuthorizationService authorizationService,
        IEcobucksLocationService ecobucksLocationService,
        IRepository<EcobucksProfile> profileRepository,
        IRepository<DisposalClaim> disposalRepository,
        IRepository<Transaction> transactionRepository,
        ILogger<EcobucksController> logger
    )
    {
        AuthorizationService = authorizationService;
        EcobucksLocationService = ecobucksLocationService;

        DisposalRepository = disposalRepository;
        ProfileRepository = profileRepository;
        TransactionRepository = transactionRepository;

        Logger = logger;
    }

    [HttpGet]
    [Authorize]
    [Route("ecobucks/me")]
    [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GetEcobucksProfilePayload))]
    public async Task<GetEcobucksProfilePayload> GetEcobucksProfileAsync()
    {
        string? token = await HttpContext.GetTokenAsync("access_token");
        if (token is null || token.Length is 0)
        {
            StatusCode(401);
            return new GetEcobucksProfilePayload
            {
                Successful = false,
                Error = "Invalid token."
            };
        }

        var result = await AuthorizationService.CheckAuthorizationAsync(token);
        if (!result.IsValid)
        {
            Console.WriteLine("============== INVALID TOKEN ==================");
            StatusCode(401);
            return new GetEcobucksProfilePayload
            {
                Successful = false,
                Error = "Invalid token."
            };
        }

        var user = AuthorizationService.ExtractUser(result);
        if (user == null)
        {
            Console.WriteLine("============== USER IS NULL ==================");
            StatusCode(401);
            return new GetEcobucksProfilePayload
            {
                Successful = false,
                Error = "Invalid token."
            };
        }

        Console.WriteLine("============== AFTER USER VALIDATION ================");

        EcobucksProfile profile;
        try
        {
            Console.WriteLine("======> BEFORE GETTING PROFILE <======");
            profile = await ProfileRepository.GetByIdAsync(user.Id);
            Console.WriteLine("======> AFTER GETTING PROFILE <======");

            if (profile == null)
            {
                Console.WriteLine("======> PROFILE IS NULL <======");

                // Initialize a Ecobucks profile.
                profile = new EcobucksProfile()
                {
                    Id = user.Id,
                    Name = user.Username,
                    Username = user.Username,
                    Credits = 0,
                    IsOperator = false,
                    Transactions = new Transaction[] { }
                };

                Console.WriteLine("======> BEFORE CREATING PROFILE <======");
                await ProfileRepository.InsertAsync(profile);
                Console.WriteLine("======> AFTER CREATING PROFILE <======");

                Console.WriteLine("Created a new Ecobucks profile. ({0})", user.Username);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Console.WriteLine("======> ERROR RETURN NULL <======");
            return null!;
        }

        Console.WriteLine("======> BEFORE RETURNING PROFILE <======");
        return new GetEcobucksProfilePayload
        {
            Successful = true,
            Profile = profile
        };
    }

    [HttpGet]
    [Authorize]
    [Route("ecobucks/me/disposals")]
    [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(UserDisposalsPayload))]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized, Type = typeof(UserDisposalsPayload))]
    public async Task<UserDisposalsPayload> GetUserDisposalsAsync()
    {
        string? token = await HttpContext.GetTokenAsync("access_token");
        if (token is null || token.Length is 0)
        {
            StatusCode(401);
            return new UserDisposalsPayload
            {
                Successful = false,
                Error = "Invalid token."
            };
        }

        var result = await AuthorizationService.CheckAuthorizationAsync(token);
        if (!result.IsValid)
        {
            StatusCode(401);
            return new UserDisposalsPayload
            {
                Successful = false,
                Error = "Invalid token."
            };
        }

        var user = AuthorizationService.ExtractUser(result);
        if (user == null)
        {
            StatusCode(401);
            return new UserDisposalsPayload
            {
                Successful = false,
                Error = "Invalid token."
            };
        }

        var disposals = await DisposalRepository.Collection.Find(_ => _.Id == user.Id).ToListAsync();

        return new UserDisposalsPayload
        {
            Successful = true,
            UserDisposals = disposals
        };
    }

    [HttpPut]
    [Authorize]
    [Route("ecobucks/me/disposals")]
    [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RegisterDisposalPayload))]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized, Type = typeof(RegisterDisposalPayload))]
    public async Task<RegisterDisposalPayload> RegisterDisposalAsync([FromBody] RegisterDisposalInput input)
    {
        string? token = await HttpContext.GetTokenAsync("access_token");
        if (token is null || token.Length is 0)
        {
            StatusCode(401);
            return new RegisterDisposalPayload
            {
                Successful = false,
                Error = "Invalid token."
            };
        }

        var result = await AuthorizationService.CheckAuthorizationAsync(token);
        if (!result.IsValid)
        {
            StatusCode(401);
            return new RegisterDisposalPayload
            {
                Successful = false,
                Error = "Invalid token."
            };
        }

        Console.WriteLine("=======> BEFORE USER EXTRACTION <=======");
        var op = AuthorizationService.ExtractUser(result);
        if (op == null)
        {
            StatusCode(401);
            return new RegisterDisposalPayload
            {
                Successful = false,
                Error = "Invalid token."
            };
        }
        Console.WriteLine("=======> AFTER USER EXTRACTION <=======");

        Console.WriteLine("=======> BEFORE ECOBUCKS PROFILE FETCH <=======");
        var profile = await ProfileRepository.GetByIdAsync(op.Id);
        if (!profile.IsOperator)
        {
            StatusCode(401);
            return new RegisterDisposalPayload
            {
                Successful = false,
                Error = "User is not an operator."
            };
        }
        Console.WriteLine("=======> AFTER ECOBUCKS PROFILE FETCH <=======");

        DisposalClaim disposal = new()
        {
            OperatorId = op.Id,
            Token = Guid.NewGuid().ToString(),
            IsClaimed = false,
            Disposals = input.Disposals,
        };

        try
        {
            Console.WriteLine("=======> BEFORE DISPOSAL DATABASE INSERTION <=======");
            await DisposalRepository.InsertAsync(disposal);
            Console.WriteLine("=======> AFTER DISPOSAL DATABASE INSERTION <=======");
        }
        catch (Exception e)
        {
            Console.WriteLine("=======> ERROR ON DISPOSAL DATABASE INSERTION <=======");
            Logger.LogError(e.Message);

            StatusCode(500);
            return new RegisterDisposalPayload
            {
                Successful = false,
                Error = $"Failed to register disposal on database.\n{e.Message}"
            };
        }

        return new RegisterDisposalPayload
        {
            Successful = true,
            Disposal = disposal
        };
    }

    [HttpPost]
    [Authorize]
    [Route("ecobucks/me/disposals")]
    [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(ClaimDisposalAndCreditsPayload))]
    [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(ClaimDisposalAndCreditsPayload))]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized, Type = typeof(ClaimDisposalAndCreditsPayload))]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError, Type = typeof(ClaimDisposalAndCreditsPayload))]
    public async Task<ClaimDisposalAndCreditsPayload> ClaimDisposalAndCreditsAsync([FromBody] ClaimDisposalAndCreditsInput input)
    {
        var validationResult = await AuthorizationService.CheckAuthorizationAsync(input.UserToken);
        if (!validationResult.IsValid)
        {
            StatusCode(401);
            return new ClaimDisposalAndCreditsPayload
            {
                Successful = false,
                Error = "Invalid token."
            };
        }

        var user = AuthorizationService.ExtractUser(validationResult);
        if (user == null)
        {
            Console.WriteLine("Invalid token. Failed to extract user.");

            StatusCode(401);
            return new ClaimDisposalAndCreditsPayload
            {
                Successful = false,
                Error = "Invalid token."
            };
        }

        var profile = await ProfileRepository.GetByIdAsync(user.Id);

        DisposalClaim disposal;
        try
        {
            var disposalFilter = Builders<DisposalClaim>.Filter.Eq("Token", input.DisposalToken);

            disposal = await DisposalRepository.Collection.Find(disposalFilter).FirstOrDefaultAsync();
            if (disposal is null)
            {
                StatusCode(400);
                return new ClaimDisposalAndCreditsPayload
                {
                    Successful = false,
                    Error = "Invalid disposal token."
                };
            }

            if (disposal.IsClaimed || disposal.UserId is not null)
            {
                StatusCode(400);
                return new ClaimDisposalAndCreditsPayload
                {
                    Successful = false,
                    Error = "Disposal already claimed."
                };
            }

            var disposalUpdate = Builders<DisposalClaim>.Update
                .Set("IsClaimed", true)
                .Set("UserId", user.Id);

            await DisposalRepository.Collection.UpdateOneAsync(disposalFilter, disposalUpdate);


            static (double, string) GetLargestUnit(double grams)
            {
                if (grams < 1000)
                    return (grams, "g");
                else if (grams < 1000000)
                    return (grams / 1000, "kg");
                else
                    return (grams / 1000000, "t");
            }

            (var weight, var unit) = GetLargestUnit(disposal.Weight);

            var transactionDescription =
                $"Disposed {weight}{unit} of {Enum.GetName(disposal.Disposals.FirstOrDefault()!.DisposalType)}"
                + (disposal.Disposals.Length > 1 ? $" and more." : ".");

            Transaction transaction = new()
            {
                TransactionType = TransactionType.Claim,
                UserId = user.Id,
                ClaimId = disposal.Id,
                Credits = disposal.Credits,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Description = transactionDescription
            };


            var profileFilter = Builders<EcobucksProfile>.Filter.Eq(_ => _.Id, user.Id);
            var profileUpdate = Builders<EcobucksProfile>.Update
                .Inc(_ => _.Credits, disposal.Credits)
                .Push(_ => _.Transactions, transaction);

            await ProfileRepository.Collection.UpdateOneAsync(profileFilter, profileUpdate);

        }
        catch (Exception e)
        {
            Console.WriteLine(e);

            StatusCode(500);
            return new ClaimDisposalAndCreditsPayload
            {
                Successful = false,
                Error = $"Failed to claim disposal.\n{e.Message}"
            };
        }

        return new ClaimDisposalAndCreditsPayload
        {
            Successful = true,
            Disposal = disposal
        };
    }

    [Route("/ecobucks/ws")]
    public async Task EcobucksWebSocketAsync()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            var connectionUUID = Guid.NewGuid();

            await EcobucksLocationService.HandleLocationWebSocketAsync(webSocket, connectionUUID);
        }
        else
        {
            HttpContext.Response.StatusCode = 400;
        }
    }
}