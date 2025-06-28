using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace SmartFleet.Services.Interfaces
{
    public interface ISearchService
    {
        IQueryable<T> ApplyFilters<T>(IQueryable<T> query, List<Expression<Func<T, bool>>> filters) where T : class;
    }
} 