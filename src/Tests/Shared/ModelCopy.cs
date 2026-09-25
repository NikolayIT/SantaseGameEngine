namespace Santase.Tests.Shared
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    using Santase.Logic.Cards;

    // The engine's view and record models are plain classes whose public properties are all there
    // is. A host maps them to its own models and back; these helpers do the same generically, so
    // tests can check that nothing is lost on the way. Linked into every test project.
    internal static class ModelCopy
    {
        // A deep copy built only from public properties, with cards carried as their codes.
        public static T Copy<T>(T model)
            where T : class
        {
            return (T)CopyValue(model, typeof(T));
        }

        // Deterministic text of a model: every public property, recursively, cards as codes.
        public static string Describe(object model)
        {
            var text = new StringBuilder();
            Append(text, model);
            return text.ToString();
        }

        // Every card anywhere in a model.
        public static IEnumerable<Card> CardsIn(object model)
        {
            switch (model)
            {
                case null:
                    yield break;
                case Card card:
                    yield return card;
                    yield break;
                case string or Enum or ValueType:
                    yield break;
                case IEnumerable items:
                    foreach (var item in items)
                    {
                        foreach (var card in CardsIn(item))
                        {
                            yield return card;
                        }
                    }

                    yield break;
                default:
                    foreach (var property in Properties(model.GetType()))
                    {
                        foreach (var card in CardsIn(property.GetValue(model)))
                        {
                            yield return card;
                        }
                    }

                    yield break;
            }
        }

        private static object CopyValue(object value, Type type)
        {
            switch (value)
            {
                case null:
                    return null;
                case Card card:
                    return CardCode.Parse(CardCode.Format(card));
                case string or Enum or ValueType:
                    return value;
                case IEnumerable items:
                    var elementType = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
                    var copies = items.Cast<object>().Select(item => CopyValue(item, elementType)).ToList();
                    var array = Array.CreateInstance(elementType, copies.Count);
                    for (var i = 0; i < copies.Count; i++)
                    {
                        array.SetValue(copies[i], i);
                    }

                    return array;
                default:
                    var copy = Activator.CreateInstance(value.GetType());
                    foreach (var property in Properties(value.GetType()))
                    {
                        property.SetValue(copy, CopyValue(property.GetValue(value), property.PropertyType));
                    }

                    return copy;
            }
        }

        private static void Append(StringBuilder text, object value)
        {
            switch (value)
            {
                case null:
                    text.Append("null");
                    break;
                case Card card:
                    text.Append(CardCode.Format(card));
                    break;
                case string or Enum or ValueType:
                    text.Append(value);
                    break;
                case IEnumerable items:
                    text.Append('[');
                    foreach (var item in items)
                    {
                        Append(text, item);
                        text.Append(',');
                    }

                    text.Append(']');
                    break;
                default:
                    text.Append('{');
                    foreach (var property in Properties(value.GetType()))
                    {
                        text.Append(property.Name).Append('=');
                        Append(text, property.GetValue(value));
                        text.Append(';');
                    }

                    text.Append('}');
                    break;
            }
        }

        private static IEnumerable<PropertyInfo> Properties(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
                .OrderBy(p => p.Name, StringComparer.Ordinal);
        }
    }
}
