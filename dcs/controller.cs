using System.Net.Http;
using System.Web.Http;
using System.Runtime.Serialization.Json;
using System.Runtime.Intrinsics.Arm;
using System.Text;

public class SecureController : ApiController
{
    [HttpPost]
    [Route("api/secure")]
    public IHttpActionResult ReceiveEncrypted(EncryptedRequest req)
    {
        if (req?.payload == null)
            return BadRequest("Missing payload");

        try
        {
            byte[] data = Convert.FromBase64String(req._VALUE);
            string json = MyPayload.ProcessPayload(data);
            var serializer = new DataContractJsonSerializer(typeof(MyPayload));
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var obj = (MyPayload)serializer.ReadObject(ms);
                return Ok(new { Received = obj });
            }
        }
        catch (Exception ex)
        {
            return InternalServerError(ex);
        }
    }
}
