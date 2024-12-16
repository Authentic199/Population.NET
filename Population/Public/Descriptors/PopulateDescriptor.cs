using Infrastructure.Facades.Populates.Definations;
using Infrastructure.Facades.Populates.Internal.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.Facades.Populates.Public.Descriptors;

[ModelBinder(typeof(QueryBinder))]
public class PopulateDescriptor
{
    public PopulateDescriptor()
    {
    }

    public PopulateDescriptor(List<string> populateKeys)
    {
        PopulateKeys = populateKeys;
    }

    public List<string> PopulateKeys { get; set; } = [PopulateConstant.SpecialCharacter.Asterisk];
}
