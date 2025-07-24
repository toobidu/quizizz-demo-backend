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
using SocketConnectionService = ConsoleApp1.Service.Implement.Socket.SocketConnectionServiceImplement;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        var config = ConfigLoader.Load();

        // Initialize Redis and JWT helper
        var redisConn = new RedisConnection(config.Redis);
        var redisService = new RedisServiceImplement(redisConn);
        var jwtHelper = new JwtHelper(config.Security);

        // Initialize Repositories
        string dbConnection = config.ConnectionStrings["DefaultConnection"];
        var databaseHelper = new DatabaseHelper(dbConnection);
        IUserRepository userRepo = new UserRepositoryImplement(databaseHelper);
        IRoleRepository roleRepo = new RoleRepositoryImplement(databaseHelper);
        IUserRoleRepository userRoleRepo = new UserRoleRepositoryImplement(databaseHelper);
        IRolePermissionRepository rolePermissionRepo = new RolePermissionRepositoryImplement(databaseHelper);
        IPermissionRepository permissionRepo = new PermissionRepositoryImplement(databaseHelper);
        IUserAnswerRepository userAnswerRepo = new UserAnswerRepositoryImplement(databaseHelper);
        IAnswerRepository answerRepo = new AnswerRepositoryImplement(databaseHelper);
        IRankRepository rankRepo = new RankRepositoryImplement(databaseHelper);
        ITopicRepository topicRepo = new TopicRepositoryImplement(databaseHelper);
        IQuestionRepository questionRepo = new QuestionRepositoryImplement(databaseHelper);

        // Initialize Room Repositories
        IRoomRepository roomRepo = new RoomRepositoryImplement(databaseHelper);
        IRoomPlayerRepository roomPlayerRepo = new RoomPlayerRepositoryImplement(databaseHelper);
        IRoomSettingsRepository roomSettingsRepo = new RoomSettingsRepositoryImplement(databaseHelper);

        // Initialize new Repositories
        IGameSessionRepository gameSessionRepo = new GameSessionRepositoryImplement(databaseHelper);
        IGameQuestionRepository gameQuestionRepo = new GameQuestionRepositoryImplement(databaseHelper);
        ISocketConnectionRepository socketConnectionRepo = new SocketConnectionRepositoryImplement(databaseHelper);

        // Initialize EmailConfig and EmailService
        var emailConfig = new EmailConfig
        {
            FromEmail = "dungto0300567@gmail.com",
            FromPassword = "your-app-password",
            FromName = "Quizizz App",
            SmtpHost = "smtp.gmail.com",
            SmtpPort = 587,
            EnableSsl = true
        };
        IEmailService emailService = new EmailServiceImplement(emailConfig);

        // Initialize Services
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

        // Initialize new Services
        IGameSessionService gameSessionService = new GameSessionServiceImplement(gameSessionRepo, gameQuestionRepo, questionRepo);
        ISocketConnectionDbService socketConnectionDbService = new SocketConnectionDbServiceImplement(socketConnectionRepo);

        // Shared dictionaries for WebSocket services
        var gameRooms = new ConcurrentDictionary<string, GameRoom>();
        var webSocketConnections = new ConcurrentDictionary<string, WebSocket>();
        var socketToRoom = new ConcurrentDictionary<string, string>();

        // Initialize WebSocket services
        var socketConnectionSocketService = new SocketConnectionService(webSocketConnections, socketToRoom);
        var roomManagementSocketService = new RoomManagementSocketServiceImplement(gameRooms, socketToRoom, webSocketConnections);
        IGameFlowSocketService gameFlowSocketService = new GameFlowSocketServiceImplement(gameRooms, webSocketConnections);
        IPlayerInteractionSocketService playerInteractionSocketService = new PlayerInteractionSocketServiceImplement(gameRooms, webSocketConnections);
        IScoringSocketService scoringSocketService = new ScoringSocketServiceImplement(gameRooms, webSocketConnections);
        IHostControlSocketService hostControlSocketService = new HostControlSocketServiceImplement(gameRooms, webSocketConnections);

        // Set dependencies between services
        socketConnectionSocketService.SetRoomManagementService(roomManagementSocketService);

        // Initialize composite SocketService
        ISocketService socketService = new SocketServiceImplement(
            socketConnectionSocketService,
            roomManagementSocketService,
            gameFlowSocketService,
            playerInteractionSocketService,
            scoringSocketService,
            hostControlSocketService
        );

        // Initialize BroadcastService
        IBroadcastService broadcastService = new BroadcastServiceImplement(
            socketService, roomRepo, roomPlayerRepo, userRepo, null!);
        ICreateRoomService createRoomService = new CreateRoomServiceImplement(
            roomRepo, roomSettingsRepo, roomPlayerRepo, userRepo, userRoleRepo, roleRepo, broadcastService, socketService);
        IJoinRoomService joinRoomService = new JoinRoomServiceImplement(
            roomRepo, roomPlayerRepo, userRepo, userRoleRepo, roleRepo, createRoomService, socketService, broadcastService);
        IRoomManagementService roomManagementService = new RoomManagementServiceImplement(
            roomRepo, roomPlayerRepo, roomSettingsRepo, userRepo);

        // Update joinRoomService for BroadcastService
        ((BroadcastServiceImplement)broadcastService).SetJoinRoomService(joinRoomService);

        // Initialize Controllers
        var authController = new AuthController(authService, jwtHelper);
        var forgotPasswordController = new ForgotPasswordController(authService);
        var rolePermissionController = new RolePermissionController(rolePermissionService, authorizationService, jwtHelper);
        var roleController = new RoleController(roleService, authorizationService, jwtHelper);
        var permissionController = new PermissionController(authorizationService, jwtHelper, permissionService);
        var userController = new UserController(userService, authorizationService, jwtHelper);
        var userProfileController = new UserProfileController(userProfileService, authorizationService);
        var createRoomController = new CreateRoomController(createRoomService, authorizationService);
        var joinRoomController = new JoinRoomController(joinRoomService, authorizationService);
        var leaveRoomController = new LeaveRoomController(joinRoomService, authorizationService);
        var gameController = new GameController(socketService, joinRoomService);
        var topicController = new TopicController(topicRepo);
        var questionController = new QuestionController(questionRepo, roomRepo, answerRepo);
        var socketConnectionService = new ConsoleApp1.Service.Implement.SocketConnectionServiceImplement();
        var gameSessionController = new GameSessionController(gameSessionRepo, gameQuestionRepo, roomRepo, socketConnectionService);
        var socketConnectionController = new SocketConnectionController(socketConnectionRepo, userRepo, roomRepo, socketConnectionService);

        // Initialize Routers
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
        var questionRouter = new QuestionRouter(questionController);
        var gameSessionRouter = new GameSessionRouter(gameSessionService);
        var socketConnectionRouter = new SocketConnectionRouter(socketConnectionDbService);
        var userAnswerRouter = new UserAnswerRouter();
        var rankingRouter = new RankingRouter();
        var healthCheckRouter = new HealthCheckRouter();

        // Start Socket.IO server
        await socketService.StartAsync(3001);

        // Register all routers to HttpServer
        var server = new HttpServer(
            "http://localhost:5000/",
            healthCheckRouter,
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
            questionRouter,
            gameSessionRouter,
            socketConnectionRouter,
            userAnswerRouter,
            rankingRouter
        );

        // Display startup information
        Console.WriteLine("===========================================");
        Console.WriteLine("🎯 QUIZIZZ API SERVER STARTING...");
        Console.WriteLine("===========================================");
        Console.WriteLine();
        Console.WriteLine("📡 HTTP Server: http://localhost:5000");
        Console.WriteLine("🔌 WebSocket Server: ws://localhost:3001");
        Console.WriteLine("🩺 Health Check: http://localhost:5000/health");
        Console.WriteLine();
        Console.WriteLine("🚀 Ready to accept connections!");
        Console.WriteLine("   Press Ctrl+C to stop the server");
        Console.WriteLine("===========================================");
        Console.WriteLine();

        await server.StartAsync();
    }
}