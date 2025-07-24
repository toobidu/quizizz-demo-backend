using System.Data;
using ConsoleApp1.Data;
using ConsoleApp1.Model.Entity.Users;
using ConsoleApp1.Repository.Interface;
using Dapper;
using Npgsql;
namespace ConsoleApp1.Repository.Implement
{
    public class UserRoleRepositoryImplement : IUserRoleRepository
    {
        private readonly DatabaseHelper _dbHelper;
        public UserRoleRepositoryImplement(DatabaseHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }
        private IDbConnection CreateConnection() => _dbHelper.GetConnection();
        public async Task<UserRole?> GetByUserIdAndRoleIdAsync(int userId, int roleId)
        {
            const string query = @"
                SELECT user_id AS UserId, role_id AS RoleId 
                FROM user_roles 
                WHERE user_id = @UserId AND role_id = @RoleId";
            using var conn = CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<UserRole>(
                query,
                new { UserId = userId, RoleId = roleId }
            );
        }
        public async Task<IEnumerable<UserRole>> GetByUserIdAsync(int userId)
        {
            const string query = @"
                SELECT user_id AS UserId, role_id AS RoleId 
                FROM user_roles 
                WHERE user_id = @UserId";
            using var conn = CreateConnection();
            var result = await conn.QueryAsync<UserRole>(query, new { UserId = userId });
            return result.ToList();
        }
        public async Task<IEnumerable<UserRole>> GetByRoleIdAsync(int roleId)
        {
            const string query = @"
                SELECT user_id AS UserId, role_id AS RoleId 
                FROM user_roles 
                WHERE role_id = @RoleId";
            using var conn = CreateConnection();
            var result = await conn.QueryAsync<UserRole>(query, new { RoleId = roleId });
            return result.ToList();
        }
        public async Task<int> AddAsync(UserRole userRole)
        {
            const string query = @"
                INSERT INTO user_roles (user_id, role_id) 
                VALUES (@UserId, @RoleId)";
            using var conn = CreateConnection();
            await conn.ExecuteAsync(query, userRole);
            return userRole.UserId;
        }
        public async Task<bool> DeleteByUserIdAsync(int userId)
        {
            const string query = @"DELETE FROM user_roles WHERE user_id = @UserId";
            using var conn = CreateConnection();
            var affectedRows = await conn.ExecuteAsync(query, new { UserId = userId });
            return affectedRows > 0;
        }
        public async Task<bool> DeleteByUserIdAndRoleIdAsync(int userId, int roleId)
        {
            const string query = @"DELETE FROM user_roles WHERE user_id = @UserId AND role_id = @RoleId";
            using var conn = CreateConnection();
            var affectedRows = await conn.ExecuteAsync(query, new { UserId = userId, RoleId = roleId });
            return affectedRows > 0;
        }
    }
}
