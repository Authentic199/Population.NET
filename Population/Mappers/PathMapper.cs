using AutoMapper;
using Infrastructure.Facades.Populates.Builders;
using Infrastructure.Facades.Populates.Extensions;
using Infrastructure.Facades.Populates.Public;
using System.Linq.Expressions;
using System.Reflection;

namespace Infrastructure.Facades.Populates.Mappers;

internal static class PathMapper
{
    private static readonly MapperConfiguration Config = new(cfg => cfg.CreateMap<IMetaPathBag, IMetaPathBag>().ConvertUsing<MapPath>());
    private static readonly Mapper Mapper = new(Config);

    /// <summary>
    /// Maps the source <see cref="IMetaPathBag"/> to a new <see cref="IMetaPathBag"/> to prepare it for search operations.
    /// </summary>
    /// <param name="source">The source <see cref="IMetaPathBag"/> to transform.</param>
    /// <returns>
    /// A new <see cref="IMetaPathBag"/> instance with transformed paths, ready for search operations.
    /// </returns>
    /// <remarks>
    /// This method leverages AutoMapper to create a transformed version of the provided <paramref name="source"/>. 
    /// It ensures that paths in the <see cref="IMetaPathBag"/> are converted to the appropriate types and formats
    /// necessary for downstream search processing.
    /// </remarks>
    public static IMetaPathBag Map(IMetaPathBag source)
        => Mapper.Map<IMetaPathBag>(source);
}

internal class MapPath : ITypeConverter<IMetaPathBag, IMetaPathBag>
{
    /// <summary>
    /// Converts the source <see cref="IMetaPathBag"/> to a new <see cref="IMetaPathBag"/> using AutoMapper.
    /// </summary>
    /// <param name="source">The source <see cref="IMetaPathBag"/> to convert.</param>
    /// <param name="destination">The destination <see cref="IMetaPathBag"/>.</param>
    /// <param name="context">The AutoMapper resolution context.</param>
    /// <returns>The converted <see cref="IMetaPathBag"/>.</returns>
    /// <remarks>
    /// This method iterates through each path in the source <see cref="IMetaPathBag"/>, creates a corresponding path in the destination <see cref="IMetaPathBag"/>,
    /// and populates it with transformed information by converting the <see cref="Type"/> property in each <see cref="PathMap"/> of <see cref="PathInfo"/> objects to <see cref="string"/>.
    /// </remarks>
    public IMetaPathBag Convert(IMetaPathBag source, IMetaPathBag destination, ResolutionContext context)
    {
        destination = new MetaPathBag();
        foreach (KeyValuePair<MemberPath, PathInfo> metaPath in source)
        {
            Expression pathMap = metaPath.Value.PathMap;
            MemberInfo destinationMember = metaPath.Value.DestinationInfo;

            if (destinationMember.ResolveMemberType().IsIgnoreSearch())
            {
                destination.TryAdd(metaPath.Key, new PathInfo(pathMap, destinationMember));
                continue;
            }

            destination.TryAdd(metaPath.Key, new PathInfo(pathMap.ConvertStringType(destinationMember), destinationMember));
        }

        return destination;
    }
}
