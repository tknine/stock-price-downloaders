using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YahooPriceDownloader
{
    public static class Utility
    {
        public static StreamReader ToStreamReader(this string str)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(str);
            //byte[] byteArray = Encoding.ASCII.GetBytes(contents);
            MemoryStream stream = new MemoryStream(byteArray);


            // convert stream to string
            return new StreamReader(stream);
        }
    }
}
