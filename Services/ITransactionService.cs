using System.Threading.Tasks;

namespace Transaction.Services
{
    public interface ITransactionService
    {
        Task<string> GetCardTypeAsync(string binNumber);
        Task ProcessTranactionAsync(string transFile, string outputFile);
    }
}