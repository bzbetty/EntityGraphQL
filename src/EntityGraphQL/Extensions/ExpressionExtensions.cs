﻿using EntityGraphQL.Schema;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace EntityGraphQL.Extensions
{
    public static class ExpressionExtensions
    {
        public static Delegate CompileAndCache(this LambdaExpression expression, DelegateCache? cache, bool enabled)
        {
            if(cache != null && enabled)
            {
                return cache.GetCompiledExpression(expression);                
            }

            return expression.Compile();
        }
    }
}
