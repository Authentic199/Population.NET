
# Population.NET

Population.NET is a .NET library designed to facilitate complex data manipulation and querying operations. It provides robust tools for sorting, filtering, and projecting data, making it easier to work with large datasets efficiently.

## Features

- **Sorting**: Easily sort collections using various criteria.
- **Filtering**: Apply complex filters to collections with support for multiple logical operators.
- **Projection**: Project data from one type to another using customizable mappings.
- **Integration**: Seamlessly integrates with popular libraries like AutoMapper, Entity Framework Core, and more.

---

## Installation

### Using Package Manager
Add the following package reference to your project file:

```xml
<PackageReference Include="Population.NET-071099" Version="1.0.0" />
```

### Using .NET CLI
Alternatively, install it via the .NET CLI:

```bash
dotnet add package Population.NET-071099 --version 1.0.0
```

---

## Usage

### Sorting
To sort a collection, use the `SortBuilder` class. Below is an example of sorting a collection by a specific property:

```csharp
using Infrastructure.Facades.Populates.Builders;
using Population.Public.Descriptors;
using System.Collections.Generic;

ICollection<SortDescriptor> sortDescriptors = new List<SortDescriptor>
{
    new SortDescriptor("PropertyName", SortOrder.Asc)
};

var sortedQuery = sortDescriptors.BuildSortQuery<MyDocumentType>();
```

---

### Filtering
To filter a collection, use the `FilterBuilder` class. Here’s an example of applying filters:

```csharp
using Infrastructure.Facades.Populates.Builders;
using Population.Public.Descriptors;
using System.Collections.Generic;

ICollection<FilterDescriptor> filterDescriptors = new List<FilterDescriptor>
{
    new FilterDescriptor("PropertyName", "Value", NextLogicalOperator.And, CompareOperator.Equal, "Group")
};

var filteredQuery = filterDescriptors.BuildFilterQuery<MyDocumentType>();
```

---

### Projection
To project data from one type to another, use the `ProjectionBuilder` class:

```csharp
using Infrastructure.Facades.Populates.Builders;
using AutoMapper;
using System.Linq;

var configurationProvider = new MapperConfiguration(cfg => cfg.CreateMap<SourceType, DestinationType>());
var projectionBuilder = new ProjectionBuilder(configurationProvider);

var projectedQuery = projectionBuilder.GetProjection(sourceQueryable, typeof(DestinationType), new[] { "PopulateKey" }, null);
```

---

## Detailed Examples

### Sorting Example: Multiple Properties
Here’s a detailed example for sorting a collection by multiple properties:

```csharp
using Infrastructure.Facades.Populates.Builders;
using Population.Public.Descriptors;
using System.Collections.Generic;

ICollection<SortDescriptor> sortDescriptors = new List<SortDescriptor>
{
    new SortDescriptor("FirstName", SortOrder.Asc),
    new SortDescriptor("LastName", SortOrder.Desc)
};

var sortedQuery = sortDescriptors.BuildSortQuery<MyDocumentType>();
```

---

### Filtering Example: Multiple Conditions
Here’s a detailed example of applying multiple filters:

```csharp
using Infrastructure.Facades.Populates.Builders;
using Population.Public.Descriptors;
using System.Collections.Generic;

ICollection<FilterDescriptor> filterDescriptors = new List<FilterDescriptor>
{
    new FilterDescriptor("Age", "30", NextLogicalOperator.And, CompareOperator.GreaterThan, "Group1"),
    new FilterDescriptor("Country", "USA", NextLogicalOperator.Or, CompareOperator.Equal, "Group2")
};

var filteredQuery = filterDescriptors.BuildFilterQuery<MyDocumentType>();
```

---

### Projection Example: Specific Populate Keys
Projecting data while specifying populate keys:

```csharp
using Infrastructure.Facades.Populates.Builders;
using AutoMapper;
using System.Linq;

var configurationProvider = new MapperConfiguration(cfg => cfg.CreateMap<SourceType, DestinationType>());
var projectionBuilder = new ProjectionBuilder(configurationProvider);

var projectedQuery = projectionBuilder.GetProjection(sourceQueryable, typeof(DestinationType), new[] { "FirstName", "LastName" }, null);
```

---

## Contributing

Contributions are welcome! Feel free to submit a pull request or open an issue to discuss any changes or improvements.

---

## License

This project is licensed under the **MIT License**. See the [LICENSE](LICENSE) file for more details.

---

## Authors

- **Authentic**

---

## Acknowledgements

Population.NET seamlessly integrates with:

- [AutoMapper](https://automapper.org/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [MassTransit](https://masstransit-project.com/)
- [Swashbuckle](https://github.com/domaindrivendev/Swashbuckle)

---

*For more detailed documentation and examples, refer to the official documentation.*
