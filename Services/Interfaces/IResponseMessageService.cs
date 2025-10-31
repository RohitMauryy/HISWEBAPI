namespace HISWEBAPI.Services
{
    public interface IResponseMessageService
    {
        (string Type, string Message) GetMessageAndTypeByAlertCode(string alertCode);

    }
}