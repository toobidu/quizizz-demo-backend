using ConsoleApp1.Service.Interface.Socket;

namespace ConsoleApp1.Service.Interface;

public interface ISocketService : 
    ISocketConnectionService,
    IRoomManagementSocketService,
    IGameFlowSocketService,
    IPlayerInteractionSocketService,
    IScoringSocketService,
    IHostControlSocketService
{
}