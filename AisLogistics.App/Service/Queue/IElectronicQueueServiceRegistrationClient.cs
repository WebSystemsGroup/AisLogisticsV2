using AisLogistics.App.Models.Queue;

namespace AisLogistics.App.Service.Queue
{
    public interface IElectronicQueueServiceRegistrationClient
    {
        Task<List<GetDatePreRegistetionResponceData>?> GetDatePreRegistration(int id);
        Task<string?> AddPreRegistration(int id, PreRegestrationRequestModel model);
        Task<bool> AddCacelPreRegistration(int id, PreRegestrationCanselRequestModel model);
    }
}
