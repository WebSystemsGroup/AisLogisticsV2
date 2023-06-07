using AisLogistics.App.Data;
using AisLogistics.DataLayer.Concrete;
using AisLogistics.DataLayer.Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Quartz.Logging;
using System.Net;
using Z.EntityFramework.Plus;

namespace AisLogistics.App.Service
{
    public class EmployeeManager
    {
        private readonly AisLogisticsContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EmployeeManager(AisLogisticsContext context,
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

       

        private async Task<ApplicationUser> GetApplicationUser()
        {
            var userName = _httpContextAccessor.HttpContext?.User.Identity?.Name;
            if (userName is null) return null;
            return await _userManager.FindByNameAsync(userName);
        }

        public async Task<string?> GetJobAsync(Guid id)
        {
            return await _context.SEmployeesJobPositions.AsNoTracking().Select(s => s.JobPositionName).FirstOrDefaultAsync();
        }

        public async Task AddSEmployeesAuthorizationLog(Guid Id, Guid OfficeId)
        {
            try
            {
                var model = new SEmployeesAuthorizationLog();
                model.SEmployeesId = await _context.SEmployees.Where(w => w.AspNetUserId == Id).Select(s => s.Id).SingleOrDefaultAsync();
                model.SOfficesId = OfficeId;
                model.LogInIp = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
                model.LogInBrowserName = _httpContextAccessor.HttpContext.Request.Headers["User-Agent"].ToString();
                model.LogInDate = DateTime.Now;
                model.LogInProvider = "Логистика";

                await _context.AddAsync(model); ;
                await _context.SaveChangesAsync();
            }
            catch(Exception  ex) 
            { 
                var a = ex; 
                var b = 5; 
            }
        }

        public async Task<Guid?> GetIdAsync()
        {
            var userName = _httpContextAccessor.HttpContext?.User.Identity?.Name;
            if (userName is null) return null;

            var applicationUser = await _userManager.FindByNameAsync(userName);
            var employee = await _context.SEmployees.SingleOrDefaultAsync(s => s.AspNetUserId == applicationUser.Id);
            return employee?.Id;
        }

        public async Task<Guid?> GetIdAsync(ApplicationUser applicationUser)
        {
            var employee = await _context.SEmployees.SingleOrDefaultAsync(s => s.AspNetUserId == applicationUser.Id);
            return employee?.Id;
        }

        public async Task<string?> GetNameAsync()
        {
            var userName = _httpContextAccessor.HttpContext?.User.Identity?.Name;
            if (userName is null) return null;

            var applicationUser = await _userManager.FindByNameAsync(userName);
            var employee = await _context.SEmployees.SingleOrDefaultAsync(s => s.AspNetUserId == applicationUser.Id);
            return employee?.EmployeeName;
        }

        public async Task<Guid?> GetOfficeAsync()
        {
            var applicationUser = await GetApplicationUser();
            var claims = await _userManager.GetClaimsAsync(applicationUser);
            var claimOfficeId = claims.FirstOrDefault(f => f.Type == "OfficeId")?.Value;
            var officeId = new Guid(claimOfficeId);

            return officeId;

            //if (await GetOfficeActiveAsync(applicationUser, officeId)) return officeId;

            //else return null;
        }

        public async Task<Guid?> GetOfficeAsync(ApplicationUser applicationUser)
        {
            var date = DateTime.Today;
            var employee = await _context.SEmployees.Select(s => new
            {
                s.AspNetUserId,
                officeId = s.SEmployeesOfficesJoins.FirstOrDefault(w =>
                    w.DateStart <= date && (w.DateStop == null || w.DateStop >= date) && !w.IsRemove)!.SOfficesId
            })
                .SingleOrDefaultAsync(s => s.AspNetUserId == applicationUser.Id);
            return employee?.officeId;
        }

        public async Task<bool> GetOfficeActiveAsync(ApplicationUser applicationUser, Guid id)
        {
            var date = DateTime.Today;

            return await _context.SEmployeesOfficesJoins
                 .AsNoTracking()
                 .AnyAsync(a => a.SEmployees.AspNetUserId == applicationUser.Id &&
                                a.SOfficesId == id &&
                                a.DateStart <= date && (a.DateStop == null || a.DateStop >= date));


        }

        public async Task<int?> GetOfficeQueueIdAsync()
        {
            var applicationUser = await GetApplicationUser();
            var claims = await _userManager.GetClaimsAsync(applicationUser);
            var claimOfficeId = claims.FirstOrDefault(f => f.Type == "OfficeId")?.Value;
            var officeId = new Guid(claimOfficeId);

            var queueId = await _context.SOffices.Where(w => w.Id == officeId).Select(s => s.QueueId).SingleOrDefaultAsync();

            return queueId != null ? int.Parse(queueId) : 0;
        }


        public async Task<EmployeeInfo> GetFullInfoAsync()
        {
            try
            {
                var userName = _httpContextAccessor.HttpContext?.User.Identity?.Name;
                if (userName is null) return new EmployeeInfo();

                var date = DateTime.Today;
                var applicationUser = await _userManager.FindByNameAsync(userName);
                var claims = await _userManager.GetClaimsAsync(applicationUser);
                var claimOfficeId = claims.FirstOrDefault(f => f.Type == "OfficeId")?.Value;
                var officeId = new Guid(claimOfficeId);
                var employee = await _context.SEmployees
                    .AsNoTracking()
                    .Select(s => new
                    {
                        s.Id,
                        s.EmployeeName,
                        s.AspNetUserId,
                        office = s.SEmployeesOfficesJoins.Where(w => !w.IsRemove).Select(ss => new
                        {
                            ss.SOfficesId,
                            ss.DateStart,
                            ss.DateStop,
                            Job = new EmployeeJobInfo(ss.SEmployeesJobPositionId, ss.SEmployeesJobPosition.JobPositionName),
                            //Office = new EmployeeOfficeInfo(ss.SOfficesId, ss.SOffices.OfficeName, ss.SOffices.OfficeMnemo)
                        }).FirstOrDefault(w => w.DateStart <= date && (w.DateStop == null || w.DateStop >= date))
                    }).SingleOrDefaultAsync(s => s.AspNetUserId == applicationUser.Id);

                var office = await _context.SOffices
                    .AsNoTracking()
                    .Where(w => w.Id == officeId)
                    .Select(s => new EmployeeOfficeInfo(s.Id, s.OfficeName, s.OfficeMnemo))
                    .FirstOrDefaultAsync();

                //TODO что делать если нет офиса?
                return new EmployeeInfo
                {
                    Id = employee.Id,
                    Name = employee?.EmployeeName,
                    Job = employee?.office?.Job,
                    Office = office
                };
            }
            catch(Exception e)
            {
                return new EmployeeInfo();
            }
            
        }
    }

    public class EmployeeInfo
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public EmployeeJobInfo Job { get; set; }
        public EmployeeOfficeInfo Office { get; set; }
    }
    public class EmployeeJobInfo
    {
        public EmployeeJobInfo() { }

        public EmployeeJobInfo(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; set; }
        public string Name { get; set; }
    }
    public class EmployeeOfficeInfo
    {
        public EmployeeOfficeInfo() { }

        public EmployeeOfficeInfo(Guid id, string name, string mnemo)
        {
            Id = id;
            Name = name;
            Mnemo = mnemo;
        }
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Mnemo { get; set; }
    }
    public class EmployeesAuthorizationLog
    {
        public EmployeesAuthorizationLog(Guid sEmployeesId, Guid sOfficesId)
        {
            SEmployeesId = sEmployeesId;
            SOfficesId = sOfficesId;
        }

        public Guid SEmployeesId { get; set; }
        public Guid SOfficesId { get; set; }
        public DateTime LogInDate { get; set; }
        public string LogInBrowserName { get; set; }
        public string LogInBrowserVersion { get; set; }
        public string LogInIp { get; set; }
        public string LogInProvider { get; set; }
    }
}
