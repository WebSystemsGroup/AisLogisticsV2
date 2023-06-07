using AisLogistics.App.Models.MDM;
using AisLogistics.DataLayer.Concrete;
using AisLogistics.DataLayer.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Globalization;

namespace AisLogistics.App.Service.MDM
{
    public class MdmService : IMdmService
    {
        private readonly AisLogisticsContext _context;

        public MdmService(AisLogisticsContext context)
        {
            _context = context;
        }

        public async Task<List<DMdmObjectsUpload>?> MdmStartedDataServicesObjectsAsync(Guid dServiceId, Guid officeIdMdm, Guid sprServicesSubId, Guid dServiceRouteStageId)
        {

            DateTime d5 = DateTime.Now.AddMinutes(-1);
            DateTime d8 = d5.AddMilliseconds(100);//.AddMinutes(1).AddSeconds(1);
            DateTime d25 = d5.AddMilliseconds(200);//.AddMinutes(2).AddSeconds(3);
            DateTime d11 = d5.AddMilliseconds(300);//.AddMinutes(3).AddSeconds(2);

            List<DMdmObjectsUpload> mdmObjects = new();

            //5 - Начало процесса регистрации запроса на услугу
            var request5 = new MdmReceptionStarted { IdReceptionChannel = 1, ReceptionStartedMfcUuid = dServiceId, ReceptionStartedTimestamp = d5.ToString("yyyy-MM-dd HH:mm:ss.fffzz", new CultureInfo("ru-RU")), TicketResultMfcUuid = null, WindowId = "0" };
            var response5 = await MdmDataServicesObjectsAttributesAsync(request5, d5, 5, officeIdMdm, dServiceId) ?? throw new InvalidOperationException("Ошибка с МДМ объект 5");
            mdmObjects.Add(response5);

            // 8 - Завершение процесса регистрации запроса на услугу
            var request8 = new MdmReceptionEnded { IdReceptionResult = 0, ReceptionStartedMfcUuid = request5.ReceptionStartedMfcUuid, ReceptionEndedMfcUuid = dServiceId, ReceptionEndedTimestamp = d8.ToString("yyyy-MM-dd HH:mm:ss.fffzz", new CultureInfo("ru-RU")), WindowId = "0" };
            var response8 = await MdmDataServicesObjectsAttributesAsync(request8, d8, 8, officeIdMdm, dServiceId) ?? throw new InvalidOperationException("Ошибка с МДМ объект 8");
            mdmObjects.Add(response8);

            // 25 Формирование дела
            var request25 = new MdmApplication { ApplicationMfcUuid = dServiceId, ApplicationTimestamp = d25.ToString("yyyy-MM-dd HH:mm:ss.fffzz", new CultureInfo("ru-RU")), ReceptionEndedMfcUuid = request8.ReceptionEndedMfcUuid, IsComplex = true, FromOtherMfc = false };
            var response25 = await MdmDataServicesObjectsAttributesAsync(request25, d25, 25, officeIdMdm, dServiceId) ?? throw new InvalidOperationException("Ошибка с МДМ объект 25");
            mdmObjects.Add(response25);

            // 11 Взятие услуги в работу работником МФЦ
            var request11 = new MdmServiceProcessingStarted { ServiceProcessingStartedMfcUuid = dServiceId, ApplicationMfsUuid = request25.ApplicationMfcUuid, ServiceProcessingStartedTimestamp = d11.ToString("yyyy-MM-dd HH:mm:ss.fffzz", new CultureInfo("ru-RU")), ServiceMfcUuid = sprServicesSubId };
            var response11 = await MdmDataServicesObjectsAttributesAsync(request11, d11, 11, officeIdMdm, dServiceId) ?? throw new InvalidOperationException("Ошибка с МДМ объект 11");
            mdmObjects.Add(response11);

            return mdmObjects;
        }

        public async Task<List<DMdmObjectsUpload>?> MdmQueryObjectsAsync(Guid dServiceId, Guid officeIdMdm)
        {
            List<DMdmObjectsUpload> mdmObjects = new();

            var request17 = new MdmQuery {QueryMfcUuid = Guid.NewGuid(), QueryTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffzz", new CultureInfo("ru-RU")), QueryType = "1"};
            var response17 = await MdmDataServicesObjectsAttributesAsync(request17, DateTime.Now, 17, officeIdMdm, dServiceId) ?? throw new InvalidOperationException("Ошибка с МДМ объект 17");
            mdmObjects.Add(response17);

            return mdmObjects;
        }

        public async Task<List<DMdmObjectsUpload>?> MdmServiceProcessingHoldObjectsAsync(Guid dServiceId, Guid officeIdMdm)
        {
            List<DMdmObjectsUpload> mdmObjects = new();

            var request18 = new MdmServiceProcessingHold { ServiceProcessingHoldMfcId = dServiceId, ServiceProcessingHoldTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffzz", new CultureInfo("ru-RU")), ServiceProcessingStartedMfcUuid = dServiceId, IdServiceHoldReason = "0"  };
            var response18 = await MdmDataServicesObjectsAttributesAsync(request18, DateTime.Now, 18, officeIdMdm, dServiceId) ?? throw new InvalidOperationException("Ошибка с МДМ объект 18");
            mdmObjects.Add(response18);

            return mdmObjects;
        }

        public async Task<List<DMdmObjectsUpload>?> MdmServiceProcessingEndedObjectsAsync(Guid dServiceId, Guid officeIdMdm, int value, bool isDeadLine, Guid dServiceRouteStageId)
        {
            List<DMdmObjectsUpload> mdmObjects = new();

            var request22 = new MdmServiceProcessingEnded { ApplicationProcessingEndedMfcUuid = dServiceId, ApplicationProcessingEndedTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffzz", new CultureInfo("ru-RU")), ApplicationProcessingEndedCause = value.ToString(new CultureInfo("ru-RU")), DeadlineViolated = isDeadLine, ServicePreocessingStartedMfcUuid = dServiceId };
            var response22 = await MdmDataServicesObjectsAttributesAsync(request22, DateTime.Now, 22, officeIdMdm, dServiceId) ?? throw new InvalidOperationException("Ошибка с МДМ объект 22");
            mdmObjects.Add(response22);

            return mdmObjects;
        }

        public async Task<List<DMdmObjectsUpload>?> MdmServiceResultsReceivedObjectsAsync(Guid dServiceId, Guid officeIdMdm)
        {
            List<DMdmObjectsUpload> mdmObjects = new();

            var request23 = new MdmServiceResultsReceived { ApplicantGotResultMfcUuid = dServiceId, ApplicantGotResultTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffzz", new CultureInfo("ru-RU")), ChanelType = "1", ServiceProcessingEndedMfcUuid = dServiceId };
            var response23 = await MdmDataServicesObjectsAttributesAsync(request23, DateTime.Now, 23, officeIdMdm, dServiceId) ?? throw new InvalidOperationException("Ошибка с МДМ объект 23");
            mdmObjects.Add(response23);

            return mdmObjects;
        }

        private async Task<DMdmObjectsUpload?> MdmDataServicesObjectsAttributesAsync<T>(T request, DateTime date, int type, Guid officeIdMdm, Guid dServiceId)
        {
            try
            {
                var mdmAttributes = await _context.SMdmObjectAttributes.Where(w => w.SMdmObjectTypeV2Id == type).Select(s => new { s.Id, s.ObjectAttributeMnemo }).ToListAsync();

                var json = JsonConvert.SerializeObject(request);
                var dictonary = JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? throw new InvalidOperationException();
                return new DMdmObjectsUpload
                {
                    CreatedDate = date,
                    DServicesId = dServiceId,
                    SOfficeIdMdm = officeIdMdm,
                    SMdmObjectTypeId = type,
                    DMdmObjectsAttributesUploads = mdmAttributes.Select(s => new DMdmObjectsAttributesUpload
                    {
                        SMdmObjectAttributeId = s.Id,
                        MdmAttributeValue = dictonary[s.ObjectAttributeMnemo],
                    }).ToList()
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
