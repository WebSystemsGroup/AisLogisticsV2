using AisLogistics.App.Service;
using AisLogistics.App.Settings;
using Microsoft.Extensions.Options;
using SmevGate.Client.Core;
using SmevRouter;

namespace AisLogistics.App.Controllers.Smev.MER
{
    public class SmevMerController : SmevBaseController
    {
        public SmevMerController(ICaseService caseService, ISmevService smevService, IGarService garService, IOptions<SmevSettings> smevOptions)
            : base(caseService, smevService, garService, smevOptions)
        {
        }

        /// <summary>
        /// Прием заявлений на зачисление в ДОУ
        /// </summary>
        /// <param name="serviceId"></param>
        /// <param name="smevId">2008</param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<string>> RequestVoteOrderAnnul(Guid serviceId, int smevId, VoteOrderAnnulRequestData request)
        {
            request.DataServicesRequestSmevId = await _smevService.CreateNewSmevRequestAsync(serviceId, smevId,
            $"{request.Applicant.Fio.LastName} {request.Applicant.Fio.FirstName} {request.Applicant.Fio.SecondName}");
            var response = SmevClient.VoteOrderAnnulRequest(request);
            return SmevResponse(response);
        }
    }
}