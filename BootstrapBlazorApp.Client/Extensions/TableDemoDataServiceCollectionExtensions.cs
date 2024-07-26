using BootstrapBlazor.Components;
using BootstrapBlazorApp.Client.Data;
using Microsoft.Extensions.Localization;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// BootstrapBlazor service extension class
    /// </summary>
    public static class TableDemoDataServiceCollectionExtensions
    {
        /// <summary>
        /// Add PetaPoco database operation service
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddTableDemoDataService(this IServiceCollection services)
        {
            _ = services.AddScoped(typeof(IDataService<>), typeof(TableDemoDataService<>));
            return services;
        }
    }

    /// <summary>
    /// Demo website sample data injection service implementation class
    /// </summary>
    internal class TableDemoDataService<TModel> : DataServiceBase<TModel> where TModel : class, new()
    {
        private static readonly ConcurrentDictionary<Type, Func<IEnumerable<TModel>, string, SortOrder, IEnumerable<TModel>>> SortLambdaCache = new();

        [NotNull]
        private List<TModel>? Items { get; set; }

        private IStringLocalizer<Foo> Localizer { get; set; }

        public TableDemoDataService(IStringLocalizer<Foo> localizer)
        {
            Localizer = localizer;
        }

        /// <summary>
        /// Query operation method
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public override Task<QueryData<TModel>> QueryAsync(QueryPageOptions options)
        {
            // The code here is not usable in actual combat. It is only written for demonstration to prevent all data from being deleted.
            if (Items == null || Items.Count == 0)
            {
                Items = Foo.GenerateFoo(Localizer).Cast<TModel>().ToList();
            }

            var items = Items.AsEnumerable();
            var isSearched = false;
            // Handle advanced queries
            if (options.SearchModel is Foo model)
            {
                if (!string.IsNullOrEmpty(model.Name))
                {
                    items = items.Cast<Foo>().Where(item => item.Name?.Contains(model.Name, StringComparison.OrdinalIgnoreCase) ?? false).Cast<TModel>();
                }

                if (!string.IsNullOrEmpty(model.Address))
                {
                    items = items.Cast<Foo>().Where(item => item.Address?.Contains(model.Address, StringComparison.OrdinalIgnoreCase) ?? false).Cast<TModel>();
                }

                isSearched = !string.IsNullOrEmpty(model.Name) || !string.IsNullOrEmpty(model.Address);
            }

            if (options.Searches.Any())
            {
                // Perform fuzzy query for SearchText
                items = items.Where(options.Searches.GetFilterFunc<TModel>(FilterLogic.Or));
            }

            // filter
            var isFiltered = false;
            if (options.Filters.Any())
            {
                items = items.Where(options.Filters.GetFilterFunc<TModel>());
                isFiltered = true;
            }

            // sort
            var isSorted = false;
            if (!string.IsNullOrEmpty(options.SortName))
            {
                // No sorting is performed externally, sorting is automatically performed internally
                var invoker = SortLambdaCache.GetOrAdd(typeof(Foo), key => LambdaExtensions.GetSortLambda<TModel>().Compile());
                items = invoker(items, options.SortName, options.SortOrder);
                isSorted = true;
            }

            var total = items.Count();

            return Task.FromResult(new QueryData<TModel>()
            {
                Items = items.Skip((options.PageIndex - 1) * options.PageItems).Take(options.PageItems).ToList(),
                TotalCount = total,
                IsFiltered = isFiltered,
                IsSorted = isSorted,
                IsSearch = isSearched
            });
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public override Task<bool> SaveAsync(TModel model, ItemChangedType changedType)
        {
            var ret = false;
            if (model is Foo foo)
            {
                if (changedType == ItemChangedType.Add)
                {
                    var id = Items.Count + 1;
                    while (Items.FirstOrDefault(item => (item as Foo)!.Id == id) != null)
                    {
                        id++;
                    }
                    var item = new Foo()
                    {
                        Id = id,
                        Name = foo.Name,
                        Address = foo.Address,
                        Complete = foo.Complete,
                        Count = foo.Count,
                        DateTime = foo.DateTime,
                        Education = foo.Education,
                        Hobby = foo.Hobby
                    } as TModel;
                    Items.Add(item!);
                }
                else
                {
                    var f = Items.OfType<Foo>().FirstOrDefault(i => i.Id == foo.Id);
                    if (f != null)
                    {
                        f.Name = foo.Name;
                        f.Address = foo.Address;
                        f.Complete = foo.Complete;
                        f.Count = foo.Count;
                        f.DateTime = foo.DateTime;
                        f.Education = foo.Education;
                        f.Hobby = foo.Hobby;
                    }
                }
                ret = true;
            }
            return Task.FromResult(ret);
        }

        public override Task<bool> DeleteAsync(IEnumerable<TModel> models)
        {
            foreach (var model in models)
            {
                _ = Items.Remove(model);
            }

            return base.DeleteAsync(models);
        }
    }
}