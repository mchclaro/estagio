using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Result;
using AutoMapper;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services;
using MediatR;

namespace Application.Aggregates.Client.Commands
{
    public class UpdateClient
    {
        public class Command : IRequest<StandardResult<object>>
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Phone { get; set; }
            public IFormFile PhotoUrl { get; set; }
            public string Zipcode { get; set; }
            public string AddressStreet { get; set; }
            public string AddressStreetNumber { get; set; }
            public string AddressDistrict { get; set; }
            public string AddressState { get; set; }
            public string AddressComplement { get; set; }
            public string AddressCity { get; set; }
        }
        public class Handler : IRequestHandler<Command, StandardResult<object>>
        {
            private readonly IClientRepository _clientRepository;
            private readonly IFileStorageService _fileStorage;
            private readonly IConfiguration _configuration;
            private readonly IMapper _mapper;
            private readonly string _imageBucket;

            public Handler(IClientRepository clientRepository,
                           IConfiguration configuration,
                           IMapper mapper, IFileStorageService fileStorage)
            {
                _clientRepository = clientRepository;
                _configuration = configuration;
                _mapper = mapper;
                _fileStorage = fileStorage;
                _imageBucket = "adp-images";
            }
            public async Task<StandardResult<object>> Handle(Command request, CancellationToken cancellationToken)
            {
                var result = new StandardResult<object> { };
                string photoUrl = "";

                try
                {
                    if (!await _clientRepository.Exists(request.Id))
                    {
                        result.AddError(Code.BadRequest, "Cliente não encontrado no banco de dados.");
                        return result.GetResult();
                    }
                    if (request.Phone == null)
                    {
                        result.AddError(Code.BadRequest, "Não foi possível continuar o cadastro pois o celular é são inválidos.");
                        return result.GetResult();
                    }

                    if (await _clientRepository.IsPhoneInUse(request.Phone))
                    {
                        result.AddError(Code.BadRequest, "Já existe uma conta associada ao celular informado.");
                        return result.GetResult();
                    }
                    if (string.IsNullOrEmpty(request.Name))
                    {
                        result.AddError(Code.BadRequest, "O nome do cliente não pode ser vazio.");
                        return result.GetResult();
                    }
                
                    var entity = _mapper.Map<Command, Domain.Entities.Client>(request);

                    //faz o upload da foto do cliente
                    if (request.PhotoUrl != null)
                    {
                        string photoUuid = Guid.NewGuid().ToString("N");
                        string objectName = $"client_photo_{photoUuid}{Path.GetExtension(request.PhotoUrl.FileName)}";
                        await _fileStorage.UploadFileFromHttpIFormFile(request.PhotoUrl, _imageBucket, objectName);
                        photoUrl = _fileStorage.GetFileUrl(_imageBucket, objectName);

                        if (!string.IsNullOrEmpty(entity.PhotoUrl) && entity.PhotoUrl != photoUrl)
                        {
                            await _fileStorage.DeleteFileFromUrl(entity.PhotoUrl);
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(photoUrl))
                        entity.PhotoUrl = photoUrl;

                    await _clientRepository.Update(entity);

                }
                catch (Exception)
                {
                    if (string.IsNullOrEmpty(photoUrl))
                    {
                        await _fileStorage.DeleteFileFromUrl(photoUrl);
                    }

                    result.AddError(Code.GenericError, "Erro ao atualizar cliente");
                }

                return result.GetResult();
            }
        }
    }
}