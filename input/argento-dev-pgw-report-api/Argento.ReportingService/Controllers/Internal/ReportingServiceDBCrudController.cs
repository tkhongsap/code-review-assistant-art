using Argento.ReportingService.DL.Users;
using Argento.ReportingService.Repository;
using Argento.ReportingService.Utility;
using Argento.ReportingService.Utility.Exceptions;
using Arcadia.CrudController;
using Arcadia.CrudController.AspNetCore;
using Arcadia.Repository.EFCore;
using Arcadia.Repository.EFCore.Entities;
using Arcadia.Security.Signature;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using System;
using System.Net;

namespace Argento.ReportingService.Controllers.Internal
{
    public abstract class ReportingServiceDBCrudController<IUnitOfWork, TEntity, TCreateDto, TReadDto, TUpdateDto, TDeleteDto> : CrudController<TEntity, TCreateDto, TReadDto, TUpdateDto, TDeleteDto>
        where TEntity : EntityBase
        where TCreateDto : class
        where TReadDto : class
        where TUpdateDto : class
        where TDeleteDto : class
    {
        protected ReportingServiceDBCrudController(IUnitOfWorkReportingServiceDB unitOfWork, RepositoryConfiguration repositoryConfiguration, IMapper mapper, IConfigurationProvider configurationProvider, CrudControllerConfiguration crudControllerConfiguration) : base(unitOfWork, repositoryConfiguration, mapper, configurationProvider, crudControllerConfiguration)
        {
        }

        protected override object GetRequestedByUserId()
        {
            var requestedUser = (RequestedUser)HttpContext.Items[ArcadiaConstants.RequestScopeKeys.RequestedByUser];
            return requestedUser.Id;
        }

        protected override Exception InitializeException(string errorCode, string errorMessage, Exception exception, HttpStatusCode statusCode)
        {
            return new ArcadiaException(errorCode, errorMessage, exception, statusCode);
        }
    }
}