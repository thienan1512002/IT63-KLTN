using DevExtreme.AspNet.Data.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json;

// Extend the DevExtreme namespace so all files that already have
// "using DevExtreme.AspNet.Data;" pick up this class for free.
namespace DevExtreme.AspNet.Data
{
    /// <summary>
    /// Concrete subclass of <see cref="DataSourceLoadOptionsBase"/> with a custom
    /// model binder that reads DevExtreme query-string keys (skip, take, sort, filter…)
    /// sent by the dxDataGrid CustomStore.
    /// </summary>
    [ModelBinder(BinderType = typeof(DataSourceLoadOptionsBinder))]
    public class DataSourceLoadOptions : DataSourceLoadOptionsBase
    {
        public static DataSourceLoadOptions FromJsonPayload(IDictionary<string, JsonElement> payload)
        {
            var loadOptions = new DataSourceLoadOptions();
            DataSourceLoadOptionsParser.Parse(
                loadOptions,
                key => payload != null && payload.TryGetValue(key, out var value)
                    ? value.GetRawText()
                    : null);

            loadOptions.StringToLower = true;
            loadOptions.Skip = 0;
            loadOptions.Take = 0;
            loadOptions.RequireTotalCount = false;

            return loadOptions;
        }
    }

    public class DataSourceLoadOptionsBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var loadOptions = new DataSourceLoadOptions();
            DataSourceLoadOptionsParser.Parse(
                loadOptions,
                key => bindingContext.ValueProvider.GetValue(key).FirstOrDefault());

            loadOptions.StringToLower = true; // Optional: makes string comparisons case-insensitive
            bindingContext.Result = ModelBindingResult.Success(loadOptions);
            return Task.CompletedTask;
        }
    }
}
