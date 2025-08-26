using AutoMapper;
using BookingService.Domain.Daos;
using BookingService.Domain.Dtos;
using BookingService.Domain.Entities;

namespace BookingService.Domain.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Booking, BookingDto>().ReverseMap();
        CreateMap<Booking, CreateBookingDto>().ReverseMap();
        CreateMap<Booking, UpdateBookingDto>().ReverseMap();
        CreateMap<Booking, BookingDao>().ReverseMap();
    }
}
