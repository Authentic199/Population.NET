<p align="center">
  <img src="icon.png" alt="Population.NET Logo" width="200" />
</p>

<h1 align="center">Population.NET</h1>

# What is Population.NET?

Population.NET is a .NET library designed to simplify complex data manipulation and querying operations. Inspired by the [Populate feature of Strapi in Node.js](https://docs.strapi.io/dev-docs/api/rest/populate-select), it offers robust tools for sorting, filtering, and projecting data, enabling efficient handling of large datasets. With Population.NET, you can seamlessly build powerful and optimized APIs to meet the demands of modern applications.

## Main Features

- **QueryContext**: Provides a common **query params** request class for search APIs.
- **Simple Population**:  Easily retrieve and populate data with a simple and intuitive API, inspired by Strapi's populate feature.
- **Population with Filters, Search, Sort, and Paging**: Combine population capabilities seamlessly with filtering, searching, sorting, and pagination to handle complex data queries efficiently.

---

## Installation

### Using Package Manager

To install **Population.NET** in using Using Package Manager, follow these steps:

1. Open **Visual Studio 2022**.
2. Go to **Tools** -> **NuGet Package Manager** -> **Manage NuGet Packages for Solution...**.
3. Search for `Population.NET` in the Browse tab and install the package.

<p align="left">
  <img src="install.png" alt="NuGet Installation Example" width="600" />
</p>

or

Add the following package reference to your project file:

```xml
<PackageReference Include="Population.NET" Version="1.1.2" />
```

### Using .NET CLI
Alternatively, install it via the .NET CLI:

```bash
dotnet add package Population.NET --version 1.1.2
```

---

## Usage

### Sorting
To sort a collection, use the `SortBuilder` class. Below is an example of sorting a collection by a specific property:

```csharp
using Infrastructure.Facades.Population.Builders;
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
using Infrastructure.Facades.Population.Builders;
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
using Infrastructure.Facades.Population.Builders;
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
using Infrastructure.Facades.Population.Builders;
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
using Infrastructure.Facades.Population.Builders;
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
using Infrastructure.Facades.Population.Builders;
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
