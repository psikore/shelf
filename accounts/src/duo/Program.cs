using System.Threading.Tasks;
using SharpTep;

namespace SharpTep
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await Server.Serve();
        }
    }
}
