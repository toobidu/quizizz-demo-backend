using ConsoleApp1.Mapper.Users;
using ConsoleApp1.Model.DTO.Users;
using ConsoleApp1.Model.Entity.Users;
using ConsoleApp1.Repository.Interface;
using ConsoleApp1.Service.Interface;
namespace ConsoleApp1.Service.Implement
{
    public class UserServiceImplement : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IRoleRepository _roleRepository;
        public UserServiceImplement(
            IUserRepository userRepository,
            IUserRoleRepository userRoleRepository,
            IRoleRepository roleRepository)
        {
            _userRepository = userRepository;
            _userRoleRepository = userRoleRepository;
            _roleRepository = roleRepository;
        }
        public async Task<bool> CreateUserAsync(UserDTO userDto)
        {
            var userEntity = UserMapper.ToEntity(userDto);
            var userId = await _userRepository.CreateUserAsync(userEntity);
            var roleId = await MapTypeAccountToRoleIdAsync(userDto.TypeAccount);
            if (roleId == 0) return false;
            var userRole = new UserRole(userId, roleId, DateTime.UtcNow, DateTime.UtcNow);
            await _userRoleRepository.AddAsync(userRole);
            return true;
        }
        public async Task<List<UserDTO>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllUsersAsync(1, 100);
            return users.Select(UserMapper.ToDTO).ToList();
        }
        public async Task<UserDTO?> GetUserByIdAsync(int userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            return user != null ? UserMapper.ToDTO(user) : null;
        }
        public async Task<bool> UpdateUserAsync(int userId, UserDTO updatedUser)
        {
            var existingUser = await _userRepository.GetUserByIdAsync(userId);
            if (existingUser == null) return false;
            var updatedEntity = new User(updatedUser.Username, updatedUser.FullName, updatedUser.Email, updatedUser.PhoneNumber, updatedUser.Address, updatedUser.Password, updatedUser.TypeAccount, DateTime.UtcNow, DateTime.UtcNow )
            {
                Id = userId
            };
            await _userRepository.UpdateUserAsync(updatedEntity);
            var newRoleId = await MapTypeAccountToRoleIdAsync(updatedUser.TypeAccount);
            var currentRoles = await _userRoleRepository.GetByUserIdAsync(userId);
            var currentRole = currentRoles.FirstOrDefault();
            if (currentRole == null || currentRole.RoleId != newRoleId)
            {
                await _userRoleRepository.DeleteByUserIdAsync(userId);
                await _userRoleRepository.AddAsync(new UserRole(userId, newRoleId, DateTime.UtcNow, DateTime.UtcNow));
            }
            return true;
        }
        public async Task<bool> DeleteUserAsync(int userId)
        {
            return await _userRepository.DeleteUserAsync(userId);
        }
        public async Task<bool> UpdateUserTypeAccountAsync(int userId, string newTypeAccount)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) return false;
            user.TypeAccount = newTypeAccount;
            await _userRepository.UpdateUserAsync(user);
            var roleId = await MapTypeAccountToRoleIdAsync(newTypeAccount);
            if (roleId == 0) return false;
            await _userRoleRepository.DeleteByUserIdAsync(userId);
            await _userRoleRepository.AddAsync(new UserRole(userId, roleId, DateTime.UtcNow, DateTime.UtcNow));
            return true;
        }
        public async Task<string?> GetTypeAccountAsync(int userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            return user?.TypeAccount;
        }
        public async Task<int> MapTypeAccountToRoleIdAsync(string typeAccount)
        {
            var role = await _roleRepository.GetByRoleNameAsync(typeAccount);
            return role?.Id ?? 0;
        }
    }
}
