﻿// Description: EF Bulk Operations & Utilities | Bulk Insert, Update, Delete, Merge from database.
// Website & Documentation: https://github.com/zzzprojects/Entity-Framework-Plus
// Forum: https://github.com/zzzprojects/EntityFramework-Plus/issues
// License: http://www.zzzprojects.com/license-agreement/
// More projects: http://www.zzzprojects.com/
// Copyright (c) 2015 ZZZ Projects. All rights reserved.

using System;
using System.Collections;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;

namespace Z.EntityFramework.Plus
{
    /// <summary>A class for query include filter parent queryable.</summary>
    /// <typeparam name="T">The type of elements of the query.</typeparam>
#if NET45
    public class QueryInterceptorQueryable : IOrderedQueryable, IDbAsyncEnumerable
#else
    public class QueryInterceptorQueryable : IOrderedQueryable
#endif
    {
        public QueryInterceptorQueryable(IQueryable query, ExpressionVisitor[] visitors)
        {
            OriginalQueryable = query;
            Visitors = visitors;
        }

        public ExpressionVisitor[] Visitors { get; set; }

        /// <summary>Gets or sets the internal provider.</summary>
        /// <value>The internal provider.</value>
        public QueryInterceptorProvider InternalProvider { get; set; }

        /// <summary>Gets or sets the original queryable.</summary>
        /// <value>The original queryable.</value>
        public IQueryable OriginalQueryable { get; set; }

        /// <summary>Gets the type of the element.</summary>
        /// <value>The type of the element.</value>
        public Type ElementType
        {
            get { return OriginalQueryable.ElementType; }
        }

        /// <summary>Gets the expression.</summary>
        /// <value>The expression.</value>
        public Expression Expression
        {
            get { return OriginalQueryable.Expression; }
        }

        /// <summary>Gets the provider.</summary>
        /// <value>The provider.</value>
        public IQueryProvider Provider
        {
            get { return InternalProvider ?? (InternalProvider = new QueryInterceptorProvider((IDbAsyncQueryProvider) OriginalQueryable.Provider) {CurrentQueryable = this}); }
        }

        /// <summary>Gets the enumerator.</summary>
        /// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
        /// <returns>The enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return Visit().GetEnumerator();
        }

        public IQueryable Visit()
        {
            var query = OriginalQueryable;
            var expression = OriginalQueryable.Expression;

            foreach (var visitor in Visitors)
            {
                expression = visitor.Visit(expression);
            }

            if (expression != OriginalQueryable.Expression)
            {
                query = OriginalQueryable.Provider.CreateQuery(expression);
            }
            return query;
        }

        public Expression Visit(Expression expression)
        {
            foreach (var visitor in Visitors)
            {
                expression = visitor.Visit(expression);
            }

            return expression;
        }

        public IQueryable Include(string path)
        {
            var objectQuery = OriginalQueryable.GetObjectQuery();
            var objectQueryIncluded = objectQuery.Include(path);
            return new QueryInterceptorQueryable(objectQueryIncluded, Visitors);
        }

#if NET45
        public IDbAsyncEnumerator GetAsyncEnumerator()
        {
            return ((IDbAsyncEnumerable) Visit().GetObjectQuery()).GetAsyncEnumerator();
        }
#endif
    }
}