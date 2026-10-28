namespace ClientStat.Application.Exceptions
{
    public class ClientNotFoundException : Exception
    {
        public long ClientId{get;}

        public ClientNotFoundException(long clientId) : base ($"Клиент с id {clientId} не найден.")
        {
            ClientId = clientId;
        }
    }
}