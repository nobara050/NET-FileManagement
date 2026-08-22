using AutoMapper;
using Drive.Api.Features.DriveItems.List;
using Drive.Application.Common.Models;
using Drive.Application.Features.DriveItems.Models;

namespace Drive.Api.Mapping;

public sealed class ApiMappingProfile : Profile
{
    public ApiMappingProfile()
    {
        CreateMap<DriveItemResult, DriveItemResponse>();
        CreateMap<PagedResult<DriveItemResult>, PagedListDriveItemsResponse>();
    }
}