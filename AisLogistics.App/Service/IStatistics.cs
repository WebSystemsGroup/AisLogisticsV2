using AisLogistics.App.Models.DTO.Statistics;
using AisLogistics.App.Models.Statistics;
using AisLogistics.App.ViewModels.Statistics;
using DataTables.AspNet.Core;

namespace AisLogistics.App.Service
{
    public interface IStatistics
    {
        Task<List<StatisticsDto>> GetStatisticsAsync();

        Task<StatisticsResponse> GetStatisticsV2Async();

        Task<StatisticsGeneralResponse?> StatisticsGeneral();
        Task<List<StatisticsGeneralGraphic>?> StatisticsGeneralGraphicTypeDay(StatisticsViewRequestModel requestModel);
        Task<List<StatisticsGeneralGraphic>?> StatisticsGeneralGraphicTypeYear(StatisticsViewRequestModel requestModel);
        Task<(List<StatisticsMfcDataResponse>?, int)> StatisticsMfcData(StatisticsViewRequestModel requestModel, IDataTablesRequest request);
        Task<(List<StatisticsMfcDataResponse>?, int)> StatisticsEmployeeData(StatisticsViewRequestModel requestModel, IDataTablesRequest request);
        Task<(List<StatisticsMfcDataResponse>?, int)> StatisticsProviderData(StatisticsViewRequestModel requestModel, IDataTablesRequest request);
        Task<(List<StatisticsMfcDataResponse>?, int)> StatisticsServiceData(StatisticsViewRequestModel requestModel, IDataTablesRequest request);
        Task<(List<StatisticsMfcDataResponse>?, int)> StatisticsCustomerTypeData(StatisticsViewRequestModel requestModel, IDataTablesRequest request);
        Task<(List<StatisticsMfcDataResponse>?, int)> StatisticsServiceTypeData(StatisticsViewRequestModel requestModel, IDataTablesRequest request);
        Task<(List<StatisticsMfcDataResponse>?, int)> StatisticsInteractionTypeData(StatisticsViewRequestModel requestModel, IDataTablesRequest request);
        Task<(List<StatisticsSmevDataResponse>?, int)> StatisticsSmevData(StatisticsViewRequestModel requestModel, IDataTablesRequest request);
        Task<(List<StatisticsSmevDataResponse>?, int)> StatisticsSmevMfcData(StatisticsViewRequestModel requestModel, IDataTablesRequest request);
        Task<(List<StatisticsSmevDataResponse>?, int)> StatisticsSmevEmployeeData(StatisticsViewRequestModel requestModel, IDataTablesRequest request);
        Task<StatisticsServiceGeneralResponse?> StatisticsServicesGeneral();
        Task<Dictionary<string, int>> StatisticsServicesGeneralServiceType();
        Task<Dictionary<string, int>> StatisticsServicesGeneralCustomerType();
        Task<Dictionary<string, int>> StatisticsServicesGeneralInteractionType();
        Task<Dictionary<string, List<string>>?> StatisticsServicesGeneralGraphicTypeDay(StatisticsViewRequestModel requestModel);
        Task<Dictionary<string, List<string>>?> StatisticsServicesGeneralGraphicTypeYear(StatisticsViewRequestModel requestModel);
        Task<Dictionary<string, List<string>>?> StatisticsSmevGraphicTypeDay(StatisticsViewRequestModel requestModel);
        Task<Dictionary<string, List<string>>?> StatisticsSmevGraphicTypeYear(StatisticsViewRequestModel requestModel);
    }
}
