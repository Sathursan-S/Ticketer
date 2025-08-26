using AutoMapper;
using TicketService.DTOs;

namespace TicketService.Mappers;

public class MapperProfile : Profile
{
    public MapperProfile()
    {
        CreateMap<CreateTicketRequest, Ticket>();
        CreateMap<Ticket, TicketResponse>();
        CreateMap<Ticket, TicketStatusResponse>();
        CreateMap<TicketStatus, Ticket>();
    }
}