// Copyright (c) Pomelo Foundation. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.Utilities;

namespace EFCore.MySql.Storage.Internal
{
    /// <summary>
    ///     <para>
    ///         Represents the mapping between a .NET <see cref="JsonObject" /> type and a database type.
    ///     </para>
    ///     <para>
    ///         This type is typically used by database providers (and other extensions). It is generally
    ///         not used in application code.
    ///     </para>
    /// </summary>
    public class MySqlJsonTypeMapping : RelationalTypeMapping
    {
        private static readonly Dictionary<Type, ValueConverter> JsonConverters = new Dictionary<Type, ValueConverter>();
        private static readonly Dictionary<Type, ValueComparer> JsonComparers = new Dictionary<Type, ValueComparer>();

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public MySqlJsonTypeMapping(
            Type clrType,
            string storeType = null,
            bool? unicode = null)
            : this(
                new RelationalTypeMappingParameters(
                    new CoreTypeMappingParameters(clrType, GetConverter(clrType), GetComprarer(clrType)),
                    storeType ?? "json",
                    StoreTypePostfix.None,
                    System.Data.DbType.String,
                    unicode ?? false))
        {
        }

        private static ValueConverter GetConverter(Type jsonType)
        {
            var elementType = jsonType.TryGetElementType(typeof(JsonObject<>));
            if (!JsonConverters.TryGetValue(elementType, out var converter))
            {
                converter = (ValueConverter)typeof(JsonToStringConverter<>).MakeGenericType(elementType)
                    .GetDeclaredConstructor(new Type[0]).Invoke(new object[0]);
                JsonConverters[elementType] = converter;
            }

            return converter;
        }

        private static ValueComparer GetComprarer(Type jsonType)
        {
            var elementType = jsonType.TryGetElementType(typeof(JsonObject<>));
            if (!JsonComparers.TryGetValue(elementType, out var converter))
            {
                converter = (ValueComparer)typeof(JsonComparer<>).MakeGenericType(elementType)
                    .GetDeclaredConstructor(new Type[0]).Invoke(new object[0]);
                JsonComparers[elementType] = converter;
            }

            return converter;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected MySqlJsonTypeMapping(RelationalTypeMappingParameters parameters)
            : base(parameters)
        {
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override RelationalTypeMapping Clone(string storeType, int? size)
            => new MySqlJsonTypeMapping(Parameters.WithStoreTypeAndSize(storeType, size));

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override CoreTypeMapping Clone(ValueConverter converter)
            => new MySqlJsonTypeMapping(Parameters.WithComposedConverter(converter));

        /// <summary>
        ///     Generates the escaped SQL representation of a literal value.
        /// </summary>
        /// <param name="literal">The value to be escaped.</param>
        /// <returns>
        ///     The generated string.
        /// </returns>
        protected virtual string EscapeSqlLiteral([NotNull]string literal)
            => Check.NotNull(literal, nameof(literal)).Replace("'", "''");

        /// <summary>
        ///     Generates the SQL representation of a literal value.
        /// </summary>
        /// <param name="value">The literal value.</param>
        /// <returns>
        ///     The generated string.
        /// </returns>
        protected override string GenerateNonNullSqlLiteral(object value)
            => $"'{EscapeSqlLiteral((string)value)}'";

        private class JsonToStringConverter<T> : ValueConverter<JsonObject<T>, string>
            where T : class
        {
            public JsonToStringConverter()
                : base(
                    v => v.ToString(),
                    v => new JsonObject<T>(v))
            {
            }
        }

        private class JsonComparer<T> : ValueComparer<JsonObject<T>>
            where T : class
        {
            public JsonComparer()
                : base((l, r) => Object.Equals(l, r), v => v.GetHashCode())
            {
            }
        }
    }
}
