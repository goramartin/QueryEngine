/*! \file
  This file includes definition of a specialised property factory.
  Properties are created by inputed string that contains the type of the property.
 */

using System;
using System.Collections.Generic;

namespace QueryEngine
{
    /// <summary>
    /// A property factory.
    /// Class includes a register of all the property types.
    /// It enables us to create instances of properties based on a string token.
    /// </summary>
    public static class PropertyFactory
    {
        /// <summary>
        /// A register with valid types.
        /// </summary>
        private static Dictionary<string, Type> registry;

        /// <summary>
        /// Inicialises the registry.
        /// </summary>
        static PropertyFactory()
        {
            registry = new Dictionary<string, Type>();
            InicialiseRegistry();
        }

        /// <summary>
        /// Inicialises the registry with predefined values.
        /// </summary>
        private static void InicialiseRegistry()
        {
            RegisterProperty("string", typeof(StringProperty));
            RegisterProperty("integer", typeof(IntProperty));
        }

        /// <summary>
        /// Registers a property with a given token and bounds a given type to it.
        /// </summary>
        /// <param name="token"> A name of the property type. </param>
        /// <param name="type"> A type of property (string, int ...). </param>
        private static void RegisterProperty(string token, Type type)
        {
            if (token == null || type == null)
                throw new ArgumentException($"PropertyFactory, cannot register null type or null token.");

            if (registry.ContainsKey(token))
                throw new ArgumentException($"PropertyFactory, property Type already registered. Token = {token}");

            registry.Add(token, type);
        }

        /// <summary>
        /// Creates an instance of a property based on a given token.
        /// </summary>
        /// <param name="token"> A property type </param>
        /// <param name="name"> A type of a property type. </param>
        /// <returns> A specialiased property for the given type. </returns>
        public static Property CreateProperty(string token, string name)
        {
            if (token == null || name == null)
                throw new ArgumentException($"PropertyFactory, passed null name or null token.");

            if (registry.TryGetValue(token, out Type propType))
                return (Property)Activator.CreateInstance(propType, name);
            else throw new ArgumentException($"PropertyFactory, failed to load type from registry. Type = {token}.");

        }
    }
}
