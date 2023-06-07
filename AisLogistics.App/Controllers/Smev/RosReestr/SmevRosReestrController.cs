using AisLogistics.App.Service;
using AisLogistics.App.Settings;
using Microsoft.Extensions.Options;
using SmevGate.Client.Core;
using SmevRouter;

namespace AisLogistics.App.Controllers.Smev.RosReestr
{
    public class SmevRosReestrController : SmevBaseController
    {
        public SmevRosReestrController(ICaseService caseService, ISmevService smevService, IGarService garService, IOptions<SmevSettings> smevOptions)
            : base(caseService, smevService, garService, smevOptions)
        {
        }

        /// <summary>
        /// Запрос на предоставление сведений, содержащихся в ЕГРН, об объектах недвижимости
        /// </summary>
        /// <param name="serviceId"></param>
        /// <param name="smevId"></param>
        /// <param name="request">414</param>
        /// <returns></returns>
        public async Task<ActionResult<string>> RequestRosReestrEgrnInfo(Guid serviceId, int smevId, GetRosReestrEgrnObjectInfoRequestData request)
        {
            if (string.IsNullOrWhiteSpace(request.Declarant.Person.ContactInfo.EMail)
                && string.IsNullOrWhiteSpace(request.Declarant.Person.ContactInfo.PhoneNumber))
            {
                request.Declarant.Person.ContactInfo = null;
            }

            request.DataServicesRequestSmevId = await _smevService.CreateNewSmevRequestAsync(serviceId, smevId,$"{request.CadastralNumber}");
            var response = SmevClient.RequestRosReestrEgrnObjectInfo(request);
            return SmevResponse(response);
        }
        /// <summary>
        /// Запрос на предоставление сведений, содержащихся в ЕГРН, об объектах недвижимости
        /// </summary>
        /// <param name="serviceId"></param>
        /// <param name="smevId"></param>
        /// <param name="request">446</param>
        /// <returns></returns>
        public async Task<ActionResult<string>> RequestRosReestrEgrnSubjectInfo(Guid serviceId, int smevId, GetRosReestrEgrnSubjectInfoRequestData request)
        {
            request.DataServicesRequestSmevId = await _smevService.CreateNewSmevRequestAsync(serviceId, smevId,$"{request.DatePeriodType}");
            var response = SmevClient.RequestRosReestrEgrnSubjectInfo(request);
            return SmevResponse(response);
        }
    }
}
