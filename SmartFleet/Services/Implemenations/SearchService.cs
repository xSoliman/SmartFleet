using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using SmartFleet.Services.Interfaces;

namespace SmartFleet.Services.Implemenations
{
    public class SearchService : ISearchService
    {
        public IQueryable<T> ApplyFilters<T>(IQueryable<T> query, List<Expression<Func<T, bool>>> filters) where T : class
        {
            if (filters == null || filters.Count == 0)
                return query;

            foreach (var filter in filters)
            {
                query = query.Where(filter);
            }
            return query;
        }
    }
} 