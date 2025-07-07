using System.Text;
using ConsoleApp1.Config;
using ConsoleApp1.Controller;
using ConsoleApp1.Repository.Implement;
using ConsoleApp1.Router;
using ConsoleApp1.Security;
using ConsoleApp1.Service.Implement;
using ConsoleApp1.Service.Interface;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        // Load cấu hình
        var config = ConfigLoader.Load();

        // Khởi tạo Redis và JWT
        var redisConn = new RedisConnection(config.Redis);
        var redisService = new RedisService(redisConn);
        var jwtHelper = new JwtHelper(config.Security);

        // Khởi tạo Repository
        string dbConnection = config.ConnectionStrings["DefaultConnection"];
        var userRepo = new UserRepositoryImplement(dbConnection);
        var roleRepo = new RoleRepositoryImplement(dbConnection);
        var userRoleRepo = new UserRoleRepositoryImplement(dbConnection);
        var rolePermissionRepo = new RolePermissionRepositoryImplement(dbConnection);
        var permissionRepo = new PermissionRepositoryImplement(dbConnection);

        // Khởi tạo Service
        IAuthService authService = new AuthService(
            userRepo, rolePermissionRepo, permissionRepo, roleRepo, userRoleRepo, redisService, jwtHelper, config.Security
        );

        IRolePermissionService rolePermissionService = new RolePermissionService(
            rolePermissionRepo, permissionRepo, roleRepo, userRoleRepo, userRepo, redisService
        );

        // Khởi tạo AuthorizationService
        IAuthorizationService authorizationService = new AuthorizationService(redisService, permissionRepo);

        // Khởi tạo Controller
        var authController = new AuthController(authService, jwtHelper);
        var rolePermissionController = new RolePermissionController(
            rolePermissionService,
            authorizationService,
            jwtHelper
        );

        // Khởi tạo và chạy HttpServer
        var server = new HttpServer(
            "http://localhost:5000/",
            new AuthRouter(authController),
            new RolePermissionRouter(rolePermissionController)
        );

        await server.StartAsync();
    }
}
