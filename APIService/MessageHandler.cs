using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace APIService
{
    public class MessageHandler : DelegatingHandler
    {
        public MessageHandler(ServiceHolder holder)
        {
            Holder = holder;
        }

        public ServiceHolder Holder { get; }

        protected async override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var options = request.RequestUri.PathAndQuery.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var serviceName = options[1];
            var meathodName = options[2];
            var serviceType = Holder.Get(serviceName);
            var instance = Activator.CreateInstance(serviceType);
            MethodInfo method= serviceType.GetMethod(meathodName);
            List<object> inputParams = new List<object>();
            // if Request is POST, reading in the details
            //if (request.Method == HttpMethod.Post)
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Count() == 1)
            {
                object input = await request.Content.ReadAsAsync(parameters[0].ParameterType, new[] { new JsonMediaTypeFormatter() });
                inputParams.Add(input);
            }
            else if (parameters.Count() >1)
            {
                Stream stream = await request.Content.ReadAsStreamAsync();
                var sr = new StreamReader(stream);

                JsonSerializer serializer = JsonTypeHelper.GetSerializer();

                JsonReader reader = new JsonTextReader(sr);
                reader.Read();
                if (reader.TokenType != JsonToken.StartObject)
                {
                    throw new InvalidOperationException("Input needs to be wrapped in an object");
                }
                reader.Read();
                while (reader.TokenType == JsonToken.PropertyName)
                {
                    var parameterName = reader.Value as string;
                    reader.Read();
                    ParameterInfo parameterInfo = null;
                    if ((parameterInfo = parameters.FirstOrDefault(s=>s.Name == parameterName))!=null)
                    {
                        Type type = parameterInfo.ParameterType;
                        var parameterIndex = Array.IndexOf(parameters, parameterInfo);
                        if (type.IsSimpleType())
                        {
                            inputParams.Add(serializer.Deserialize(reader, type));
                            //inputParams[parameterIndex] = serializer.Deserialize(reader, type);
                        }
                        else
                        {
                            inputParams.Add(serializer.Deserialize(reader));

                            //inputParams[parameterIndex] = serializer.Deserialize(reader);

                        }

                        if (inputParams[parameterIndex] is JToken token)//if we have a json token try deserializing with parameter type
                        {
                            inputParams[parameterIndex] = JsonTypeHelper.Convert(inputParams[parameterIndex], type);
                        }
                    }
                    else
                    {
                        reader.Skip();
                    }

                    reader.Read();
                }

                reader.Close();

                //string jsonString = await request.Content.ReadAsStringAsync();
                //JObject jObject = JObject.Parse(HttpUtility.HtmlDecode(jsonString));
                //foreach (var param in method.GetParameters())
                //{
                //    JProperty jProperty = jObject.Property(param.Name);
                //    object input = jProperty.ToObject(param.ParameterType);
                //   // object input = await request.Content.ReadAsAsync(param.ParameterType, new[] { new JsonMediaTypeFormatter() });
                //    inputParams.Add(input);
                //}
            }

            var result = method.Invoke(instance, inputParams.ToArray());
            var json = JsonConvert.SerializeObject(result);
            // Create the response.
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json),
                
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            // Note: TaskCompletionSource creates a task that does not contain a delegate.
          //  var tsc = new TaskCompletionSource<HttpResponseMessage>();
          //  tsc.SetResult(response);   // Also sets the task state to "RanToCompletion"
            return response;
        }

    }
    public static class JsonTypeHelper
    {
        public static bool IsSimpleType(this Type type)
        {
            return (type == typeof(string) || type.IsPrimitive || type.IsValueType || type == typeof(DateTime) || type == typeof(TimeSpan) || type.IsEnum);
        }

        public static JsonSerializer GetSerializer()
        {
            return JsonSerializer.Create(new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Objects
            });
        }

        public static object Convert(object param, Type type = null)
        {
            param = CallTypeConvertor(param, type);
            if (param is JToken token)//if we have a json token try deserializing with parameter type
            {
                if (type == null)
                {
                    if (token.Type == JTokenType.Object)
                    {
                        JsonSerializer serializer = GetSerializer();
                        param = serializer.Deserialize(token.CreateReader());
                    }
                    else
                    {
                        param = HandleTypelessTokenConvertion(token);
                        return param;
                    }

                }
                else
                {
                    param = token.ToObject(type);

                }
                if (param is Array array)
                {
                    Array arrayInstance = Array.CreateInstance(type.GetElementType(), array.Length);

                    for (int i = 0; i < array.Length; i++)
                    {
                        object item = array.GetValue(i);
                        arrayInstance.SetValue(Convert(item, type.GetElementType()), i);
                    }
                    param = arrayInstance;

                }
            }
            return param;
        }

        private static object CallTypeConvertor(object param, Type type)
        {
            if (type != null && convertors.ContainsKey(type))
            {
                param = convertors[type].Invoke(param);
            }

            return param;
        }

        private static object HandleTypelessTokenConvertion(JToken token)
        {
            if (token is JArray arrayTokens)
            {
                //get the base type
                JTokenType tokenType = arrayTokens.First.Type;
                Type simpleArrayType = GetType(tokenType);
                if (simpleArrayType != null) //simple type handeling
                {
                    Array arrayInstance = Array.CreateInstance(simpleArrayType, arrayTokens.Count);
                    for (int i = 0; i < arrayTokens.Count; i++)
                    {
                        JToken arrayToken = arrayTokens[i];
                        object converted = HandleTypelessTokenConvertion(arrayToken);
                        arrayInstance.SetValue(converted, i);
                    }
                    return arrayInstance;
                }
                else //complex type without type info
                {
                    //get the first token
                    //convert it
                    //call convertors on it
                    //create typed array
                    //put in other items

                    object firstToken = arrayTokens.First;
                    object first = Convert(firstToken);
                    Type arrayType = first.GetType();
                    first = CallTypeConvertor(first, arrayType);

                    Array arrayInstance = Array.CreateInstance(arrayType, arrayTokens.Count);
                    arrayInstance.SetValue(first, 0);

                    for (int i = 1; i < arrayTokens.Count; i++)
                    {
                        object arrayToken = arrayTokens[i];
                        var converted = Convert(arrayToken);
                        converted = CallTypeConvertor(converted, arrayType);

                        arrayInstance.SetValue(converted, i);
                    }

                    return arrayInstance;

                }

            }
            Type type = GetType(token.Type);
            if (type != null)
            {
                return System.Convert.ChangeType(token.ToString(), type);
            }
            return token;
        }

        private static Type GetType(JTokenType tokenType)
        {
            if (jTokenTypeMap.ContainsKey(tokenType))
            {
                return jTokenTypeMap[tokenType];
            }
            return null;
        }

        private static Dictionary<JTokenType, Type> jTokenTypeMap = new Dictionary<JTokenType, Type>() {
            { JTokenType.Boolean, typeof(bool)},
            { JTokenType.String, typeof(string)},
            { JTokenType.Date, typeof(DateTime)},
            { JTokenType.Float, typeof(double)},
            { JTokenType.Guid, typeof(Guid)},
            { JTokenType.Integer, typeof(int)},
            { JTokenType.TimeSpan, typeof(TimeSpan)},
        };


        private static readonly Dictionary<Type, Func<object, object>> convertors = new Dictionary<Type, Func<object, object>>()
        {
            

        };

    }
    public class ServiceHolder
    {
        Dictionary<string, Type> store = new Dictionary<string, Type>();

        public void Init()
        {
            AddServices(Assembly.GetAssembly(this.GetType()));
        }

        public void AddServices(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentException("assembly argument cannot be null", "assembly");

            Type[] types = assembly.GetTypes();


            foreach (Type type in types)
            {
                (bool isServiceType, string name) r = IsServiceType(type);
                if (r.isServiceType)
                    store.Add( r.name, type);
            }
        }

        (bool isServiceType, string name) IsServiceType(Type type)
        {
            //type must be a non-abstract class
            if (!type.IsClass || type.IsAbstract)
                return (false, null);

            RegisterServiceAttribute[] attrs =
                (RegisterServiceAttribute[])type.GetCustomAttributes(typeof(RegisterServiceAttribute), true);
            //type must have at least one AutoRegisterServiceAttribute
            if (attrs.Length == 0)
                return (false, null); 


            return (true, attrs.FirstOrDefault().Name); ;
        }

        public Type Get(string name)
        {
            if (store.ContainsKey(name))
            {
                return store[name];
            }
            return null;
        }
    }
}
