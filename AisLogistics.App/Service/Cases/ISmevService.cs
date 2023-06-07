using AisLogistics.App.ViewModels.Cases;

namespace AisLogistics.App.Service
{
    public interface ISmevService
    {
        string GetSmevFormById(int id);
        SmevDetailsResponseModel GetSmevResponse(Guid id);
        SmevDetailsResponseModel GetArchiveSmevResponse(Guid id);
        Task<string> CreateNewSmevRequestAsync(Guid servicesId, int smevId, string comment, string idRef = null);
    }
}
