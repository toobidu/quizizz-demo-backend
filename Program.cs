using System.Text;
using ConsoleApp1.Config;
using ConsoleApp1.Controller;
using ConsoleApp1.Data;
using ConsoleApp1.Repository.Implement;
using ConsoleApp1.Router;
using ConsoleApp1.Security;
using ConsoleApp1.Service;
using ConsoleApp1.Service.Implement;
using ConsoleApp1.Service.Implement.Socket;
using ConsoleApp1.Service.Interface;
using ConsoleApp1.Service.Interface.Socket;
using ConsoleApp1.Repository.Interface;
using ConsoleApp1.Model.DTO.Game;
using System.Collections.Concurrent;
using System.Net.WebSockets;
internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        var config = ConfigLoader.Load();
        // Kh?i t?o Redis v� JWT helper
        var redisConn = new RedisConnection(config.Redis);
        var redisService = new RedisServiceImplement(redisConn);
        var jwtHelper = new JwtHelper(config.Security);
        // Kh?i t?o Repository (d? li?u t? PostgreSQL)
        string dbConnection = config.ConnectionStrings["DefaultConnection"];
        IUserRepository userRepo = new UserRepositoryImplement(dbConnection);
        IRoleRepository roleRepo = new RoleRepositoryImplement(dbConnection);
        IUserRoleRepository userRoleRepo = new UserRoleRepositoryImplement(dbConnection);
        IRolePermissionRepository rolePermissionRepo = new RolePermissionRepositoryImplement(dbConnection);
        IPermissionRepository permissionRepo = new PermissionRepositoryImplement(dbConnection);
        IUserAnswerRepository userAnswerRepo = new UserAnswerRepositoryImplement(dbConnection);
        IAnswerRepository answerRepo = new AnswerRepositoryImplement(dbConnection);
        IRankRepository rankRepo = new RankRepositoryImplement(dbConnection);
        ITopicRepository topicRepo = new TopicRepositoryImplement(dbConnection);
        IQuestionRepository questionRepo = new QuestionRepositoryImplement(dbConnection);
        
        // Kh?i t?o Repository cho Room
        IRoomRepository roomRepo = new RoomRepositoryImplement(dbConnection);
        IRoomPlayerRepository roomPlayerRepo = new RoomPlayerRepositoryImplement(dbConnection);
        IRoomSettingsRepository roomSettingsRepo = new RoomSettingsRepositoryImplement(dbConnection);
        // Kh?i t?o Repository cho c�c b?ng m?i
        var databaseHelper = new DatabaseHelper(dbConnection);
        IGameSessionRepository gameSessionRepo = new GameSessionRepositoryImplement(databaseHelper);
        IGameQuestionRepository gameQuestionRepo = new GameQuestionRepositoryImplement(databaseHelper);
        ISocketConnectionRepository socketConnectionRepo = new SocketConnectionRepositoryImplement(databaseHelper);
        // Kh?i t?o EmailConfig v� EmailService
        var emailConfig = new EmailConfig
        {
            FromEmail = "dungto0300567@gmail.com", // Email th?t
            FromPassword = "your-app-password", // C?n App Password t? Google
            FromName = "Quizizz App",
            SmtpHost = "smtp.gmail.com",
            SmtpPort = 587,
            EnableSsl = true
        };
        IEmailService emailService = new EmailServiceImplement(emailConfig);
        // Kh?i t?o Service
        IAuthService authService = new AuthServiceImplement(
            userRepo, permissionRepo, roleRepo, userRoleRepo, redisService, emailService, jwtHelper,
            config.Security
        );
        IRolePermissionService rolePermissionService = new RolePermissionServiceImplement(
            rolePermissionRepo, permissionRepo, roleRepo, userRoleRepo, userRepo, redisService
        );
        IRoleService roleService = new RoleServiceImplement(roleRepo, permissionRepo, rolePermissionRepo, userRoleRepo);
        IAuthorizationService authorizationService = new AuthorizationServiceImplement(redisService, permissionRepo);
        IPermissionService permissionService = new PermissionServiceImplement(permissionRepo);
        IUserService userService = new UserServiceImplement(userRepo, userRoleRepo, roleRepo);
        IUserProfileService userProfileService = new UserProfileServiceImplement(userRepo, userAnswerRepo, rankRepo, topicRepo);
        // Kh?i t?o Service cho c�c b?ng m?i
        IGameSessionService gameSessionService = new GameSessionServiceImplement(gameSessionRepo, gameQuestionRepo, null);
        ISocketConnectionDbService socketConnectionDbService = new SocketConnectionDbServiceImplement(socketConnectionRepo);
        // Kh?i t?o shared dictionaries cho WebSocket services
        var gameRooms = new ConcurrentDictionary<string, GameRoom>();
        var webSocketConnections = new ConcurrentDictionary<string, WebSocket>();
        var socketToRoom = new ConcurrentDictionary<string, string>();
        // Kh?i t?o c�c WebSocket service con v?i shared dictionaries
        var socketConnectionSocketService = new ConsoleApp1.Service.Implement.Socket.SocketConnectionServiceImplement(webSocketConnections, socketToRoom);
        var roomManagementSocketService = new RoomManagementSocketServiceImplement(gameRooms, socketToRoom, webSocketConnections);
        IGameFlowSocketService gameFlowSocketService = new GameFlowSocketServiceImplement(gameRooms, webSocketConnections);
        IPlayerInteractionSocketService playerInteractionSocketService = new PlayerInteractionSocketServiceImplement(gameRooms, webSocketConnections);
        IScoringSocketService scoringSocketService = new ScoringSocketServiceImplement(gameRooms, webSocketConnections);
        IHostControlSocketService hostControlSocketService = new HostControlSocketServiceImplement(gameRooms, webSocketConnections);
        // Thi?t l?p reference gi?a c�c service
        socketConnectionSocketService.SetRoomManagementService(roomManagementSocketService);
        // Kh?i t?o composite SocketService v?i t?t c? dependency
        ISocketService socketService = new SocketServiceImplement(
            socketConnectionSocketService,
            roomManagementSocketService,
            gameFlowSocketService,
            playerInteractionSocketService,
            scoringSocketService,
            hostControlSocketService
        );
        // Kh?i t?o BroadcastService tru?c (kh�ng c?n joinRoomService):
        IBroadcastService broadcastService = new BroadcastServiceImplement(
            socketService, roomRepo, roomPlayerRepo, userRepo, null!); // T?m th?i null
        ICreateRoomService createRoomService = new CreateRoomServiceImplement(
            roomRepo, roomSettingsRepo, roomPlayerRepo, userRepo, userRoleRepo, roleRepo, broadcastService, socketService);
        IJoinRoomService joinRoomService = new JoinRoomServiceImplement(
            roomRepo, roomPlayerRepo, userRepo, userRoleRepo, roleRepo, createRoomService, socketService, broadcastService);
        IRoomManagementService roomManagementService = new RoomManagementServiceImplement(
            roomRepo, roomPlayerRepo, roomSettingsRepo, userRepo);
        // C?p nh?t joinRoomService cho BroadcastService:
        ((BroadcastServiceImplement)broadcastService).SetJoinRoomService(joinRoomService);
        // Kh?i t?o Controller:
        var authController = new AuthController(authService, jwtHelper);
        var forgotPasswordController = new ForgotPasswordController(authService);
        var rolePermissionController = new RolePermissionController(rolePermissionService, authorizationService, jwtHelper);
        var roleController = new RoleController(roleService, authorizationService, jwtHelper);
        var permissionController = new PermissionController(authorizationService, jwtHelper, permissionService);
        var userController = new UserController(userService, authorizationService, jwtHelper);
        var userProfileController = new UserProfileController(userProfileService, authorizationService);
        var createRoomController = new CreateRoomController(createRoomService, authorizationService);
        var joinRoomController = new JoinRoomController(joinRoomService,authorizationService);
        var leaveRoomController = new LeaveRoomController(joinRoomService, authorizationService);
        var gameController = new GameController(socketService, joinRoomService);
        var topicController = new TopicController(topicRepo);
        var questionController = new QuestionController(questionRepo, roomRepo, answerRepo);
        var gameSessionController = new GameSessionController(gameSessionService);
        var socketConnectionController = new SocketConnectionController(socketConnectionDbService);
        // Kh?i t?o Router cho t?ng Controller:
        var authRouter = new AuthRouter(authController);
        var forgotPasswordRouter = new ForgotPasswordRouter(forgotPasswordController);
        var rolePermissionRouter = new RolePermissionRouter(rolePermissionController);
        var roleRouter = new RoleRouter(roleController);
        var permissionRouter = new PermissionRouter(permissionController);
        var userRouter = new UserRouter(userController);
        var userProfileRouter = new UserProfileRouter(userProfileController, jwtHelper);
        var createRoomRouter = new CreateRoomRouter(createRoomController, jwtHelper);
        var joinRoomRouter = new JoinRoomRouter(joinRoomController, jwtHelper);
        var leaveRoomRouter = new LeaveRoomRouter(leaveRoomController, jwtHelper);
        var gameRouter = new GameRouter(gameController, questionController);
        var topicRouter = new TopicRouter(topicController);
        var gameSessionRouter = new GameSessionRouter(gameSessionController);
        var socketConnectionRouter = new SocketConnectionRouter(socketConnectionController);
        // Kh?i d?ng Socket.IO server
        await socketService.StartAsync(3001);
        // �ang k� t?t c? router v�o HttpServer
        var server = new HttpServer(
            "http://localhost:5000/",
            authRouter,
            forgotPasswordRouter,
            rolePermissionRouter,
            roleRouter,
            permissionRouter,
            userRouter,
            userProfileRouter,
            joinRoomRouter,
            createRoomRouter,
            leaveRoomRouter,
            gameRouter,
            topicRouter,
            gameSessionRouter,
            socketConnectionRouter
        );
        await server.StartAsync();
    }
}
