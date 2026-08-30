using AutoMapper;
using TransactionAggregation.Contracts;
using TransactionAggregation.Domain;
using TransactionAggregation.Contracts.Transactions;
namespace TransactionAggregation.Application.Mappings;

public class TransactionMappingProfile : Profile
{
    public TransactionMappingProfile()
    {
        
        CreateMap<TransactionMessage, Transaction>()

    .ForMember(
        destination => destination.Id,
        options => options.MapFrom(source => source.TransactionId));

    CreateMap<Transaction, TransactionResponseDto>()
            .ForMember(
                destination => destination.PaymentMethod,
                options => options.MapFrom(
                    source => source.PaymentMethod.ToString()))
            .ForMember(
                destination => destination.Direction,
                options => options.MapFrom(
                    source => source.Direction.ToString()));
    }
}