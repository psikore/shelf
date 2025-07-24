using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DataModels;

namespace MsgPackSerializerGenerator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string code = MsgPackSerializerGenerator.GenerateSerializer(typeof(Person));
            File.WriteAllText(@"E:\projects\MsgPackSerializerGenerator\Test\GeneratedSerializers.cs", code);
        }
    }
}
