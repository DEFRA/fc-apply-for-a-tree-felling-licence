using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Forestry.Flo.Services.Common.Extensions
{
    public static class StreamExtensions
    {
        /// <summary>
        /// Takes a stream input and returns a byte array
        /// </summary>
        /// <param name="input"></param>
        /// <returns>Returns a Byte[]</returns>
        public static byte[] ConvertStreamToBytes(this Stream input)
        {
            using (var ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}
