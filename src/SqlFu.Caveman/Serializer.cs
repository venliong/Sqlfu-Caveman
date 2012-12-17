using System.Text;
using CavemanTools.Logging;
using Newtonsoft.Json;

namespace SqlFu.Caveman
{
    internal static class Serializer
    {
         public static byte[] Serialize(this object data)
         {
             var txt = JsonConvert.SerializeObject(data, GetJsonSettings());
#if DEBUG
             LogHelper.DefaultLogger.Debug(txt);
#endif
             return Encoding.Unicode.GetBytes(txt);
         }

         public static JsonSerializerSettings GetJsonSettings()
         {
             return new JsonSerializerSettings()
             {
                 TypeNameHandling =
                     TypeNameHandling.Objects,
                 PreserveReferencesHandling =
                     PreserveReferencesHandling.
                         Arrays
             };
         }
        public static object Deserialize(this byte[] data)
        {
            var js = Encoding.Unicode.GetString(data);
            return JsonConvert.DeserializeObject(js, GetJsonSettings());
        }

    }
}