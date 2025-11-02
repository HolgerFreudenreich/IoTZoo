using Domain.Interfaces;
using Domain.Interfaces.Crud;
using Domain.Pocos;

namespace Domain.Services
{
    public interface IMailReceiverFactory
    {
        MailReceiverService Create(MailReceiverConfig config, IDataTransferService dataTransferService, IProjectCrudService projectCrudService);
    }
}