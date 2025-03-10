﻿using System.Linq.Expressions;
using Application.Common.Models;

namespace UI.Extensions
{
    public static class SearchModelExtensions
    {
        public static string ToQueryString(this SearchModel? model)
        {
            var routes = model?.ToRouteData(true) ?? new();
            return string.Join("&", routes.Select(x => $"{x.Key}={x.Value}"));
        }

        public static Dictionary<string, string> ToRouteData(this SearchModel model)
        {
            return model.ToRouteData(false);
        }

        public static Dictionary<string, string> ToRouteData(this SearchModel model, Dictionary<string, string> itemsToInclude)
        {
            var dictionary = model.ToRouteData();

            foreach (var itemToInclude in itemsToInclude)
            {
                dictionary.Add(itemToInclude.Key, itemToInclude.Value);
            }

            return dictionary;
        }

        private static Dictionary<string, string> ToRouteData(this SearchModel model, bool flattenCollection)
        {
            var dictionary = new Dictionary<string, string>();

            model.Add(x => x.From, dictionary);
            model.Add(x => x.Name, dictionary);
            model.Add(x => x.Postcode, dictionary);
            model.Add(x => x.TuitionType, dictionary);
            model.AddAll(x => x.KeyStages, dictionary, flattenCollection);
            model.AddAll(x => x.Subjects, dictionary, flattenCollection);
            model.Add(x => x.CompareListOrderBy, dictionary);
            model.Add(x => x.CompareListOrderByDirection, dictionary);
            model.Add(x => x.CompareListTuitionType, dictionary);
            model.Add(x => x.CompareListGroupSize, dictionary);
            model.Add(x => x.CompareListShowWithVAT, dictionary);

            return dictionary;
        }

        private static void AddAll<T>(
            this SearchModel model,
            Expression<Func<SearchModel, IEnumerable<T>?>> expression,
            Dictionary<string, string> dictionary,
            bool flattenCollection)
        {
            if (flattenCollection)
            {
                model.Add(expression, BuildQueryString, dictionary);
            }
            else
            {
                var name = expression.MemberName();
                var collection = expression.Compile()(model);

                if (collection is null) return;

                int i = 0;
                foreach (var item in collection)
                {
                    if (item != null)
                    {
                        var itemString = item.ToString();

                        if (!string.IsNullOrWhiteSpace(itemString))
                        {
                            dictionary.Add($"{name}[{i}]", itemString);
                            i++;
                        }
                    }
                }
            }
        }

        private static void Add<T>(
            this SearchModel model,
            Expression<Func<SearchModel, T?>> expression,
            Dictionary<string, string> dictionary)
        {
            model.Add(expression, (prop, _) => prop?.ToString() ?? "", dictionary);
        }

        private static void Add<T>(
            this SearchModel model,
            Expression<Func<SearchModel, T?>> expression,
            Func<T, string, string> buildQueryText,
            Dictionary<string, string> dictionary)
        {
            var name = expression.MemberName();
            var property = expression.Compile()(model);

            if (property is null) return;

            var qs = buildQueryText(property, name);

            if (!string.IsNullOrEmpty(qs))
            {
                dictionary.Add(name, qs);
            }
        }

        private static string MemberName<T>(this Expression<Func<SearchModel, T?>> expression)
            => expression.Body switch
            {
                MemberExpression x when true => x.Member.Name,
                _ => throw new ArgumentException("Only member expressions are supported", nameof(expression)),
            };

        private static string BuildQueryString<T>(IEnumerable<T> data, string name)
        {
            var enumerable = data as T[] ?? data.ToArray();
            var first = enumerable.Take(1).Select(x => x?.ToString());
            var rest = enumerable.Skip(1).Select(x => $"{name}={x}");
            return string.Join("&", first.Union(rest));
        }
    }
}
