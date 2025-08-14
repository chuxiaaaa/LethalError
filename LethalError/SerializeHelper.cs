using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using YamlDotNet.Serialization;

namespace LethalError
{
    public static class SerializeHelper
    {
        public static string YamlSerialize(object obj)
        {
            var builder = new SerializerBuilder().ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull).Build();
            return builder.Serialize(obj);
        }

        public static T YamlDeserialize<T>(string input)
        {
            try
            {
                var builder = new DeserializerBuilder().Build();
                return builder.Deserialize<T>(input);
            }
            catch (Exception)
            {
                return default;
            }
        }
    }
}
