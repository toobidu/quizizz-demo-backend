using System.Text;
using ConsoleApp1.Config;
using ConsoleApp1.Controller;
using ConsoleApp1.Data;
using ConsoleApp1.Repository.Implement;
using ConsoleApp1.Router;
using ConsoleApp1.Security;
using ConsoleApp1.Service;
using ConsoleApp1.Service.Implement;
using ConsoleApp1.Service.Interface;
using ConsoleApp1.Repository.Interface;

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
        ICreateRoomService createRoomService = new CreateRoomServiceImplement(
            roomRepo, roomSettingsRepo, roomPlayerRepo, userRepo, userRoleRepo, roleRepo);
        IJoinRoomService joinRoomService = new JoinRoomServiceImplement(
            roomRepo, roomPlayerRepo, userRepo, userRoleRepo, roleRepo, createRoomService);
        ISocketService socketService = new SocketServiceImplement();

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
        var gameController = new GameController(socketService, joinRoomService);

        // Khởi tạo Router cho từng Controller:
        var authRouter = new AuthRouter(authController);
        var forgotPasswordRouter = new ForgotPasswordRouter(forgotPasswordController);
        var rolePermissionRouter = new RolePermissionRouter(rolePermissionController);
        var roleRouter = new RoleRouter(roleController);
        var permissionRouter = new PermissionRouter(permissionController);
        var userRouter = new UserRouter(userController);
        var userProfileRouter = new UserProfileRouter(userProfileController);
        var createRoomRouter = new CreateRoomRouter(createRoomController);
        var joinRoomRouter = new JoinRoomRouter(joinRoomController);
        var gameRouter = new GameRouter(gameController);

        // Khởi động Socket.IO server
        await socketService.StartAsync(3001);
        
        // Đăng ký tất cả router vào HttpServer
        var server = new HttpServer(
            "http://localhost:5000/",
            authRouter,
            forgotPasswordRouter,
            rolePermissionRouter,
            roleRouter,
            permissionRouter,
            userRouter,
            userProfileRouter,
            createRoomRouter,
            joinRoomRouter,
            gameRouter
        );

        await server.StartAsync();
    }
}