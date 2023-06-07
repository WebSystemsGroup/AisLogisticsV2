using GarService;
using Microsoft.AspNetCore.Mvc;

namespace AisLogistics.App.Service
{
    public interface IGarService
    {
        GarFullTextSearchExtendedResponseData SearchAddressExtended(GarFullTextSearchRequestData request);
    }
}
