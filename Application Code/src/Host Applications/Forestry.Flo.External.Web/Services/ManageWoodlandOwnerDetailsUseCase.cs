using System.Security.Claims;
using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Configuration;
using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using WoodlandOwnerModel = Forestry.Flo.Services.Applicants.Models.WoodlandOwnerModel;

namespace Forestry.Flo.External.Web.Services
{
    /// <summary>
    /// Handles the use case for managing woodland owner details.
    /// </summary>
    public class ManageWoodlandOwnerDetailsUseCase
    {
        private readonly IAuditService<ManageWoodlandOwnerDetailsUseCase> _auditService;
        private readonly IWoodlandOwnerCreationService _creationService;
        private readonly IRetrieveWoodlandOwners _retrievalService;
        private readonly IAgentAuthorityService _agentAuthorityService;
        private readonly IRetrieveUserAccountsService _accountsService;
        private readonly RequestContext _requestContext;
        private readonly ILogger<ManageWoodlandOwnerDetailsUseCase> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly FcAgencyOptions _fcAgencyOptions;

        public ManageWoodlandOwnerDetailsUseCase(
            IWoodlandOwnerCreationService creationService,
            IRetrieveWoodlandOwners retrievalService,
            IAuditService<ManageWoodlandOwnerDetailsUseCase> auditService,
            IAgentAuthorityService agentAuthorityService,
            RequestContext requestContext,
            ILogger<ManageWoodlandOwnerDetailsUseCase> logger,
            IHttpContextAccessor httpContextAccessor,
            IRetrieveUserAccountsService accountsService,
            IOptions<FcAgencyOptions> fcAgencyOptions)
        {
            _creationService = Guard.Against.Null(creationService);
            _retrievalService = Guard.Against.Null(retrievalService);
            _agentAuthorityService = Guard.Against.Null(agentAuthorityService);
            _auditService = Guard.Against.Null(auditService);
            _requestContext = Guard.Against.Null(requestContext);
            _logger = logger;
            _httpContextAccessor = Guard.Against.Null(httpContextAccessor);
            _accountsService = Guard.Against.Null(accountsService);
            _fcAgencyOptions = Guard.Against.Null(fcAgencyOptions.Value);
        }

        public async Task<Result<ManageWoodlandOwnerDetailsModel>> GetWoodlandOwnerModelAsync(
            Guid id,
            ExternalApplicant user,
            CancellationToken cancellationToken)
        {
            var userAccessModel = await _accountsService.RetrieveUserAccessAsync(user.UserAccountId!.Value, cancellationToken);
            if (userAccessModel.IsFailure)
            {
                _logger.LogError("Could not retrieve user access");
                return userAccessModel.ConvertFailure<ManageWoodlandOwnerDetailsModel>();
            }

            var (_, isFailure, woodlandOwner, error) = await _retrievalService.RetrieveWoodlandOwnerByIdAsync(id, userAccessModel.Value, cancellationToken);

            if (isFailure)
            {
                _logger.LogError("Unable to retrieve woodland owner with id {id}, error: {error}", id, error);
                return Result.Failure<ManageWoodlandOwnerDetailsModel>($"Unable to retrieve woodland owner, error: {error}");
            }

            var contactAddress = ModelMapping.ToAddressModel(woodlandOwner.ContactAddress!);
            var organisationAddress = woodlandOwner.IsOrganisation
                ? ModelMapping.ToAddressModel(woodlandOwner.OrganisationAddress!)
                : null;

            return new ManageWoodlandOwnerDetailsModel
            {
                ContactAddress = ModelMapping.ToAddressModel(woodlandOwner.ContactAddress!),
                ContactAddressMatchesOrganisationAddress = woodlandOwner.IsOrganisation && organisationAddress != null && organisationAddress!.Equals(contactAddress),
                ContactEmail = woodlandOwner.ContactEmail!,
                ContactName = woodlandOwner.ContactName,
                ContactTelephoneNumber = woodlandOwner.ContactTelephone,
                IsOrganisation = woodlandOwner.IsOrganisation,
                OrganisationAddress = organisationAddress,
                OrganisationName = woodlandOwner.OrganisationName,
                Id = woodlandOwner.Id!.Value
            };
        }

        /// <summary>
        /// Utilises an implementation of <see cref="IWoodlandOwnerCreationService"/> to update a woodland owner entity.
        /// </summary>
        /// <param name="model">A populated <see cref="ManageWoodlandOwnerDetailsModel"/>.</param>
        /// <param name="user">The logged-in <see cref="ExternalApplicant"/> updating the woodland owner details.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A result indicating whether the entity has been successfully updated.</returns>
        public async Task<Result> UpdateWoodlandOwnerEntityAsync(
            ManageWoodlandOwnerDetailsModel model,
            ExternalApplicant user,
            CancellationToken cancellationToken)
        {
            var woodlandOwnerModel = new WoodlandOwnerModel
            {
                ContactAddress = ModelMapping.ToAddressEntity(model.ContactAddress!),
                ContactEmail = model.ContactEmail,
                ContactName = model.ContactName,
                ContactTelephone = model.ContactTelephoneNumber,
                Id = model.Id,
                IsOrganisation = model.IsOrganisation,
                OrganisationAddress = model.IsOrganisation 
                    ? ModelMapping.ToAddressEntity(model.OrganisationAddress!) 
                    : null,
                OrganisationName = model.OrganisationName
            };

            var (_, isFailure, result, error) = await _creationService.AmendWoodlandOwnerDetailsAsync(woodlandOwnerModel, cancellationToken);

            if (isFailure)
            {
                _logger.LogError("Unable to update woodland owner entity with id {id}, error: {error}", model.Id, error);

                await _auditService.PublishAuditEventAsync(
                    new AuditEvent(
                        AuditEvents.UpdateWoodlandOwnerFailureEvent,
                        model.Id,
                        user.UserAccountId,
                        _requestContext,
                        new
                        {
                            IsOrganisation = model.IsOrganisation,
                            Error = error
                        }
                    ), cancellationToken);

                return Result.Failure("Unable to update woodland owner entity");
            }

            if (result)
            {
                await _auditService.PublishAuditEventAsync(
                    new AuditEvent(
                        AuditEvents.UpdateWoodlandOwnerEvent,
                        model.Id,
                        user.UserAccountId,
                        _requestContext,
                        new
                        {
                            IsOrganisation = model.IsOrganisation
                        }
                    ), cancellationToken);

                if (user.WoodlandOwnerId != null && Guid.Parse(user.WoodlandOwnerId) == model.Id)
                {
                    await UpdateClaimsPrincipalAsync(user, cancellationToken);
                }
            }

            return Result.Success();
        }

        public async Task<Result<bool>> VerifyAgentCanManageWoodlandOwnerAsync(Guid woodlandOwnerId, Guid agencyId, CancellationToken cancellationToken)
        {
            return await _agentAuthorityService.EnsureAgencyAuthorityStatusAsync(
                new EnsureAgencyOwnsWoodlandOwnerRequest(agencyId, woodlandOwnerId),
                [AgentAuthorityStatus.Created, AgentAuthorityStatus.FormUploaded],
                cancellationToken);
        }

        public async Task<Result<bool>> VerifyAgentCanSubmitApplicationAsync(Guid woodlandOwnerId, Guid agencyId, CancellationToken cancellationToken)
        {
            return await _agentAuthorityService.EnsureAgencyAuthorityStatusAsync(
                new EnsureAgencyOwnsWoodlandOwnerRequest(agencyId, woodlandOwnerId),
                [AgentAuthorityStatus.FormUploaded],
                cancellationToken);
        }

        private async Task UpdateClaimsPrincipalAsync(ExternalApplicant user, CancellationToken cancellationToken)
        {
            var existingAccount = await _accountsService.RetrieveUserAccountEntityByIdAsync(user.UserAccountId!.Value, cancellationToken);

            if (existingAccount.IsFailure)
            {
                _logger.LogError("Could not locate user account for logged in user with id {id}, error: {Error}", user.UserAccountId, existingAccount.Error);
                return;
            }

            List<ClaimsIdentity> identities = new()
            {
                ClaimsIdentityHelper.CreateClaimsIdentityFromApplicantUserAccount(existingAccount.Value, _fcAgencyOptions.PermittedEmailDomainsForFcAgent)
            };

            // re-add any identities on the user that are not related to FLO - so primarily the AD B2C stuff
            identities.AddRange(user.Principal.Identities.Where(x => x.AuthenticationType != FloClaimTypes.ClaimsIdentityAuthenticationType));

            user = new ExternalApplicant(new ClaimsPrincipal(identities));

            // update the user on the Http Context
            if (_httpContextAccessor.HttpContext != null)
            {
                await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await _httpContextAccessor.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, user.Principal);
            }
        }
    }
}