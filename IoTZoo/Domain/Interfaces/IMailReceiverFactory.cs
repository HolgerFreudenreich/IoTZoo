using Domain.Interfaces.Crud;
using Domain.Pocos;
using Domain.Services;

namespace Domain.Interfaces
{
    public interface IMailReceiverFactory
    {
        MailReceiverService Create(MailReceiverConfig config, IDataTransferService dataTransferService, IProjectCrudService projectCrudService);
    }
}