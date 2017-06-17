using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EntityFramework_Repository.Models
{
    public class Repository_Helper
    {
        public static string GetTableName(Type type, DbContext context)
        {
            var metadata = ((IObjectContextAdapter)context).ObjectContext.MetadataWorkspace;

            // Get the part of the model that contains info about the actual CLR types
            var objectItemCollection = ((ObjectItemCollection)metadata.GetItemCollection(DataSpace.OSpace));

            // Get the entity type from the model that maps to the CLR type
            var entityType = metadata
                    .GetItems<EntityType>(DataSpace.OSpace)
                    .Single(e => objectItemCollection.GetClrType(e) == type);

            // Get the entity set that uses this entity type
            var entitySet = metadata
                .GetItems<EntityContainer>(DataSpace.CSpace)
                .Single()
                .EntitySets
                .Single(s => s.ElementType.Name == entityType.Name);

            // Find the mapping between conceptual and storage model for this entity set
            var mapping = metadata.GetItems<EntityContainerMapping>(DataSpace.CSSpace)
                    .Single()
                    .EntitySetMappings
                    .Single(s => s.EntitySet == entitySet);

            // Find the storage entity set (table) that the entity is mapped
            var table = mapping
                .EntityTypeMappings.Single()
                .Fragments.Single()
                .StoreEntitySet;

            // Return the table name from the storage entity set
            return (string)table.MetadataProperties["Table"].Value ?? table.Name;
        }
        public static string GetColumnName(Type type, DbContext context, string propName)
        {
            var metadata = ((IObjectContextAdapter)context).ObjectContext.MetadataWorkspace;

            //// Get the part of the model that contains info about the actual CLR types
            var objectItemCollection = ((ObjectItemCollection)metadata.GetItemCollection(DataSpace.OSpace));

            // Get the entity type from the model that maps to the CLR type
            var entityType = metadata
                    .GetItems<EntityType>(DataSpace.OSpace)
                    .Single(e => objectItemCollection.GetClrType(e) == type);

            // Get the entity set that uses this entity type
            var entitySet = metadata
                .GetItems<EntityContainer>(DataSpace.CSpace)
                .Single()
                .EntitySets
                .Single(s => s.ElementType.Name == entityType.Name);

            // Find the mapping between conceptual and storage model for this entity set
            var mapping = metadata.GetItems<EntityContainerMapping>(DataSpace.CSSpace)
                    .Single()
                    .EntitySetMappings
                    .Single(s => s.EntitySet == entitySet);

            // Find the storage entity set (table) that the entity is mapped
            var table = mapping
                .EntityTypeMappings.Single()
                .Fragments.Single()
                .StoreEntitySet;

            // Return the table name from the storage entity set
            var tableName = table.MetadataProperties["Table"].Value ?? table.Name;

            // Find the storage property (column) that the property is mapped
            var columnName = mapping
                .EntityTypeMappings.Single()
                .Fragments.Single()
                .PropertyMappings
                .OfType<ScalarPropertyMapping>()
                      .Single(m => m.Property.Name == propName)
                .Column
                .Name;

            // Return the columnName name from the storage entity set
            return columnName;
        }
        public static string GetPropertyName(Type type, DbContext context, string colName)
        {
            var metadata = ((IObjectContextAdapter)context).ObjectContext.MetadataWorkspace;

            //// Get the part of the model that contains info about the actual CLR types
            var objectItemCollection = ((ObjectItemCollection)metadata.GetItemCollection(DataSpace.OSpace));

            // Get the entity type from the model that maps to the CLR type
            var entityType = metadata
                    .GetItems<EntityType>(DataSpace.OSpace)
                    .Single(e => objectItemCollection.GetClrType(e) == type);

            // Get the entity set that uses this entity type
            var entitySet = metadata
                .GetItems<EntityContainer>(DataSpace.CSpace)
                .Single()
                .EntitySets
                .Single(s => s.ElementType.Name == entityType.Name);

            // Find the mapping between conceptual and storage model for this entity set
            var mapping = metadata.GetItems<EntityContainerMapping>(DataSpace.CSSpace)
                    .Single()
                    .EntitySetMappings
                    .Single(s => s.EntitySet == entitySet);

            // Find the storage entity set (table) that the entity is mapped
            var table = mapping
                .EntityTypeMappings.Single()
                .Fragments.Single()
                .StoreEntitySet;

            // Return the table name from the storage entity set
            var tableName = table.MetadataProperties["Table"].Value ?? table.Name;

            // Find the EF Entity that the storage property (column) is mapped to
            var propName = mapping
                .EntityTypeMappings.Single()
                .Fragments.Single()
                .PropertyMappings
                .OfType<ScalarPropertyMapping>()
                        .Single(m => m.Column.Name == colName)
                //.Single(m => m.Property.Name == propName)
                .Property
                .Name;

            // Return the property name from the EF Entity set
            return propName;
        }
    }
    public static class StringExtensions
    {
        public static string SafeReplace(this string input, string find, string replace, bool matchWholeWord)
        {
            string textToFind = matchWholeWord ? string.Format(@"\b{0}\b", find) : find;
            return Regex.Replace(input, textToFind, replace);
        }
    }
    public static class QuickMapper
    {
        #region Extension Methods
        public static TDestination MapTo<TDestination>(this object Source, params string[] includeProperties)
            where TDestination : class, new()
        {
            return Map(Source, new TDestination(), ConvertStrings(includeProperties));
        }

        public static object MapTo(this object Source, Type DestinationType, params string[] includeProperties)
        {
            return Map(Source, System.Activator.CreateInstance(DestinationType, _ctorParams), ConvertStrings(includeProperties));
        }

        public static TDestination MapTo<TDestination>(this object Source, TDestination Destination, params string[] includeProperties)
            where TDestination : class, new()
        {
            return Map(Source, Destination, ConvertStrings(includeProperties));
        }

        public static T2 Map<T1, T2>(T1 Source, T2 Destination, params string[] includeProperties)
            where T1 : class
            where T2 : class
        {
            var includes = ConvertStrings(includeProperties);
            if (Source != null && Destination != null)
            {
                if ((Source.GetType().Name == "ICollection`1" || Source.GetType().GetInterface("ICollection`1") != null) &&
                    (Destination.GetType().Name == "ICollection`1" || Destination.GetType().GetInterface("ICollection`1") != null))
                {
                    var destinationGenericType = Destination.GetType();
                    var destinationGenericArg = destinationGenericType.GetGenericArguments()[0];
                    return DoMapCollection(Source, Destination, destinationGenericArg, includes);
                }
                return DoMap(Source, Destination, includes);
            }
            else
            {
                return Source != null ? Destination : null;
            }
        }

        public static TDestination MapTo<TDestination>(this object Source, params Expression<Func<TDestination, object>>[] includeProperties)
            where TDestination : class, new()
        {
            return Map(Source, new TDestination(), GetIncludeNames(includeProperties));
        }

        public static TDestination MapTo<TDestination>(this object Source, TDestination Destination, params Expression<Func<TDestination, object>>[] includeProperties)
            where TDestination : class, new()
        {
            return Map(Source, Destination, GetIncludeNames(includeProperties));
        }

        public static TDestination MapTo<TDestination>(this object Source)
            where TDestination : class, new()
        {
            return Map(Source, new TDestination());
        }

        public static TDestination MapTo<TDestination>(this object Source, TDestination Destination)
            where TDestination : class, new()
        {
            return Map(Source, Destination);
        }

        public static T2 Map<T1, T2>(T1 Source, T2 Destination)
            where T1 : class
            where T2 : class
        {
            if (Source != null && Destination != null)
            {
                if ((Source.GetType().Name == "ICollection`1" || Source.GetType().GetInterface("ICollection`1") != null) &&
                    (Destination.GetType().Name == "ICollection`1" || Destination.GetType().GetInterface("ICollection`1") != null))
                {
                    var destinationGenericType = Destination.GetType();
                    var destinationGenericArg = destinationGenericType.GetGenericArguments()[0];
                    return DoMapCollection(Source, Destination, destinationGenericArg, new List<IncludePropertiesInfo>().ToArray());
                }
                return DoMap(Source, Destination, new List<IncludePropertiesInfo>().ToArray());
            }
            else
            {
                return Source != null ? Destination : null;
            }
        }

        public static object MapToInclude<TSource>(this TSource Source, params Expression<Func<TSource, object>>[] includeProperties)
        {
            return null;
        }

        public static object MapToExclude<TSource>(this TSource Source)
        {
            return null;
        }

        public static T2 Map<T1, T2>(T1 Source, T2 Destination, params Expression<Func<T2, object>>[] includeProperties)
            where T1 : class
            where T2 : class
        {
            var includes = GetIncludeNames(includeProperties);
            if (Source != null && Destination != null)
            {
                if ((Source.GetType().Name == "ICollection`1" || Source.GetType().GetInterface("ICollection`1") != null) &&
                    (Destination.GetType().Name == "ICollection`1" || Destination.GetType().GetInterface("ICollection`1") != null))
                {
                    var destinationGenericType = Destination.GetType();
                    var destinationGenericArg = destinationGenericType.GetGenericArguments()[0];
                    return DoMapCollection(Source, Destination, destinationGenericArg, includes);
                }
                return DoMap(Source, Destination, includes);
            }
            else
            {
                return Source != null ? Destination : null;
            }
        }
        #endregion // Extension Methods

        #region Private Members
        private class IncludePropertiesInfo
        {
            public string PropertyName { get; set; }
            public bool Exclude { get; set; }
            public IncludePropertiesInfo[] ChildIncludeProperties { get; set; }
        }

        static private object[] _ctorParams = new object[] { };

        private static T2 Map<T1, T2>(T1 Source, T2 Destination, params IncludePropertiesInfo[] includeProperties)
            where T1 : class
            where T2 : class, new()
        {
            if (Source != null && Destination != null)
            {
                if ((Source.GetType().Name == "ICollection`1" || Source.GetType().GetInterface("ICollection`1") != null) &&
                    (Destination.GetType().Name == "ICollection`1" || Destination.GetType().GetInterface("ICollection`1") != null))
                {
                    var destinationGenericType = Destination.GetType();
                    var destinationGenericArg = destinationGenericType.GetGenericArguments()[0];
                    return DoMapCollection(Source, Destination, destinationGenericArg, includeProperties);
                }
                return DoMap(Source, Destination, includeProperties);
            }
            else
            {
                return Source != null ? Destination : null;
            }
        }

        private static IncludePropertiesInfo[] GetIncludeNames(dynamic includeProperties)
        {
            var includes = new List<IncludePropertiesInfo>();
            var newArrayExprerssion = includeProperties as NewArrayExpression;
            if (newArrayExprerssion == null)
            {
                foreach (var property in includeProperties)
                {
                    includes.Add(CreateIncludePropertyInfo(property));
                }
            }
            else
            {
                foreach (var property in newArrayExprerssion.Expressions)
                {
                    dynamic prop = property;
                    includes.Add(CreateIncludePropertyInfo(prop.Operand));
                }
            }
            return includes.ToArray();
        }

        private static IncludePropertiesInfo CreateIncludePropertyInfo(dynamic property)
        {
            if (property.Body.NodeType == ExpressionType.Call)
            {
                MethodCallExpression methodbody = property.Body as MethodCallExpression;
                if (methodbody.Method.DeclaringType.Name.Equals("QuickMapper"))
                {
                    dynamic mainMemberExpr = methodbody.Arguments.FirstOrDefault();
                    string mainMemberName = null;
                    if (mainMemberExpr is MemberExpression)
                        mainMemberName = mainMemberExpr.Member.Name;
                    else if (mainMemberExpr is UnaryExpression)
                        mainMemberName = ((MemberExpression)(mainMemberExpr.Body).Operand).Member.Name;

                    if (methodbody.Method.Name.Equals("MapToInclude"))
                    {
                        var childIncludeProperties = GetIncludeNames(methodbody.Arguments.LastOrDefault());
                        return new IncludePropertiesInfo { PropertyName = mainMemberName, ChildIncludeProperties = childIncludeProperties };
                    }
                    else if (methodbody.Method.Name.Equals("MapToExclude"))
                    {
                        return new IncludePropertiesInfo { PropertyName = mainMemberName, Exclude = true };
                    }
                }
            }

            MemberExpression body = (property.Body.NodeType == ExpressionType.Convert) ? (MemberExpression)((UnaryExpression)property.Body).Operand : (MemberExpression)property.Body;
            return new IncludePropertiesInfo { PropertyName = body.Member.Name };
        }

        private static IncludePropertiesInfo[] ConvertStrings(string[] includeProperties)
        {
            var includes = new List<IncludePropertiesInfo>();
            foreach (var property in includeProperties)
            {
                includes.Add(new IncludePropertiesInfo { PropertyName = property });
            }
            return includes.ToArray();
        }

        private static dynamic DoMapCollection(dynamic source, dynamic destination, Type destinationGenericArg, params IncludePropertiesInfo[] includeProperties)
        {
            if (destinationGenericArg.GetConstructor(Type.EmptyTypes) != null)
            {
                foreach (var src in source)
                {
                    var dest = System.Activator.CreateInstance(destinationGenericArg, _ctorParams);
                    var method = destination.GetType().GetMethod("Add");
                    method.Invoke(destination, new object[] { DoMap(src, dest, includeProperties) });
                }
            }
            return destination;
        }

        private static dynamic DoMap(object source, object destination, params IncludePropertiesInfo[] includeProperties)
        {
            foreach (var destinationProperty in destination.GetType().GetProperties())
            {
                if (destinationProperty.SetMethod == null)
                    continue;

                var sourceProperty = source.GetType().GetProperty(destinationProperty.Name);

                if (sourceProperty == null)
                    continue;

                if (includeProperties.Any() && includeProperties.Where(x => x.PropertyName == destinationProperty.Name).Any())
                {
                    var includeProp = includeProperties.Where(x => x.PropertyName == destinationProperty.Name).FirstOrDefault();

                    if (includeProp.Exclude)
                        continue;

                    if (destinationProperty.PropertyType.GetConstructor(Type.EmptyTypes) != null)
                    {
                        var sourceValue = sourceProperty.GetValue(source);
                        var destinationMapValue = System.Activator.CreateInstance(destinationProperty.PropertyType, _ctorParams);
                        var childIncludeProperties = includeProp.ChildIncludeProperties;
                        if (childIncludeProperties != null)
                            destinationProperty.SetValue(destination, Map(sourceValue, destinationMapValue, childIncludeProperties));
                        else
                            destinationProperty.SetValue(destination, Map(sourceValue, destinationMapValue));
                        continue;
                    }
                }

                if (sourceProperty.PropertyType == destinationProperty.PropertyType)
                {
                    var sourceValue = sourceProperty.GetValue(source);
                    destinationProperty.SetValue(destination, sourceValue);
                    continue;
                }
            }
            return destination;
        }
        #endregion
    }

}
