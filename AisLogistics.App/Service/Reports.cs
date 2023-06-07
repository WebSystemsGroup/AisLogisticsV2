using AisLogistics.App.Models.DTO.Reporpts;
using AisLogistics.App.Models.Enums;
using AisLogistics.App.Models.Reports;
using AisLogistics.App.ViewModels.Reports;
using AisLogistics.DataLayer.Concrete;
using AisLogistics.DataLayer.Entities.FunctionsModel;
using AisLogistics.DataLayer.Entities.Models;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing;
using FastReport;
using FastReport.AdvMatrix;
using FastReport.Utils;
using FastReport.Web;
using Fizzler;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;

namespace AisLogistics.App.Service
{
    public class Reports : IReports
    {
        private readonly AisLogisticsContext _context;
        private HttpClient client;
        public Reports(AisLogisticsContext context)
        {
            _context = context;
            client = new HttpClient();
        }

        public async Task<List<ReportsDto>> GetReportsAsync()
        {
            return await _context.SReports
                .AsNoTracking()
                .Where(w => Debugger.IsAttached || w.IsActive)
                .Select(s => new ReportsDto 
                {
                    Id = s.Id,
                    Name = s.ReportName,
                    SReportGroupId = s.SReportGroupId,
                    SReportGroupName = s.SReportGroups.GroupName,
                    Order = s.ReportOrder,
                    Path = s.ReportPath
                })
                .OrderBy(o=>o.Order)
                .ToListAsync();
        }

        public async Task<ReportsDto?> GetReportAsync(Guid Id)
        {
            return await _context.SReports
                .Where(w => w.Id==Id)
                .Select(s => new ReportsDto
                {
                    Id = s.Id,
                    Name = s.ReportName,
                    SReportGroupId = s.SReportGroupId,
                    SReportGroupName = s.SReportGroups.GroupName,
                    Order = s.ReportOrder,
                    Path = s.ReportPath,
                    Function = s.ReportFunction,
                    File = s.ReportFile
                }).FirstOrDefaultAsync();
        }

        public async Task<SForm?> GetReportsFileAsync(int id)
        {
            return await _context.SForms.SingleOrDefaultAsync(s => s.Id == id);
        }

        public async Task<byte[]> GetReportsFileAsync(Guid id)
        {
            return await _context.SReports.Where(w => w.Id == id).Select(s => s.ReportFile).FirstAsync();
        }


        

        public async Task<IList?> GetDataReport(ReportViewRequestModel requestModel, string connection)
        {
            IList? data = null;
            try
            {
                switch(requestModel.Method)
                {
                    case ReportsType.Function:
                        data = await GetDataFunctionReport(requestModel, connection, Type.GetType(requestModel.ModelType));
                    break;
                    case ReportsType.Sps:
                        data = await GetDataSpsReport(requestModel, connection, Type.GetType(requestModel.ModelType));
                    break;
                }
                return data;
            }
            catch (Exception e)
            {
                return data;
            }
        }


        private async Task<IList?> GetDataSpsReport(ReportViewRequestModel requestModel, string connection, Type type)
        {
            try
            {
                List<object> data = new List<object>();
                var url = $"http://10.8.8.47/api/report/services-counts";

                var query = new Dictionary<string, string>()
                {
                    ["date_from"] = DateTime.Parse(requestModel.DateStart).ToString("yyyy-MM-dd"),
                    ["date_to"] = DateTime.Parse(requestModel.DateStop).ToString("yyyy-MM-dd"),
                };

                var uri = QueryHelpers.AddQueryString(Debugger.IsAttached ? url : connection, query);

                var response = await client.GetAsync(uri);

                if (!response.IsSuccessStatusCode) return null;

                var a = (IList)await response.Content.ReadFromJsonAsync(typeof(List<>).MakeGenericType(type));
                return a;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private async Task<IList?> GetDataFunctionReport(ReportViewRequestModel requestModel, string connection, Type type)
        {
            DbCommand? cmd = null;
            DbDataReader? reader = null;
            try
            {
                cmd = _context.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = connection;

                var parameters = GetParametrs(cmd, requestModel);

                cmd.Parameters.AddRange(parameters);

                await cmd.Connection.OpenAsync();
                reader = await cmd.ExecuteReaderAsync();

                var response = await GetListAsync(reader, type);

                return response;
            }
            catch (Exception e)
            {
                return null;
            }
            finally
            {
                if (reader != null)
                {
                    await reader.CloseAsync();
                    await reader.DisposeAsync();
                }
                if (cmd != null)
                {
                    await cmd.Connection.CloseAsync();
                }
            }
        }

        private static Array GetParametrs(DbCommand dbcommand, ReportViewRequestModel requestModel)
        {
            List<DbParameter> response = new List<DbParameter>();

            if (!string.IsNullOrEmpty(requestModel.DateStart))
            {
                var parametr = dbcommand.CreateParameter();
                parametr.ParameterName = "in_date_start";
                parametr.Value = DateTime.Parse(requestModel.DateStart);
                parametr.DbType = DbType.Date;
                response.Add(parametr);
            }
            if (!string.IsNullOrEmpty(requestModel.DateStop))
            {
                var parametr = dbcommand.CreateParameter();
                parametr.ParameterName = "in_date_stop";
                parametr.Value = DateTime.Parse(requestModel.DateStop);
                parametr.DbType = DbType.Date;
                response.Add(parametr);
            }
            if (requestModel.MfcId.HasValue)
            {
                var parametr = dbcommand.CreateParameter();
                parametr.ParameterName = "in_s_offices_id";
                parametr.Value = requestModel.MfcId;
                parametr.DbType = DbType.Guid;
                response.Add(parametr);
            }
            if (requestModel.MfcIdList!=null)
            {
                var parametr = dbcommand.CreateParameter();
                parametr.ParameterName = "in_s_offices_id";
                parametr.Value = requestModel.MfcIdList.Any(a=>a==Guid.Empty) ? DBNull.Value : requestModel.MfcIdList.ToArray();
                response.Add(parametr);
            }
            if (requestModel.EmployeeId.HasValue)
            {
                var parametr = dbcommand.CreateParameter();
                parametr.ParameterName = "in_s_employees_id";
                parametr.Value = requestModel.EmployeeId;
                parametr.DbType = DbType.Guid;
                response.Add(parametr);
            }
            if (requestModel.EmployeeIdList!=null)
            {
                var parametr = dbcommand.CreateParameter();
                parametr.ParameterName = "in_s_employees_id";
                parametr.Value = requestModel.EmployeeIdList.Any(a => a == Guid.Empty) ? DBNull.Value : requestModel.EmployeeIdList.ToArray();
                response.Add(parametr);
            }
            if (requestModel.ProviderId.HasValue)
            {
                var parametr = dbcommand.CreateParameter();
                parametr.ParameterName = "in_s_offices_id_provider";
                parametr.Value = requestModel.ProviderId;
                parametr.DbType = DbType.Guid;
                response.Add(parametr);
            }
            if (requestModel.ProviderIdList != null)
            {
                var parametr = dbcommand.CreateParameter();
                parametr.ParameterName = "in_s_offices_id_provider";
                parametr.Value = requestModel.ProviderIdList.Any(a => a == Guid.Empty) ? DBNull.Value : requestModel.ProviderIdList.ToArray();
                response.Add(parametr);
            }
            if (requestModel.ServiceId.HasValue)
            {
                var parametr = dbcommand.CreateParameter();
                parametr.ParameterName = "in_s_services_id";
                parametr.Value = requestModel.ServiceId;
                parametr.DbType = DbType.Guid;
                response.Add(parametr);
            }
            if (requestModel.ServiceIdList!=null)
            {
                var parametr = dbcommand.CreateParameter();
                parametr.ParameterName = "in_s_services_id";
                parametr.Value = requestModel.ServiceIdList.Any(a => a == Guid.Empty) ? DBNull.Value : requestModel.ServiceIdList.ToArray();
                response.Add(parametr);
            }
            if (requestModel.SmevId.HasValue)
            {
                var parametr = dbcommand.CreateParameter();
                parametr.ParameterName = "in_s_smev_id";
                parametr.Value = requestModel.SmevId;
                parametr.DbType = DbType.Guid;
                response.Add(parametr);
            }
            if (requestModel.SmevIdList != null)
            {
                var parametr = dbcommand.CreateParameter();
                parametr.ParameterName = "in_s_smev_id";
                parametr.Value = requestModel.SmevIdList.Any(a => a == Guid.Empty) ? DBNull.Value : requestModel.SmevIdList.ToArray();
                response.Add(parametr);
            }
            if (requestModel.SmevRequestId.HasValue)
            {
                var parametr = dbcommand.CreateParameter();
                parametr.ParameterName = "in_s_smev_request_id";
                parametr.Value = requestModel.SmevRequestId;
                parametr.DbType = DbType.Int32;
                response.Add(parametr);
            }
            if (requestModel.SmevRequestIdList!=null)
            {
                var parametr = dbcommand.CreateParameter();
                parametr.ParameterName = "in_s_smev_request_id";
                parametr.Value = requestModel.SmevRequestIdList.Any(a => a == 0) ? DBNull.Value : requestModel.SmevRequestIdList.ToArray();
                response.Add(parametr);
            }

            return response.ToArray();
        }

        private static async Task<IList> GetListAsync(DbDataReader reader, Type type)
        {
            var list = new List<object>();
            while (await reader.ReadAsync())
            {
                //var type = typeof(T);
                var obj = Activator.CreateInstance(type);
                foreach (var prop in type.GetProperties())
                {
                    var value = reader[prop.Name].ToString();
                    if (string.IsNullOrEmpty(value))
                    {
                        prop.SetValue(obj, null);
                    }
                    else
                    {
                        prop.SetValue(obj, reader[prop.Name]);
                    }

                }
                list.Add(obj);
            }
            return (IList)list;
        }
    }
}
