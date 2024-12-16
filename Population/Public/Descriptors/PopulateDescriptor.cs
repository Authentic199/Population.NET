using Populates.Definations;
using Populates.Internal.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Populates.Public.Descriptors;

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
