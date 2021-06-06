using System.Threading.Tasks;

namespace BeyondAuth.PasswordValidators.Topology
{
    public interface IPasswordTopologyProvider
    {
        Task<long> GetTopologyCount(string password);

        Task IncrementTopologyCount(string password);
    }
}
