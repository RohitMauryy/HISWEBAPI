using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using log4net;
using System.Reflection;
using HISWEBAPI.Services.Interfaces;

namespace HISWEBAPI.Services.Interfaces
{
    public interface ISmsService
    {
        bool SendOtp(string contactNumber, string otp);
        bool SendSms(string contactNumber, string message);

    }
}

