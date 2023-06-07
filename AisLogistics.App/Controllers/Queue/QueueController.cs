using AisLogistics.App.Hubs;
using AisLogistics.App.Models;
using AisLogistics.App.Models.Queue;
using AisLogistics.App.Service;
using AisLogistics.App.Service.Queue;
using AisLogistics.App.ViewModels.ModelBuilder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using QueueReference;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AisLogistics.App.Controllers.Queue
{
    public class QueueController : AbstractController
    {
        private readonly IReferencesService _referencesService;
        private readonly IElectronicQueueServiceClient _queue;
        private readonly IElectronicQueueServiceRegistrationClient _registration;
        private readonly ILogger<QueueController> _logger;
        private readonly IHubContext<NotificationHub> _hubContext;
        private int debugMfcId = 4; // 
        private string debugIp = "10.40.12.7"; //

        public QueueController(IElectronicQueueServiceClient queue, IElectronicQueueServiceRegistrationClient registration, IReferencesService referencesService,
            EmployeeManager employeeManager, ILogger<QueueController> logger, IHubContext<NotificationHub> hubContext) : base(employeeManager)
        {
            _queue = queue;
            _registration = registration;
            _referencesService = referencesService;
            _logger = logger;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Перенаправление клиента в другое окно
        /// </summary>
        /// <param name="window_id"></param>
        /// <param name="num"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> jsonAbonRedirect(int window_id, string num)
        {
            var employeeId = await _employeeManager.GetIdAsync();

            var model = await _queue.AbonRedirect(new RedirectAbonRequest
            {
                num = num,
                window_id = window_id,
                type_data = "json",
                mfc = Debugger.IsAttached ? debugMfcId : (int)await _employeeManager.GetOfficeQueueIdAsync(),
                operator_guid = employeeId.HasValue ? employeeId.Value.ToString() : string.Empty,
            });
            return Json(model);
        }

        /// <summary>
        /// Отложить абонента
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> jsonDelayAbon(string num)
        {
            var employeeId = await _employeeManager.GetIdAsync();
            var model = await _queue.DelayAbon(new DelayAbonRequest
            {
                num = num,
                type_data = "json",
                time = DateTime.Now.AddMinutes(15).ToShortTimeString(),
                mfc = Debugger.IsAttached ? debugMfcId : (int)await _employeeManager.GetOfficeQueueIdAsync(),
                operator_guid = employeeId.HasValue ? employeeId.Value.ToString() : string.Empty,
            });
            return Json(model);
        }

        /// <summary>
        /// Список окон с их ID
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult> jsonListWindow()
        {
            var model = await _queue.ListWindow(new ListWindowRequest
            {
                type_data = "json",
                mfc = Debugger.IsAttached ? debugMfcId : (int)await _employeeManager.GetOfficeQueueIdAsync(),
            });
            return Json(model);
        }

        /// <summary>
        /// Список отложенных абонентов
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult> jsonListAbonRedirect()
        {
            var model = await _queue.ListAbonRedirect(new ListAbonRedirectRequest
            {
                type_data = "json",
                ip_operator = Debugger.IsAttached ? debugIp : HttpContext.Connection.RemoteIpAddress.ToString(),
                mfc = Debugger.IsAttached ? debugMfcId : (int)await _employeeManager.GetOfficeQueueIdAsync(),
            });

            return Json(model);
        }

        /// <summary>
        /// Список абонентов в очереди
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult> jsonListAbonInQueue()
        {
            var model = await _queue.ListAbonInQueue(new ListAbonInQueueRequest
            {
                type_data = "json",
                mfc = Debugger.IsAttached ? debugMfcId : (int)await _employeeManager.GetOfficeQueueIdAsync(),
            });
            return Json(model);
        }

        /// <summary>
        /// Список отложенных абонентов
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult> jsonListAbonDelay()
        {
            var model = await _queue.ListAbonDelay(new ListAbonDelayRequest
            {
                type_data = "json",
                ip_operator = Debugger.IsAttached ? debugIp : HttpContext.Connection.RemoteIpAddress.ToString(),
                mfc = Debugger.IsAttached ? debugMfcId : (int)await _employeeManager.GetOfficeQueueIdAsync(),
            });
            return Json(model);
        }

        /// <summary>
        /// Номер талона для сотрудника
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult> jsonNumAbonOnWindow()
        {
            var model = await _queue.NumAbonOnWindow(new CountAbonRequestClient
            {
                Ip_operator = Debugger.IsAttached ? debugIp : HttpContext.Connection.RemoteIpAddress.ToString(),
                Mfc = Debugger.IsAttached ? debugMfcId : (int)await _employeeManager.GetOfficeQueueIdAsync(),
            });

            return Json(model);
        }

        /// <summary>
        /// Количество клииентов
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult> jsonCountAbon()
        {
            var model = await _queue.CountAbon(new CountAbonRequestClient
            {
                Ip_operator = Debugger.IsAttached ? debugIp : HttpContext.Connection.RemoteIpAddress.ToString(),
                Mfc = Debugger.IsAttached ? debugMfcId : (int)await _employeeManager.GetOfficeQueueIdAsync(),
            });
            if (model != null)
                model.request_ip = HttpContext.Connection.RemoteIpAddress.ToString();
            return Json(model);
        }

        /// <summary>
        /// Следующий клиент из очереди
        /// </summary>
        /// <param name="EndCall"></param>
        /// <param name="Num"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> jsonNextAbon(int EndCall, string Num)
        {
            var employeeId = await _employeeManager.GetIdAsync();
            var model = await _queue.NextAbon(new NextAbonRequest
            {
                ip_operator = Debugger.IsAttached ? debugIp : HttpContext.Connection.RemoteIpAddress.ToString(),
                mfc = Debugger.IsAttached ? debugMfcId : (int)await _employeeManager.GetOfficeQueueIdAsync(),
                end_call = EndCall,
                num = Num,
                operator_guid = employeeId.HasValue ? employeeId.Value.ToString() : string.Empty,
                type_data = "json"
            });

            var TestModel = new NextAbonResponseClient
            {
                ErrorCode = 0,
                Num = "0",
                id_appointment_type = "1",
                ticket_mfc_uuid = "097B6B28-F677-4225-BC3C-3028B699CF9A",
                ticket_timestamp = "11.06.2020 15:05:54",
                ticket_сalled_timestamp = "11.06.2020 15:18:56",
                window_id = "8"
            };
            //signalRQueue();
            return Json(Debugger.IsAttached ? model : model);
        }

        /// <summary>
        /// Вызов перенаправленого клиента по номеру или по  порядку
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> jsonNextAbonRedirect(string num)
        {
            var employeeId = await _employeeManager.GetIdAsync();
            var model = await _queue.NextAbonRedirect(new NextAbonRedirectRequest
            {
                num = num,
                type_data = "json",
                ip_operator = Debugger.IsAttached ? debugIp : HttpContext.Connection.RemoteIpAddress.ToString(),
                mfc = Debugger.IsAttached ? debugMfcId : (int)await _employeeManager.GetOfficeQueueIdAsync(),
                operator_guid = employeeId.HasValue ? employeeId.Value.ToString() : string.Empty
            });
            return Json(model);
        }

        /// <summary>
        /// Вызов отложенного абонента
        /// </summary>
        /// <param name="Num"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> jsonNextAbonDelay(string Num)
        {
            var employeeId = await _employeeManager.GetIdAsync();
            var model = await _queue.NextAbonDelay(new NextAbonDelayRequest
            {
                type_data = "json",
                ip_operator = Debugger.IsAttached ? debugIp : HttpContext.Connection.RemoteIpAddress.ToString(),
                mfc = Debugger.IsAttached ? debugMfcId : (int)await _employeeManager.GetOfficeQueueIdAsync(),
                num = Num,
                operator_guid = employeeId.HasValue ? employeeId.Value.ToString() : string.Empty
            });
            return Json(model);
        }

        /// <summary>
        /// Вызов заявителя пришедшего за справкой
        /// </summary>
        /// <param name="EndCall"></param>
        /// <param name="Num"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> jsonNextAbonVIP(int EndCall, string Num)
        {
            var employeeId = await _employeeManager.GetIdAsync();
            var model = await _queue.NextAbonVIP(new NextAbonVIPRequest
            {
                type_data = "json",
                ip_operator = Debugger.IsAttached ? debugIp : HttpContext.Connection.RemoteIpAddress.ToString(),
                mfc = Debugger.IsAttached ? debugMfcId : (int)await _employeeManager.GetOfficeQueueIdAsync(),
                num = Num,
                operator_guid = employeeId.HasValue ? employeeId.Value.ToString() : string.Empty,
                end_call = EndCall
            });
            return Json(model);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start_date"></param>
        /// <param name="stop_date"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> jsonFgisMdm(string start_date, string stop_date)
        {
            var model = await _queue.FgisMdm(new FGIS_MDMRequest
            {
                type_data = "json",
                mfc = Debugger.IsAttached ? debugMfcId : (int)await _employeeManager.GetOfficeQueueIdAsync(),
                start_date = start_date,
                stop_date = stop_date,
            });
            return Json(model);
        }

        public async Task<IActionResult?> AddPreRegestrationModal()
        {
            try
            {
                var mfc = await _referencesService.GetOfficesQueueAsync();

                List<PreRegestratonList> date = new List<PreRegestratonList>();
                List<PreRegestratonList> time = new List<PreRegestratonList>();

                var queueId = Debugger.IsAttached ? 1 : (int)await _employeeManager.GetOfficeQueueIdAsync();

                var responce = await _registration.GetDatePreRegistration(queueId);

                int idx = 0;

                if (responce is null) throw new Exception();

                responce?.ForEach(f =>
                {
                    date.Add(new PreRegestratonList { key = f.date_format, value = f.date, idx = idx });
                    time.AddRange(f.time.Select(s => new PreRegestratonList { idx = idx, value = s }));

                    idx++;
                });

                var model = new GetDatePreRegestrationModelDto
                {
                    MFC = new SelectList(mfc, "Id", "OfficeName", queueId).ToList(),
                    Date = date,
                    Time = time
                };

                var modelBuilder = new ModalViewModelBuilder()
                .AddModalType(ModalType.SMALL)
                .AddModalTitle("Предварительная запись")
                .AddModalViewPath("../Shared/Components/QueuePartial/_ModalPreRegistrationsAdd")
                .AddModel(model);

                return ModalLayoutView(modelBuilder);
            }
            catch (Exception ex)
            {
                ShowError(MessageDescription.Error);
                return null;
            }
        }

        public async Task<IActionResult> AddCancelPreRegestrationModal()
        {
            var mfc = await _referencesService.GetOfficesQueueAsync();

            var queueId = Debugger.IsAttached ? 1 : (int)await _employeeManager.GetOfficeQueueIdAsync(); ;

            var model = new RegistrationModelDto
            {
                MFC = new SelectList(mfc, "Id", "OfficeName", queueId).ToList(),
            };

            var modelBuilder = new ModalViewModelBuilder()
            .AddModalType(ModalType.SMALL)
            .AddModalTitle("Предварительная запись")
            .AddModalViewPath("../Shared/Components/QueuePartial/_ModalCanselPreRegistrationsAdd")
            .AddModel(model);

            return ModalLayoutView(modelBuilder);
        }


        [HttpPost]
        public async Task<string?> PreRegestrationModelSave(AddPreRegestrationRequestModel model)
        {
            try
            {
                var request = new PreRegestrationRequestModel
                {
                    date = DateTime.ParseExact(model.date, "yyyy-MM-dd", CultureInfo.GetCultureInfo("ru-RU")).ToShortDateString(),
                    time = model.time,
                    phone = "8" + Regex.Replace(model.phone, @"\+7\(|\)|\s|-", ""),
                    fio = model.fio,
                    service = model.service
                };

                var responce = await _registration.AddPreRegistration(model.queue_id, request);

                return responce;
            }
            catch (Exception ex)
            {
                return null;
            }

        }

        [HttpPost]
        public async Task CancelPreRegestrationModelSave(AddCancelPreRegestrationRequestModel model)
        {
            var request = new PreRegestrationCanselRequestModel
            {
                date = model.date,
                code = model.code,
            };

            var responce = await _registration.AddCacelPreRegistration(model.queue_id, request);

            if (responce) ShowSuccess(MessageDescription.RemoveSuccess);
            else ShowError(MessageDescription.Error);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<JsonResult> GetDatePreRegestration(int id)
        {
            var responce = await _registration.GetDatePreRegistration(id);

            List<PreRegestratonList> date = new List<PreRegestratonList>();
            List<PreRegestratonList> time = new List<PreRegestratonList>();

            int idx = 0;

            responce?.ForEach(f =>
            {
                date.Add(new PreRegestratonList { key = f.date_format, value = f.date, idx = idx });
                time.AddRange(f.time.Select(s => new PreRegestratonList { idx = idx, value = s }));

                idx++;
            });

            var model = new GetDatePreRegestrationModelDto
            {
                Date = date,
                Time = time
            };

            return Json(model);
        }

        [AllowAnonymous]
        public async Task NewAbon(int id, string num)
        {
            _logger?.LogInformation(DateTime.Now.ToString() + "-> NewAbon-> id:" + id + "-> num:" + num);
            try
            {
                var mfc_id = await _referencesService.GetOfficeForQueueAsync(id);
                if (mfc_id != null)
                {
                    var employees = await _referencesService.GetEmployeesForQueueAsync(mfc_id.Id);
                    if (employees is not null and { Count: > 0 })
                    {
                        var users = employees.Select(s => s.Id.ToString()).ToList();
                        await _hubContext.Clients.Users(users).SendAsync("Queue", num);
                    }
                }
            }
            catch
            {
                _logger?.LogError(DateTime.Now.ToString() + "-> NewAbon-> id:" + id + "-> num:" + num);
            }
        }
    }
}
