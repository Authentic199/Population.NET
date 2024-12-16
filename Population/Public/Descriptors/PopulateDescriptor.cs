using Population.Definations;
using Population.Internal.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Population.Public.Descriptors;

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

    public List<string> PopulateKeys { get; set; } = [Asterisk];
}
