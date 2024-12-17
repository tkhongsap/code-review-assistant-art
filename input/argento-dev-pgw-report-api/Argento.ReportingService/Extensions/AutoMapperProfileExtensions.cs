using Arcadia.Repository.EFCore.Entities;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Argento.ReportingService.Extensions
{
    public static class AutoMapperProfileExtensions
    {

        private const string AssemblyName = "Argento.ReportingService.Repository";

        public static void CreateMapEntityClasses(this Profile profile)
        {
            Assembly repositoryAssembly = Assembly.Load(AssemblyName);
            IEnumerable<Type> entityTypes = repositoryAssembly.GetExportedTypes()
                .Where(x => x.IsClass && !x.IsAbstract && typeof(EntityBase).IsAssignableFrom(x));
            foreach (Type entityType in entityTypes)
            {
                profile.CreateMap(entityType, entityType);
            }
        }
    }
}