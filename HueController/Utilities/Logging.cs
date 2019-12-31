using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace HueController.Utilities
{
    public static class Logging
    {
        /// <summary>
        /// Itemizes a primitive.
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="myObject">Primitive to parse.</param>
        /// <param name="objectName">Parameter name of the primitive to prase.</param>
        /// <returns>Dictionary of property and value KeyValuePairs</returns>
        public static Dictionary<string, string> ToDictionary<T>(this T myObject, string objectName) where T : struct
        {
            // Primitive objects can simply have their value converted to a string.
            return new Dictionary<string, string> { { objectName, myObject.ToString() } };
        }

        /// <summary>
        /// Itemizes a string. 
        /// </summary>
        /// <param name="myObject">String to parse.</param>
        /// <param name="objectName">Paramete name of the string to parse.</param>
        /// <returns>Dictionary of property and value KeyValuePairs</returns>
        public static Dictionary<string, string> ToDictionary(this string myObject, string objectName)
        {
            return new Dictionary<string, string> { { objectName ?? myObject, myObject } };
        }

        /// <summary>
        /// Obsolete. Use ToDictionary(string) instead. Since string is a class, must override the generic
        /// implementation of the same to ensure ToDictionary usage with string objects is properly detected
        /// and a compiler warning is raised for proper redirect.
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="myObject">Dictionary of property and value KeyValuePairs</param>
        /// <returns>Dictionary of property and value KeyValuePairs</returns>
        [Obsolete("Use ToDictionary(nameof(myString)) instead.", true)]
        public static Dictionary<string, string> ToDictionary(this string myObject)
        {
            throw new NotImplementedException("Use ToDictionary(nameof(myString)) instead.");
        }

        /// <summary>
        /// Itemizes all property and value pairs.
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="myObject">Dictionary of property and value KeyValuePairs</param>
        /// <param name="loggingPurpose">Indicates if output is for logging\AppInsight purposes only.</param>
        /// <param name="keyPrefix">Prefixes keys for this dictionary with the provided string.  Useful to avoid key collisions.</param>
        /// <returns>Dictionary of property and value KeyValuePairs</returns>
        public static IDictionary<string, string> ToDictionary<T>(this T myObject) where T : class
        {
            IDictionary<string, string> properties = new Dictionary<string, string>();

            foreach (PropertyInfo propertyInfo in myObject.GetType().GetProperties())
            {
                try
                {
                    // Serialize public properties which are not an interface. Nested properties are OK.
                    if (propertyInfo.PropertyType.IsVisible && !propertyInfo.PropertyType.IsInterface)
                    {
                        if (!propertyInfo.PropertyType.IsSerializable &&
                            (null != propertyInfo.GetValue(myObject)) &&
                            propertyInfo.GetValue(myObject).GetType().GetProperties().Any())
                        {
                            // Flatten complex objects which have properties to flatten
                            properties = MergeDictionaries(new IDictionary<string, string>[] { properties, propertyInfo.GetValue(myObject)?.ToDictionary() });
                        }
                        else
                        {
                            string value;

                            if (!propertyInfo.PropertyType.IsSerializable ||
                                (propertyInfo.PropertyType.IsGenericType && typeof(ICollection<>).IsAssignableFrom(propertyInfo.PropertyType.GetGenericTypeDefinition()) ||
                                propertyInfo.PropertyType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICollection<>))))
                            {
                                value = JsonConvert.SerializeObject(propertyInfo.GetValue(myObject));
                            }
                            else
                            {
                                value = propertyInfo.GetValue(myObject)?.ToString();
                            }

                            string newValue = string.IsNullOrEmpty(value) ? $"Null {propertyInfo.Name}" : value;

                            DisplayNameAttribute displayNameAttribute = (DisplayNameAttribute)propertyInfo.GetCustomAttribute(typeof(DisplayNameAttribute));
                            if (null != displayNameAttribute)
                            {
                                properties[displayNameAttribute.DisplayName] = newValue;
                            }
                            else
                            {
                                properties[propertyInfo.Name] = newValue;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                }
            }

            return properties as Dictionary<string, string>;
        }

        /// <summary>
        /// Flattens multiple dictionaries in to a single dictionary, if needed. Duplicates are not merged when already present.
        /// </summary>
        /// <param name="contextProperties">Property Bags to flatten</param>
        /// <returns>An instance of <see cref="IDictionary{string, string}"/>.</returns>
        public static IDictionary<string, string> MergeDictionaries(this IDictionary<string, string>[] contextProperties)
        {
            return null != contextProperties && contextProperties.Count() > 1 ?
                contextProperties.Where(x => null != x).SelectMany(x => x).ToLookup(x => x.Key, x => x.Value).ToDictionary(x => x.Key, x => x.First()) :
                contextProperties?.SingleOrDefault();
        }

        /// <summary>
        /// Merges multiple dictionaries in to a single dictionary.
        /// </summary>
        /// <param name="initialDictionary">The dictionary that will be merged into.</param>
        /// <param name="mergedDictionaries">An array of dictionaries that will be merged into <paramref name="initialDictionary"/>.</param>
        /// <returns>An instance of <see cref="IDictionary{string, string}"/>.</returns>
        /// <remarks>Dictionary parameters latest in the array will have the highest precedence when setting values.</remarks>
        public static IDictionary<string, string> MergeDictionaries(this IDictionary<string, string> initialDictionary, params IDictionary<string, string>[] mergedDictionaries)
        {
            if (initialDictionary != null && mergedDictionaries != null)
            {
                List<IDictionary<string, string>> contextPropertiesArray = new List<IDictionary<string, string>>();
                contextPropertiesArray.Add(initialDictionary);
                contextPropertiesArray.AddRange(mergedDictionaries);
                return MergeDictionaries(contextPropertiesArray.ToArray());
            }

            return initialDictionary;
        }
    }
}
