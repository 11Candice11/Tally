using AutoMapper;
using InvoiceTrackerAPI2.DTOs;
using InvoiceTrackerAPI2.Models;

namespace InvoiceTrackerAPI2.Mappings;

// AutoMapper profile that defines how models map to DTOs
// computed fields like Subtotal and Total are calculated here not stored in the db
public class InvoiceMappingProfile : Profile
{
    public InvoiceMappingProfile()
    {
        // compute using LINQ expressions
        CreateMap<LineItem, LineItemDto>()
            .ForMember(d => d.Total, o => o.MapFrom(s => s.Qty * s.UnitPrice));

        CreateMap<Invoice, InvoiceDto>()
            .ForMember(d => d.Subtotal,  o => o.MapFrom(s => s.LineItems.Sum(l => l.Qty * l.UnitPrice)))
            .ForMember(d => d.VatAmount, o => o.MapFrom(s => s.LineItems.Sum(l => l.Qty * l.UnitPrice) * s.VatRate))
            .ForMember(d => d.Total,     o => o.MapFrom(s =>
                s.LineItems.Sum(l => l.Qty * l.UnitPrice) * (1 + s.VatRate) - s.Discount + s.LateFee));

        // explicitly ignore Id, UserId, InvoiceNumber and User
        // we dont want client setting their own IDs and stuff
        CreateMap<CreateInvoiceDto, Invoice>()
            .ForMember(d => d.Id,            o => o.Ignore())
            .ForMember(d => d.UserId,        o => o.Ignore())
            .ForMember(d => d.InvoiceNumber, o => o.Ignore())
            .ForMember(d => d.User,          o => o.Ignore());

        // null fields are skipped — only provided fields are applied
        CreateMap<UpdateInvoiceDto, Invoice>()
            .ForMember(d => d.Id,            o => o.Ignore())
            .ForMember(d => d.UserId,        o => o.Ignore())
            .ForMember(d => d.InvoiceNumber, o => o.Ignore())
            .ForMember(d => d.User,          o => o.Ignore())
            .ForMember(d => d.Status,        o => o.Ignore())
            .ForAllMembers(o => o.Condition((_, _, srcVal) => srcVal is not null));

        CreateMap<CreateLineItemDto, LineItem>()
            .ForMember(d => d.Id,        o => o.Ignore())
            .ForMember(d => d.InvoiceId, o => o.Ignore())
            .ForMember(d => d.Invoice,   o => o.Ignore());
    }
}
