using AutoMapper;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services;
using Domain.Utils;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Application.Result;
using MediatR;

namespace backend.src.Application.Aggregates.User.Commands
{
    public class CreateUser
    {
        public class Command : IRequest<StandardResult<object>>
        {
            public string Name { get; set; }
            public string Phone { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
            public bool IsActive { get; set; }
            public IFormFile PhotoUrl { get; set; }
            public Role Role { get; set; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Email).EmailAddress().WithMessage("Email inválido.");
            }
        }

        public class Handler : IRequestHandler<Command, StandardResult<object>>
        {
            private readonly IUserRepository _userRepository;
            private readonly IFileStorageService _fileStorage;
            private readonly IConfiguration _configuration;
            private readonly IMapper _mapper;
            private readonly string _imageBucket;

            public Handler(IUserRepository userRepository,
                           IFileStorageService fileStorage,
                           IConfiguration configuration,
                           IMapper mapper)
            {
                _userRepository = userRepository;
                _fileStorage = fileStorage;
                _configuration = configuration;
                _mapper = mapper;
                _imageBucket = "adp-images";
            }

            public async Task<StandardResult<object>> Handle(Command request, CancellationToken cancellationToken)
            {
                var result = new StandardResult<object> { };
                string photoUrl = "";

                try
                {
                    if (request.Phone == null || request.Email == null)
                    {
                        result.AddError(Code.BadRequest, "Não foi possível continuar o cadastro pois o celular e/ou email são inválidos.");
                        return result.GetResult();
                    }

                    // request.Phone = ValidationHelper.RemoveDirtCharsForMobile(request.Phone);

                    if (await _userRepository.IsPhoneInUse(request.Phone) || await _userRepository.IsEmailInUse(request.Email))
                    {
                        result.AddError(Code.BadRequest, "Já existe uma conta associada ao e-mail ou celular informado.");
                        return result.GetResult();
                    }
                    if (string.IsNullOrEmpty(request.Name))
                    {
                        result.AddError(Code.BadRequest, "O nome do usuario não pode ser vazio.");
                        return result.GetResult();
                    }

                    if (string.IsNullOrEmpty(request.Password))
                    {
                        result.AddError(Code.BadRequest, "A senha do usuario não pode ser vazia.");
                        return result.GetResult();
                    }

                    // if (request.PhotoUrl != null && !FileSizeValidationHelper.IsFileSizeAllowed(_configuration, request.PhotoUrl.Length))
                    // {
                    //     result.AddError(Code.BadRequest, "O tamanho da foto excede o limite permitido. Selecione uma foto que possua no máximo 8MB de tamanho.");
                    //     return result.GetResult();
                    // }

                    var entity = _mapper.Map<Command, Domain.Entities.User>(request);

                    //faz o upload da foto do user)
                    if (request.PhotoUrl != null)
                    {
                        string photoUuid = Guid.NewGuid().ToString("N");
                        string objectName = $"user_photo_{photoUuid}{Path.GetExtension(request.PhotoUrl.FileName)}";
                        await _fileStorage.UploadFileFromHttpIFormFile(request.PhotoUrl, _imageBucket, objectName);
                        photoUrl = _fileStorage.GetFileUrl(_imageBucket, objectName);
                    }

                    await _userRepository.Create(entity);
                    
                }
                catch(Exception)
                {
                    if (string.IsNullOrEmpty(photoUrl))
                    {
                        await _fileStorage.DeleteFileFromUrl(photoUrl);
                    }

                    result.AddError(Code.GenericError, "Erro ao cadastrar o usuário");
                }

                return result.GetResult();
            }
        }
    }
}