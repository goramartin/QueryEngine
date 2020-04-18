﻿/*! \file
  
  File includes definition of a specialised property factory.
  Properties are created by inputed string that contains the type of the property.

 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryEngine
{
    /// <summary>
    /// Property factory.
    /// Class includes register of all the property types.
    /// Enables to create instance of a property based on a string token.
    /// </summary>
    static class PropertyFactory
    {
        /// <summary>
        /// Register with valid value types.
        /// </summary>
        static Dictionary<string, Type> registry;

        /// <summary>
        /// Inicialises registry.
        /// </summary>
        static PropertyFactory()
        {
            registry = new Dictionary<string, Type>();
            InicialiseRegistry();
        }

        /// <summary>
        /// Inicialises registry with predefined values.
        /// </summary>
        private static void InicialiseRegistry()
        {
            RegisterProperty("string", typeof(StringProperty));
            RegisterProperty("integer", typeof(IntProperty));

        }

        /// <summary>
        /// Registers a property with a given token and bounds a given type to the string.
        /// </summary>
        /// <param name="token"> Property type. </param>
        /// <param name="type"> Type of property type. </param>
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
        /// <param name="token"> Property type </param>
        /// <param name="name"> Type of a property type. </param>
        /// <returns></returns>
        public static Property CreateProperty(string token, string name)
        {
            if (token == null || name == null)
                throw new ArgumentException($"PropertyFactory, passed null name or null token.");

            if (!registry.ContainsKey(token))
                throw new ArgumentException($"PropertyFactory, token not found. Token = {token}.");

            Type propType = null;
            if (registry.TryGetValue(token, out propType))
            {
                return (Property)Activator.CreateInstance(propType, name);
            }
            else throw new ArgumentException($"PropertyFactory, failed to load type from registry. Type = {name}.");

        }
    }
}