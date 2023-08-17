namespace BuildingBlocks.Mongo;

using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

public class ImmutablePocoConvention : ConventionBase, IClassMapConvention
{
    private readonly BindingFlags _bindingFlags;

    public ImmutablePocoConvention()
        : this(BindingFlags.Instance | BindingFlags.Public)
    {
    }

    public ImmutablePocoConvention(BindingFlags bindingFlags)
    {
        _bindingFlags = bindingFlags | BindingFlags.DeclaredOnly;
    }

    public void Apply(BsonClassMap classMap)
    {
        var readOnlyProperties = classMap.ClassType.GetTypeInfo()
            .GetProperties(_bindingFlags)
            .Where(p => IsReadOnlyProperty(classMap, p))
            .ToList();

        foreach (var constructor in classMap.ClassType.GetConstructors())
        {
            var matchProperties = GetMatchingProperties(constructor, readOnlyProperties);
            if (matchProperties.Any())
            {
                classMap.MapConstructor(constructor);

                foreach (var p in matchProperties)
                {
                    classMap.MapMember(p);
                }
            }
        }
    }

    private static List<PropertyInfo> GetMatchingProperties(
        ConstructorInfo constructor,
        List<PropertyInfo> properties)
    {
        var matchProperties = new List<PropertyInfo>();

        var ctorParameters = constructor.GetParameters();
        foreach (var ctorParameter in ctorParameters)
        {
            var matchProperty = properties.FirstOrDefault(p => ParameterMatchProperty(ctorParameter, p));
            if (matchProperty == null)
            {
                return new List<PropertyInfo>();
            }

            matchProperties.Add(matchProperty);
        }

        return matchProperties;
    }


    private static bool ParameterMatchProperty(ParameterInfo parameter, PropertyInfo property)
    {
        return string.Equals(property.Name, parameter.Name, StringComparison.InvariantCultureIgnoreCase)
               && parameter.ParameterType == property.PropertyType;
    }

    private static bool IsReadOnlyProperty(BsonClassMap classMap, PropertyInfo propertyInfo)
    {
        if (!propertyInfo.CanRead)
        {
            return false;
        }

        if (propertyInfo.CanWrite)
        {
            return false;
        }

        if (propertyInfo.GetIndexParameters().Length != 0)
        {
            return false;
        }

        var getMethodInfo = propertyInfo.GetMethod;
        if (getMethodInfo.IsVirtual && getMethodInfo.GetBaseDefinition().DeclaringType != classMap.ClassType)
        {
            return false;
        }

        return true;
    }
}
