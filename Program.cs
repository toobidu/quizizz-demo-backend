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

        // Khởi tạo Redis và JWT helper
        var redisConn = new RedisConnection(config.Redis);
        var redisService = new RedisServiceImplement(redisConn);
        var jwtHelper = new JwtHelper(config.Security);

        // Khởi tạo Repository (dữ liệu từ PostgreSQL)
        string dbConnection = config.ConnectionStrings["DefaultConnection"];
        IUserRepository userRepo = new UserRepositoryImplement(dbConnection);
        IRoleRepository roleRepo = new RoleRepositoryImplement(dbConnection);
        IUserRoleRepository userRoleRepo = new UserRoleRepositoryImplement(dbConnection);
        IRolePermissionRepository rolePermissionRepo = new RolePermissionRepositoryImplement(dbConnection);
        IPermissionRepository permissionRepo = new PermissionRepositoryImplement(dbConnection);
        IUserAnswerRepository userAnswerRepo = new UserAnswerRepositoryImplement(dbConnection);
        IRankRepository rankRepo = new RankRepositoryImplement(dbConnection);
        ITopicRepository topicRepo = new TopicRepositoryImplement(dbConnection);

        // Khởi tạo Repository cho Room
        IRoomRepository roomRepo = new RoomRepositoryImplement(dbConnection);
        IRoomPlayerRepository roomPlayerRepo = new RoomPlayerRepositoryImplement(dbConnection);
        IRoomSettingsRepository roomSettingsRepo = new RoomSettingsRepositoryImplement(dbConnection);

        // Khởi tạo EmailConfig và EmailService
        var emailConfig = new EmailConfig
        {
            FromEmail = "dungto0300567@gmail.com", // Email thật
            FromPassword = "your-app-password", // Cần App Password từ Google
            FromName = "Quizizz App",
            SmtpHost = "smtp.gmail.com",
            SmtpPort = 587,
            EnableSsl = true
        };
        IEmailService emailService = new EmailServiceImplement(emailConfig);

        // Khởi tạo Service
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
        // Khởi tạo shared dictionaries cho WebSocket services
        var gameRooms = new ConcurrentDictionary<string, GameRoom>();
        var webSocketConnections = new ConcurrentDictionary<string, WebSocket>();
        var socketToRoom = new ConcurrentDictionary<string, string>();
        
        // Khởi tạo các WebSocket service con với shared dictionaries
        var socketConnectionService = new SocketConnectionServiceImplement(webSocketConnections, socketToRoom);
        var roomManagementSocketService = new RoomManagementSocketServiceImplement(gameRooms, socketToRoom, webSocketConnections);
        IGameFlowSocketService gameFlowSocketService = new GameFlowSocketServiceImplement();
        IPlayerInteractionSocketService playerInteractionSocketService = new PlayerInteractionSocketServiceImplement();
        IScoringSocketService scoringSocketService = new ScoringSocketServiceImplement();
        IHostControlSocketService hostControlSocketService = new HostControlSocketServiceImplement(gameRooms, webSocketConnections);
        
        // Thiết lập reference giữa các service
        socketConnectionService.SetRoomManagementService(roomManagementSocketService);
        
        // Khởi tạo composite SocketService với tất cả dependency
        ISocketService socketService = new SocketServiceImplement(
            socketConnectionService,
            roomManagementSocketService,
            gameFlowSocketService,
            playerInteractionSocketService,
            scoringSocketService,
            hostControlSocketService
        );
        // Khởi tạo BroadcastService trước (không cần joinRoomService):
        IBroadcastService broadcastService = new BroadcastServiceImplement(
            socketService, roomRepo, roomPlayerRepo, userRepo, null!); // Tạm thời null

        ICreateRoomService createRoomService = new CreateRoomServiceImplement(
            roomRepo, roomSettingsRepo, roomPlayerRepo, userRepo, userRoleRepo, roleRepo, broadcastService, socketService);
        IJoinRoomService joinRoomService = new JoinRoomServiceImplement(
            roomRepo, roomPlayerRepo, userRepo, userRoleRepo, roleRepo, createRoomService, socketService, broadcastService);
        IRoomManagementService roomManagementService = new RoomManagementServiceImplement(
            roomRepo, roomPlayerRepo, roomSettingsRepo, userRepo);

        // Cập nhật joinRoomService cho BroadcastService:
        ((BroadcastServiceImplement)broadcastService).SetJoinRoomService(joinRoomService);

        // Khởi tạo Controller:
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

        // Khởi tạo Router cho từng Controller:
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
        var gameRouter = new GameRouter(gameController);
        var topicRouter = new TopicRouter(topicController);

        // Khởi động Socket.IO server
        Console.WriteLine("[Máy chủ] Đang khởi động máy chủ Socket.IO trên cổng 3001...");
        await socketService.StartAsync(3001);
        Console.WriteLine("[Máy chủ] Máy chủ Socket.IO đã khởi động thành công trên cổng 3001");
        
        // Đăng ký tất cả router vào HttpServer
        Console.WriteLine("[Máy chủ] Đang khởi tạo máy chủ HTTP tại http://localhost:5000/");
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
            topicRouter
        );

        Console.WriteLine("[Máy chủ] Đang khởi động máy chủ HTTP...");
        Console.WriteLine("[Máy chủ] Các endpoint có sẵn:");
        Console.WriteLine("[Máy chủ] - GET /api/profile/me (Lấy thông tin người dùng hiện tại)");
        Console.WriteLine("[Máy chủ] - GET /api/profile/search/{username} (Tìm kiếm người dùng)");
        Console.WriteLine("[Máy chủ] - PUT /api/profile/password (Đổi mật khẩu)");
        Console.WriteLine("[Máy chủ] - PUT /api/profile/update (Cập nhật thông tin)");
        Console.WriteLine("[Máy chủ] ===========================================");
        Console.WriteLine("[Máy chủ] QUAN TRỌNG: Frontend nên kết nối đến http://localhost:5000, KHÔNG phải cổng 8080!");
        Console.WriteLine("[Máy chủ] ===========================================");
        await server.StartAsync();
    }
}