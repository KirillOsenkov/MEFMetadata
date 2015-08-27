using System.Reflection;

namespace System.ComponentModel.Composition.Hosting
{
    public class ReflectionHelpers
    {
        private static Assembly mscorlib = typeof(PropertyInfo).Assembly;
        private static Type runtimeTypeType = mscorlib.GetType("System.RuntimeType");
        private static Type runtimePropertyInfoType = mscorlib.GetType("System.Reflection.RuntimePropertyInfo");
        private static PropertyInfo cacheProperty = runtimeTypeType.GetProperty("Cache", BindingFlags.Instance | BindingFlags.NonPublic);
        private static ConstructorInfo runtimePropertyInfoCtor = runtimePropertyInfoType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)[0];

        public static PropertyInfo GetPropertyInfo(
            Module module,
            int propertyMetadataToken,
            int parentTypeMetadataToken)
        {
            Type propertyParentType = module.ResolveType(parentTypeMetadataToken);
            var cache = cacheProperty.GetValue(propertyParentType, null);
            var constructorArguments = new object[]
            {
                propertyMetadataToken,
                propertyParentType,
                cache,
                false
            };
            var propertyInfo = (PropertyInfo)runtimePropertyInfoCtor.Invoke(constructorArguments);
            return propertyInfo;
        }
    }
}
