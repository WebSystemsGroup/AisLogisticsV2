using AisLogistics.DataLayer.Concrete;

namespace AisLogistics.App.Service.Cases
{
    public class CasesIssued : ICasesIssued
    {
        private readonly AisLogisticsContext _context;
        private readonly EmployeeManager _employeeManager;
        public CasesIssued(AisLogisticsContext context, EmployeeManager employeeManager)
        {
            _context = context;
            _employeeManager = employeeManager;
        }
    }
}
