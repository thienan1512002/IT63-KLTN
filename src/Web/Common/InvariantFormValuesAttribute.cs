using System.Globalization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Web.Common;

// HTML number inputs always submit a dot decimal separator, including with a Vietnamese UI.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class InvariantFormValuesAttribute : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        if (!context.HttpContext.Request.HasFormContentType) return;
        context.ValueProviderFactories.RemoveType<FormValueProviderFactory>();
        context.ValueProviderFactories.Insert(0, new InvariantFormValueProviderFactory());
    }
    public void OnResourceExecuted(ResourceExecutedContext context) { }
    private sealed class InvariantFormValueProviderFactory : IValueProviderFactory
    {
        public async Task CreateValueProviderAsync(ValueProviderFactoryContext context)
        {
            var form = await context.ActionContext.HttpContext.Request.ReadFormAsync(context.ActionContext.HttpContext.RequestAborted);
            context.ValueProviders.Add(new FormValueProvider(BindingSource.Form, form, CultureInfo.InvariantCulture));
        }
    }
}
