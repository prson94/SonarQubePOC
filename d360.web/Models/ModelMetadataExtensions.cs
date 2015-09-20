using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;

namespace d360.web.Models
{
    public static class ModelMetadataExtensions
    {
        public static string GetName<TModel, TProperty>(this TModel model, Expression<Func<TModel, TProperty>> ex)
        {
            return ModelMetadata
                .FromLambdaExpression<TModel, TProperty>(ex, new ViewDataDictionary<TModel>(model))
                .DisplayName;
        }
        public static string GetDescription<TModel, TProperty>(this TModel model, Expression<Func<TModel, TProperty>> ex)
        {
            return ModelMetadata
                .FromLambdaExpression<TModel, TProperty>(ex, new ViewDataDictionary<TModel>(model))
                .Description;
        }
    }
}