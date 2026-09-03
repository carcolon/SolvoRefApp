using System.Net;
using Core.Contracts.Fabric;
using Core.Contracts.Identity;
using Core.Models.Global;
using MediatR;

namespace Core.Feature.Identity.UpdateUsersExtraData
{
    public class UpdateUsersExtraDataRequestHandler : IRequestHandler<UpdateUsersExtraDataRequest, Response<Unit>>
    {
        private readonly IUserService _userService;
        private readonly IFabricService _fabricService;

        public UpdateUsersExtraDataRequestHandler(IUserService userService, IFabricService fabricService)
        {
            _userService = userService;
            _fabricService = fabricService;
        }

        public async Task<Response<Unit>> Handle(UpdateUsersExtraDataRequest request, CancellationToken cancellationToken)
        {
            var users = await _userService.GetUsers();
            var emails = users.Select(x => x.Email).ToList();
            var fabricData = await _fabricService.GetExtraUserInformation(emails);
            if (!fabricData.Success)
            {
                return Response<Unit>.ErrorResponse(fabricData.Errors, fabricData.StatusCode);
            }
            foreach (var user in users)
            {
                var userToUpdate = fabricData.Data.FirstOrDefault(x => x.Email == user.Email);
                if (userToUpdate != null)
                {
                    user.PayrollCompany = userToUpdate.PayrollCompany;
                    user.Country = userToUpdate.Country;
                    user.PersonalId = userToUpdate.PersonalId;
                    user.PaymentFrequency = userToUpdate.PayrollFrequencyClassification;
                    user.SolId = userToUpdate.SolId;
                    user.Status = userToUpdate.Status;
                    await _userService.UpdateUsers(user);
                }
            }
            return Response<Unit>.SuccessResponse(Unit.Value, HttpStatusCode.OK);
        }
    }
}
