using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Plugins.CasinoBot.Cards;

namespace SharpIrcBot.Plugins.CasinoBot.Player
{
    public class EventDispatcher
    {
        public virtual void DispatchEvent(object caller, JObject eventObject)
        {
            JToken eventToken;
            if (!eventObject.TryGetValue("event", StringComparison.Ordinal, out eventToken))
            {
                throw new ArgumentException($"missing \"event\" name/value pair in {nameof(eventObject)}", nameof(eventObject));
            }
            if (eventToken.Type != JTokenType.String)
            {
                throw new ArgumentException($"value for \"event\" is not a string in {nameof(eventObject)}", nameof(eventObject));
            }

            string eventName = (string)((JValue)eventToken).Value;

            // any takers?
            Type botType = caller.GetType();
            foreach (MethodInfo method in botType.GetMethods())
            {
                bool thisEvent = method.GetCustomAttributes<EventAttribute>()
                    .Any(ea => ea.EventName == eventName);
                if (!thisEvent)
                {
                    // no such luck; next!
                    continue;
                }

                // try populating all the parameters
                bool parameterFailure = false;
                ParameterInfo[] parameters = method.GetParameters();
                var arguments = new object[parameters.Length];
                for (int i = 0; i < parameters.Length; ++i)
                {
                    ParameterInfo parameter = parameters[i];

                    string valueName = parameter.GetCustomAttributes<EventValueAttribute>()
                        .Select(eva => eva.ValueName)
                        .FirstOrDefault();

                    if (valueName == null || eventObject[valueName] == null)
                    {
                        // cannot populate the argument from the event

                        if (!parameter.HasDefaultValue)
                        {
                            // and it doesn't have a default value; I give up
                            parameterFailure = true;
                            break;
                        }

                        arguments[i] = parameter.DefaultValue;
                    }
                    else
                    {
                        // okay, see if we can fill it
                        TypeInfo parameterType = parameter.ParameterType.GetTypeInfo();
                        JToken eventValue = eventObject.GetValue(valueName);

                        if (parameterType.IsAssignableFrom(typeof(JToken)))
                        {
                            // alright, raw JToken it is
                            arguments[i] = eventValue;
                        }
                        else if (parameterType.IsAssignableFrom(typeof(JValue)) && eventValue is JValue)
                        {
                            arguments[i] = (JValue)eventValue;
                        }
                        else if (parameterType.IsAssignableFrom(typeof(JObject)) && eventValue is JObject)
                        {
                            arguments[i] = (JObject)eventValue;
                        }
                        else if (parameterType.IsAssignableFrom(typeof(JArray)) && eventValue is JArray)
                        {
                            arguments[i] = (JArray)eventValue;
                        }
                        else if (eventValue.Type == JTokenType.Null)
                        {
                            if (!parameterType.IsValueType)
                            {
                                // reference type
                                arguments[i] = null;
                            }
                            else if (parameterType.IsGenericType
                                    && parameterType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                            {
                                // nullable value type
                                arguments[i] = null;
                            }
                            else
                            {
                                // nope :(
                                parameterFailure = true;
                                break;
                            }
                        }
                        else if (eventValue.Type == JTokenType.Float || eventValue.Type == JTokenType.Integer)
                        {
                            if (parameterType.IsAssignableFrom(typeof(int))
                                    || parameterType.IsAssignableFrom(typeof(int?)))
                            {
                                arguments[i] = (int)eventValue;
                            }
                            else if (parameterType.IsAssignableFrom(typeof(long))
                                    || parameterType.IsAssignableFrom(typeof(long?)))
                            {
                                arguments[i] = (long)eventValue;
                            }
                            else if (parameterType.IsAssignableFrom(typeof(double))
                                    || parameterType.IsAssignableFrom(typeof(double?)))
                            {
                                arguments[i] = (double)eventValue;
                            }
                            else if (parameterType.IsAssignableFrom(typeof(decimal))
                                    || parameterType.IsAssignableFrom(typeof(decimal?)))
                            {
                                arguments[i] = (decimal)eventValue;
                            }
                            // add more?
                            else
                            {
                                parameterFailure = true;
                                break;
                            }
                        }
                        else if (eventValue.Type == JTokenType.String)
                        {
                            var stringValue = (string)eventValue;

                            if (parameterType.IsAssignableFrom(typeof(char))
                                    || parameterType.IsAssignableFrom(typeof(char?)))
                            {
                                if (stringValue.Length == 1)
                                {
                                    arguments[i] = stringValue[0];
                                }
                                else
                                {
                                    parameterFailure = true;
                                    break;
                                }
                            }
                            else if (parameterType.IsAssignableFrom(typeof(string)))
                            {
                                arguments[i] = stringValue;
                            }
                            else
                            {
                                parameterFailure = true;
                                break;
                            }
                        }
                        else if (eventValue.Type == JTokenType.Array)
                        {
                            var arrayValue = (JArray)eventValue;

                            if (arrayValue.All(jv => jv is JObject))
                            {
                                // might be a useful array
                                if (parameterType.IsAssignableFrom(typeof(List<Card>)))
                                {
                                    try
                                    {
                                        List<Card> cards = arrayValue.Select(jv => CardUtils.CardFromJson((JObject)jv))
                                            .ToList();
                                        arguments[i] = cards;
                                    }
                                    catch (ArgumentException)
                                    {
                                        parameterFailure = true;
                                        break;
                                    }
                                }
                                else if (parameterType.IsAssignableFrom(typeof(List<JObject>)))
                                {
                                    List<JObject> jobjects = arrayValue.Select(jv => (JObject)jv)
                                        .ToList();
                                    arguments[i] = jobjects;
                                }
                                else
                                {
                                    parameterFailure = true;
                                    break;
                                }
                            }
                            else if (parameterType.IsAssignableFrom(typeof(List<JToken>)))
                            {
                                List<JToken> jtokens = arrayValue.ToList();
                                arguments[i] = jtokens;
                            }
                            else
                            {
                                parameterFailure = true;
                                break;
                            }
                        }
                        else
                        {
                            parameterFailure = true;
                            break;
                        }
                    }
                }

                if (parameterFailure)
                {
                    continue;
                }

                // cool, we found a matching method; run it!
                method.Invoke(caller, arguments);
                break;
            }
        }
    }
}
