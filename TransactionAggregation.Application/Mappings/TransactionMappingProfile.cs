using AutoMapper;
using TransactionAggregation.Contracts;
using TransactionAggregation.Domain;

namespace TransactionAggregation.Application.Mappings;

public class TransactionMappingProfile : Profile
{
    public TransactionMappingProfile()
    {
        
        CreateMap<TransactionMessage, Transaction>()
        
    .ForMember(
        destination => destination.Id,
        options => options.MapFrom(source => source.TransactionId));
    }
}