using AutoMapper;
using Drive.Application.Features.DriveItems.Models;
using Drive.Domain.Entities;

namespace Drive.Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<DriveItem, DriveItemResult>();
    }
}