using System.Net;

namespace StargateAPI.Controllers
{
    public class BaseResponse
    {
        public bool Success { get; set; } = false;
        public string Message { get; set; } = "Operation failed";
        public int ResponseCode { get; set; } = (int)HttpStatusCode.BadRequest;
    }
}