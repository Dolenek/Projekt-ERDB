using System.Threading;
using System.Threading.Tasks;

namespace EpicRPGBot.UI.Pets
{
    public interface IPetGateway
    {
        Task<PetInventory> LoadAsync(CancellationToken cancellationToken);
        Task FuseAsync(PetFusionStep step);
    }
}
