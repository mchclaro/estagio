using Application.Result;
using AutoMapper;
using Domain.Interfaces.Repositories;
using MediatR;

namespace Application.Aggregates.PaymentMethod.Commands
{
    public class CreatePaymentMethod
    {
        public class Command : IRequest<StandardResult<object>>
        {
            public string Name { get; set; }
            public string Description { get; set; }
        }

        public class Handler : IRequestHandler<Command, StandardResult<object>>
        {
            private readonly IPaymentMethodRepository _paymentMethodRepository;
            private readonly IConfiguration _configuration;
            private readonly IMapper _mapper;

            public Handler(IPaymentMethodRepository paymentMethodRepository,
                           IConfiguration configuration,
                           IMapper mapper)
            {
                _paymentMethodRepository = paymentMethodRepository;
                _configuration = configuration;
                _mapper = mapper;
            }
            public async Task<StandardResult<object>> Handle(Command request, CancellationToken cancellationToken)
            {
                var result = new StandardResult<object> { };

                try
                {
                    if (string.IsNullOrEmpty(request.Name))
                    {
                        result.AddError(Code.BadRequest, "O nome não pode ser vazio.");
                        return result.GetResult();
                    }
                    if (string.IsNullOrEmpty(request.Description))
                    {
                        result.AddError(Code.BadRequest, "A descrição não pode ser vazio.");
                        return result.GetResult();
                    }
                
                    var entity = _mapper.Map<Command, Domain.Entities.PaymentMethod>(request);
                    await _paymentMethodRepository.Create(entity);

                }
                catch (Exception)
                {
                    result.AddError(Code.GenericError, "Erro ao cadastrar o metodo de pagamento");
                }

                return result.GetResult();
            }
        }
    }
}