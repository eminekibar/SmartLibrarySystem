using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SmartLibrarySystem.DAL;
using SmartLibrarySystem.Models;

namespace SmartLibrarySystem.BLL
{
    public class UserService
    {
        private readonly UserRepository userRepository = new UserRepository();

        public IEnumerable<User> GetAll() => userRepository.GetAll();

        public IEnumerable<User> GetByRole(string role) => userRepository.GetByRole(role);

        public User GetByEmail(string email) => userRepository.GetByEmail(email);

        public User GetById(int id) => userRepository.GetById(id);

        public ValidationResult Register(User user, string password)
        {
            var validation = ValidateUser(user, password, true);
            if (!validation.IsValid)
            {
                return validation;
            }

            user.PasswordHash = PasswordHasher.Hash(password);
            userRepository.Add(user);
            return validation;
        }

        public ValidationResult UpdateUser(User user, string password = null)
        {
            var validation = ValidateUser(user, password, !string.IsNullOrWhiteSpace(password));
            if (!validation.IsValid)
            {
                return validation;
            }

            if (!string.IsNullOrWhiteSpace(password))
            {
                user.PasswordHash = PasswordHasher.Hash(password);
            }
            else
            {
                var existing = userRepository.GetById(user.UserId);
                user.PasswordHash = existing?.PasswordHash;
            }

            userRepository.Update(user);
            return validation;
        }

        public void DeleteUser(int userId) => userRepository.Delete(userId);

        public User Login(string email, string password)
        {
            var user = userRepository.GetByEmail(email);
            if (user == null)
            {
                return null;
            }

            return PasswordHasher.Verify(password, user.PasswordHash) ? user : null;
        }

        private ValidationResult ValidateUser(User user, string password, bool checkPassword)
        {
            var result = new ValidationResult();
            if (string.IsNullOrWhiteSpace(user.FullName))
            {
                result.AddError("FullName boş bırakılamaz.");
            }

            if (!IsValidEmail(user.Email))
            {
                result.AddError("Email formatı hatalı.");
            }

            if (checkPassword && !IsStrongPassword(password))
            {
                result.AddError("Parola en az 8 karakter olmalı ve bir büyük harf, bir küçük harf ile bir sayı içermelidir.");
            }

            if (string.IsNullOrWhiteSpace(user.SchoolNumber))
            {
                result.AddError("SchoolNumber boş bırakılamaz.");
            }

            var exists = userRepository.EmailExists(user.Email, user.UserId > 0 ? (int?)user.UserId : null);
            if (exists)
            {
                result.AddError("Bu e-posta ile kayıtlı kullanıcı zaten var.");
            }

            return result;
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }
            const string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern);
        }

        private static bool IsStrongPassword(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
            {
                return false;
            }

            var hasUpper = password.Any(char.IsUpper);
            var hasLower = password.Any(char.IsLower);
            var hasDigit = password.Any(char.IsDigit);
            return hasUpper && hasLower && hasDigit;
        }
    }
}
