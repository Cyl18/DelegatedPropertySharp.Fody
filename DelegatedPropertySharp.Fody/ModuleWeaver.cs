using System;
using System.Collections.Generic;
using System.Linq;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using FodyTools;
using Mono.Cecil.Cil;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using ICustomAttributeProvider = Mono.Cecil.ICustomAttributeProvider;

namespace DelegatedPropertySharp.Fody
{
    public class ModuleWeaver : BaseModuleWeaver
    {
        static readonly Dictionary<TypeDefinition, TypeReference> HandlerRegistry = new Dictionary<TypeDefinition, TypeReference>();
        public override void Execute()
        {
            foreach (var data in ModuleDefinition.Types
                .Where(type => type.Interfaces.Any(i => i.InterfaceType.Name == "IDelegatedPropertyHandler`3")))
            {
                var parameter = data.Interfaces.First(i => i.InterfaceType.Name == "IDelegatedPropertyHandler`3").InterfaceType;
                var genericArguments = ((GenericInstanceType)parameter).GenericArguments;
                var genericArgument = genericArguments[2];
                HandlerRegistry[genericArgument.Resolve()] = data;
            }
            
            foreach (var data in ModuleDefinition.Types
                .SelectMany(type => type.Properties)
                .Select(prop => new {prop, attributeData = ConsumeAttribute(prop) })
                .Where(o => o.attributeData.hasAttribute))
            {
                if (data.prop.GetMethod != null) MakeGetter(data.prop, data.attributeData.handlerType);
                if (data.prop.SetMethod != null) MakeSetter(data.prop, data.attributeData.handlerType);
            }
            
        }

        static int _objectCount;
        static int ObjectCount => _objectCount++;

        void AddInitCCtorCode(TypeDefinition type, FieldReference field, TypeReference propertyType, PropertyDefinition property)
        {
            field = ModuleDefinition.ImportReference(field);
            propertyType = ModuleDefinition.ImportReference(propertyType);
            var module = ModuleDefinition;

            type.InsertIntoStaticConstructor(
                Instruction.Create(OpCodes.Newobj, field.FieldType.Resolve().GetDefaultConstructor().MakeHostInstanceGeneric(type, propertyType)),
                Instruction.Create(OpCodes.Stsfld, field),

                Instruction.Create(OpCodes.Ldsfld, field),
                Instruction.Create(OpCodes.Ldtoken, property.DeclaringType),
                Instruction.Create(OpCodes.Call, module.ImportReference(typeof(Type).GetMethod("GetTypeFromHandle"))),
                Instruction.Create(OpCodes.Ldstr, property.Name),
                Instruction.Create(OpCodes.Call, module.ImportReference(typeof(Type).GetMethod("GetProperty", new[] { typeof(string) }))),
                Instruction.Create(OpCodes.Callvirt, field.FieldType.Resolve().Properties.Single(prop => prop.Name == "PropertyInfo").SetMethod.MakeHostInstanceGeneric(property.DeclaringType, property.PropertyType)));
        }


        void MakeGetter(PropertyDefinition property, TypeReference handlerType)
        {
            var methodDefinition = property.GetMethod.Resolve();
            methodDefinition.Body.Instructions.Clear();

            var il = methodDefinition.Body.GetILProcessor();
            if (handlerType.GenericParameters.Count != 2)
                throw new WeavingException("The handler type must have 2 generic type param");

            var field = GetOrCreateBackingField(property, handlerType);

            
            il.Emit(OpCodes.Ldsfld, field);
            il.Emit(property.GetMethod.IsStatic ? OpCodes.Ldnull : OpCodes.Ldarg_0);
            
            il.Emit(OpCodes.Callvirt, handlerType.Resolve().Methods.First(method => method.Name == "Get").MakeHostInstanceGeneric(property.DeclaringType, property.PropertyType));
            il.Emit(OpCodes.Ret);
            il.Body.OptimizeMacros();

            property.DeclaringType.Methods.RemoveAll(method => method.Name == property.GetMethod.Name);
            property.DeclaringType.Methods.Add(methodDefinition);
            property.GetMethod = methodDefinition;
        }

        void MakeSetter(PropertyDefinition property, TypeReference handlerType)
        {
            var methodDefinition = property.SetMethod.Resolve();
            
            var il = methodDefinition.Body.GetILProcessor();
            methodDefinition.Body.Instructions.Clear();
            if (handlerType.GenericParameters.Count != 2)
                throw new WeavingException("The handler type must have 2 generic type param");

            var field = GetOrCreateBackingField(property, handlerType);


            il.Emit(OpCodes.Ldsfld, field);
            il.Emit(property.SetMethod.IsStatic ? OpCodes.Ldnull : OpCodes.Ldarg_0);
            il.Emit(property.SetMethod.IsStatic ? OpCodes.Ldarg_0: OpCodes.Ldarg_1);

            il.Emit(OpCodes.Callvirt, handlerType.Resolve().Methods.First(method => method.Name == "Set").MakeHostInstanceGeneric(property.DeclaringType, property.PropertyType));
            il.Emit(OpCodes.Ret);
            il.Body.OptimizeMacros();
        }

        static readonly Dictionary<PropertyDefinition, FieldDefinition> BackingFieldRegistry = new Dictionary<PropertyDefinition, FieldDefinition>();

        FieldDefinition GetOrCreateBackingField(PropertyDefinition property, TypeReference handlerType)
        {
            if (BackingFieldRegistry.ContainsKey(property))
            {
                return BackingFieldRegistry[property];
            }
            else
            {
                var definition = new FieldDefinition($"_handler{ObjectCount}", FieldAttributes.Private | FieldAttributes.Static,
                    handlerType.MakeGenericInstanceType(property.DeclaringType, property.PropertyType));
                BackingFieldRegistry[property] = definition;
                property.DeclaringType.Fields.Add(definition);
                AddInitCCtorCode(property.DeclaringType, definition, property.PropertyType, property);
                return definition;
            }
        }

        private static (bool hasAttribute, TypeReference handlerType) ConsumeAttribute(ICustomAttributeProvider attributeProvider)
        {
            const string attributeName = "DelegatedPropertySharp.DelegatedPropertyAttributeBase";

            var attribute = attributeProvider.CustomAttributes
                .Select(p => (p.AttributeType.GetSelfAndBaseTypes(), p))
                .Where(p => p.Item1.Any(t => t.FullName == attributeName))
                .Select(p => p.p).FirstOrDefault();

            if (attribute == null)
                return (false, null);
            
            var handlerType = HandlerRegistry[attribute.AttributeType.Resolve()];
            
            return (true, handlerType);
        }


        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield return "netstandard";
            yield return "mscorlib";
        }

    }

    static class Extensions
    {
        public static MethodReference MakeHostInstanceGeneric(this MethodReference self,
            params TypeReference[] args)
        {
            var reference = new MethodReference(
                self.Name,
                self.ReturnType,
                self.DeclaringType.MakeGenericInstanceType(args))
            {
                HasThis = self.HasThis,
                ExplicitThis = self.ExplicitThis,
                CallingConvention = self.CallingConvention
            };

            foreach (var parameter in self.Parameters)
            {
                reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
            }

            foreach (var genericParam in self.GenericParameters)
            {
                reference.GenericParameters.Add(new GenericParameter(genericParam.Name, reference));
            }

            return reference;
        }
    }
}
